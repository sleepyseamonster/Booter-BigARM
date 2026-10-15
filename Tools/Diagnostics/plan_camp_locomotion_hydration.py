"""Read-only GUID/dependency transfer plan for the current saved Camp scene.

Runtime/editor/test sources, dynamic Resources and shader includes are added as
explicit compilation/bootstrap roots. Applying the plan is a separate guarded
background operation; this tool never writes either Unity Assets tree.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

GUID = re.compile(rb"guid:\s*([a-fA-F0-9]{32})")
TEXT = {".unity", ".prefab", ".asset", ".mat", ".controller", ".anim", ".overridecontroller", ".meta", ".asmdef"}


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("repository", type=Path)
    parser.add_argument("candidate", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    root, candidate = args.repository.resolve(), args.candidate.resolve()
    if root == candidate or not (root / ".git").exists() or (candidate / ".git").exists() or not (candidate / ".animation-playground-validation-project").exists():
        raise ValueError("Use the canonical Git checkout and its marked isolated candidate.")
    if args.output.exists():
        raise FileExistsError("Preserve prior hydration plans.")
    index = {}
    for metadata in (root / "Assets").rglob("*.meta"):
        match = re.search(rb"(?m)^guid: ([a-fA-F0-9]{32})", metadata.read_bytes())
        if match:
            index.setdefault(match[1].decode().lower(), []).append(Path(str(metadata)[:-5]))
    pending = [root / "Assets/_Project/Scenes/Reference/CampLoopTesting.unity"]
    for directory in ["Assets/_Project/Scripts", "Assets/_Project/Tests", "Assets/_Project/Shaders"]:
        if (root / directory).exists():
            pending.extend(p for p in (root / directory).rglob("*") if p.is_file() and p.suffix != ".meta")
    pending.extend(p for p in (root / "Assets").rglob("*") if p.is_file() and "Resources" in p.parts and p.suffix != ".meta")
    visited, unresolved = set(), set()
    while pending:
        asset = pending.pop()
        if asset in visited or not asset.is_file():
            continue
        if not asset.resolve().is_relative_to(root / "Assets"):
            raise ValueError("Dependency left Assets.")
        visited.add(asset)
        for source in [asset, Path(str(asset) + ".meta")]:
            if source.suffix not in TEXT or not source.exists():
                continue
            for value in GUID.findall(source.read_bytes()):
                guid = value.decode().lower()
                if guid.startswith("0000000000000000"):
                    continue
                matches = index.get(guid, [])
                if len(matches) > 1:
                    raise ValueError(f"Ambiguous dependency GUID: {guid}")
                if matches:
                    if matches[0].is_file():
                        pending.append(matches[0])
                else:
                    unresolved.add(guid)
    selected = set(visited)
    for asset in list(visited):
        metadata = Path(str(asset) + ".meta")
        if not metadata.exists():
            raise FileNotFoundError(f"Missing paired metadata: {asset}")
        selected.add(metadata)
        for parent in asset.parents:
            if parent == root / "Assets" or not parent.is_relative_to(root / "Assets"):
                break
            metadata = Path(str(parent) + ".meta")
            if metadata.exists():
                selected.add(metadata)
    rows = []
    for source in sorted(selected):
        relative = source.relative_to(root).as_posix()
        target = candidate / relative
        original = sha(target) if target.exists() else None
        expected = sha(source)
        rows.append(dict(path=relative, sha256=expected, candidateBefore=original,
                         bytes=source.stat().st_size, transfer=expected != original))
    report = dict(state="read-only-current-saved-scene-hydration-plan", repository=str(root), candidate=str(candidate),
                  files=rows, unresolvedGuids=sorted(unresolved),
                  dependencyScope="Camp scene YAML closure plus current scripts/tests, dynamic Resources and shader includes")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8", newline="\n")
    transfers = [r for r in rows if r["transfer"]]
    print(json.dumps(dict(files=len(rows), transfers=len(transfers), bytes=sum(r["bytes"] for r in transfers),
                          unresolvedGuids=len(unresolved))))


if __name__ == "__main__":
    main()
