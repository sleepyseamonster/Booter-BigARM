"""Check Engine documentation, records, and obvious Unity build dependencies.

This focused static check is not a complete dependency or security audit.
"""
import json
import os
import re
import subprocess
from pathlib import Path
from urllib.parse import unquote, urlsplit
from common import ROOT

SKIP = {".git", ".cache", "build", "out", "__pycache__"}


def inspect(root):
    errors, warnings = [], []
    files = []
    for directory, folders, names in os.walk(root, followlinks=False):
        folders[:] = [name for name in folders if name not in SKIP]
        for name in folders:
            candidate = Path(directory) / name
            if candidate.is_symlink() and root not in candidate.resolve().parents:
                errors.append(str(candidate.relative_to(root)) + ": external directory symlink")
        files.extend(Path(directory) / name for name in names)
    for path in files:
        relative = str(path.relative_to(root))
        if path.is_symlink() and root not in path.resolve().parents:
            errors.append(relative + ": external symlink")
            continue
        if path.suffix == ".md":
            text = path.read_text(encoding="utf-8")
            if text.count("```") % 2:
                errors.append(relative + ": unbalanced code fences")
            if any(line != line.rstrip() for line in text.splitlines()):
                errors.append(relative + ": trailing whitespace")
            prose = re.sub(r"```.*?```", "", text, flags=re.S)
            for link in re.findall(r"\]\(([^)]+)\)", prose):
                link = link.strip("<>")
                parsed = urlsplit(link)
                if parsed.scheme or not parsed.path:
                    continue
                target = (path.parent / unquote(parsed.path)).resolve()
                if not target.exists():
                    errors.append(relative + ": broken local link " + link)
                elif root not in target.parents and target != root:
                    warnings.append(relative + ": external read-only reference " + link)
        if path.suffix == ".json":
            try:
                value = json.loads(path.read_text(encoding="utf-8"))
                if isinstance(value, dict) and "schema_version" in value and value["schema_version"] != 1:
                    errors.append(relative + ": unsupported schema_version")
            except (ValueError, UnicodeError) as error:
                errors.append(relative + ": invalid JSON " + str(error))
        if path.suffix in {".cpp", ".h", ".hpp", ".cmake"} or path.name == "CMakeLists.txt":
            text = path.read_text(encoding="utf-8")
            if re.search(r"UnityEngine|UnityEditor|\.\./(?:\.\./)*(?:Assets|Library)/", text):
                errors.append(relative + ": possible Unity runtime/build dependency")
    return errors, warnings, len(files)


def main():
    errors, warnings, count = inspect(ROOT)
    tracked = subprocess.check_output(["git", "ls-files", "--", "."], cwd=ROOT, text=True)
    for name in tracked.splitlines():
        if any(part in SKIP for part in Path(name).parts):
            errors.append("Tracked generated output: " + name)
    inventory = ROOT / "Research/dependencies.json"
    if inventory.exists():
        data = json.loads(inventory.read_text())
        seen = set()
        for row in data["dependencies"]:
            if row["id"] in seen:
                errors.append("Duplicate dependency ID: " + row["id"])
            seen.add(row["id"])
            if row["status"] == "selected" and not row.get("revision"):
                errors.append("Selected dependency without revision: " + row["id"])
    print(json.dumps({"files_checked": count, "errors": errors,
                      "external_reference_links": warnings}, indent=2))
    return int(bool(errors))


if __name__ == "__main__":
    raise SystemExit(main())
