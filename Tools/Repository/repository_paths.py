"""Repository-root discovery and validated, read-only relocation resolution."""

import json
from pathlib import Path, PureWindowsPath


def find_root(start):
    start = Path(start).resolve()
    if start.is_file():
        start = start.parent
    for candidate in (start, *start.parents):
        if (candidate / "Packages/manifest.json").is_file() and (candidate / "ProjectSettings/ProjectVersion.txt").is_file():
            return candidate
    raise ValueError("Unity repository anchors were not found above the tool")


def relative_name(value):
    if not isinstance(value, str) or not value or "\\" in value:
        raise ValueError(f"Expected a slash-separated relative path: {value}")
    path = Path(value)
    if path.is_absolute() or PureWindowsPath(value).is_absolute() or PureWindowsPath(value).drive or ".." in path.parts:
        raise ValueError(f"Path must remain relative to its root: {value}")
    return path.as_posix()


def contained(root, value):
    resolved = (root / relative_name(value)).resolve()
    if not resolved.is_relative_to(root.resolve()):
        raise ValueError(f"Path escapes the repository: {value}")
    return resolved


class Relocations:
    def __init__(self, records=(), versions=()):
        self.edges = {}
        self.versions = {}
        for row in versions:
            key = (relative_name(row["source"]), row["sha256"])
            destination = relative_name(row["destination"])
            if key in self.versions and self.versions[key] != destination:
                raise ValueError("Conflicting preserved content version")
            self.versions[key] = destination
        for row in records:
            source = relative_name(row["source"])
            target = relative_name(row["destination"])
            if source == target:
                continue
            if source in self.edges and self.edges[source] != target:
                raise ValueError(f"Conflicting relocation for {source}")
            self.edges[source] = target
        for source in self.edges:
            self.resolve(source)
        destinations = {}
        for source, target in self.edges.items():
            key = target.casefold()
            if key in destinations and destinations[key] != source:
                raise ValueError(f"Relocation target collision: {target}")
            destinations[key] = source

    def resolve(self, value):
        value = relative_name(value)
        visited = set()
        while value in self.edges:
            if value in visited:
                raise ValueError("Relocation cycle detected")
            visited.add(value)
            value = self.edges[value]
        return value

    @classmethod
    def from_repository(cls, root):
        directory = root / "Docs/Operations/Repository/Relocations"
        records = []
        versions = []
        if directory.is_dir():
            for receipt in sorted(directory.glob("*.json")):
                value = json.loads(receipt.read_text(encoding="utf-8"))
                records.extend(value["records"])
                versions.extend(value.get("preserved_versions", []))
        return cls(records, versions)

    def path(self, root, value):
        return contained(root, self.resolve(value))

    def version_path(self, root, value, expected_hash):
        key = (relative_name(value), expected_hash)
        return self.path(root, self.versions.get(key, value))
