"""Create a fresh, hash-sealed production dependency candidate for input profiling.

Run with the repository Python runtime. No source file is changed. --estimate
performs selection and capacity checks only; normal execution creates the copy.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess


SCENE = "Assets/_Project/Scenes/Production/GreaterWasteland.unity"
VALIDATION_ROOT = Path("D:/BooterBigArmValidation")
VALIDATION_ROOTS = (VALIDATION_ROOT, Path("C:/BooterBigArmValidation"))
HEADROOM = 30 * 1024**3


def git(root: Path, *args: str) -> bytes:
    return subprocess.run(["git", "-C", str(root), *args], check=True,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout


def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def safe_relative(value: str) -> str:
    # AssetDatabase and Git return repository-relative POSIX paths.
    if "\\" in value or ":" in value:
        raise ValueError(f"Invalid repository-relative path: {value!r}")
    path = PurePosixPath(value)
    if path.is_absolute() or ".." in path.parts or str(path) != value:
        raise ValueError(f"Invalid repository-relative path: {value!r}")
    return value


def source_path(root: Path, value: str) -> Path:
    path = root.joinpath(*PurePosixPath(safe_relative(value)).parts)
    if not path.resolve().is_relative_to(root):
        raise ValueError(f"Source escapes repository: {value}")
    # Reject linked sources rather than copying externally owned content.
    for ancestor in (path, *path.parents):
        if ancestor == root:
            break
        if ancestor.is_symlink() or (hasattr(ancestor, "is_junction") and ancestor.is_junction()):
            raise ValueError(f"Linked source: {value}")
    return path


def eligible_asset(value: str) -> bool:
    parts = PurePosixPath(value).parts
    lowered = {part.lower() for part in parts}
    if not parts or parts[0] != "Assets":
        return False
    if any(part.startswith(".") for part in parts):
        return False  # Unity does not import .gitkeep or other dot files.
    if lowered.intersection({"terrain", "legacy", "legacy2d", "historicalproject"}):
        return False
    if re.search(r"\.blend(?:\d+)?(?:\.meta)?$", value, re.IGNORECASE):
        return False
    if "Scenes" in parts:
        # Keep the production scene, but never unrelated scene contents.
        return value in {SCENE, SCENE + ".meta"} or value.endswith(".meta") and not ".unity" in value
    return True


def select_files(root: Path, dependency_file: Path) -> list[str]:
    checked_paths: dict[str, Path] = {}

    def checked(value: str) -> Path:
        if value not in checked_paths:
            checked_paths[value] = source_path(root, value)
        return checked_paths[value]

    dependencies = json.loads(dependency_file.read_text(encoding="utf-8-sig"))
    if not isinstance(dependencies, list) or any(not isinstance(v, str) for v in dependencies):
        raise ValueError("Dependency JSON must be an AssetDatabase string array")
    selected = {safe_relative(v) for v in dependencies if v.startswith("Assets/")}
    if SCENE not in selected:
        raise ValueError("Dependency snapshot does not include GreaterWasteland")
    tracked = git(root, "ls-files", "-z").decode("utf-8").split("\0")
    selected.update(safe_relative(v) for v in tracked if v and eligible_asset(v))
    selected.update({"Packages/manifest.json", "Packages/packages-lock.json"})
    selected.update(path.relative_to(root).as_posix()
                    for path in (root / "ProjectSettings").rglob("*") if path.is_file())
    # Include every selected asset and ancestor's existing metadata. Folder
    # metadata preserves GUIDs without importing unrelated directory contents.
    for value in list(selected):
        if not value.startswith("Assets/"):
            continue
        path = checked(value)
        if not value.endswith(".meta") and path.is_file():
            meta = value + ".meta"
            if not checked(meta).is_file():
                raise FileNotFoundError(f"Missing required asset metadata: {meta}")
            selected.add(meta)
        for parent in PurePosixPath(value).parents:
            if str(parent) in {".", "Assets"}:
                break
            meta = str(parent) + ".meta"
            if checked(meta).is_file():
                selected.add(meta)
    # AssetDatabase may return folder paths; directories are created alongside
    # their .meta files and need no byte receipt of their own.
    files = []
    for value in sorted(selected):
        path = checked(value)
        if path.is_dir():
            continue
        if not path.is_file():
            raise FileNotFoundError(f"Missing selected live source: {value}")
        if value.endswith(".meta"):
            owner = checked(value[:-5])
            if not owner.exists():
                raise FileNotFoundError(f"Orphan metadata: {value}")
        files.append(value)
    return files


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--destination", type=Path,
                        default=VALIDATION_ROOT / "InputInteraction20261012-01")
    parser.add_argument("--dependencies", type=Path, default=Path("Logs/input-profile-dependencies.json"))
    parser.add_argument("--estimate", action="store_true")
    args = parser.parse_args()
    root = args.source.resolve(strict=True)
    destination = args.destination.resolve()
    validation_root = destination.parent.resolve(strict=True)
    if validation_root not in {value.resolve() for value in VALIDATION_ROOTS} or not re.fullmatch(r"InputInteraction[A-Za-z0-9-]+", destination.name):
        raise ValueError("Destination must be a fresh InputInteraction child of C:/BooterBigArmValidation or D:/BooterBigArmValidation")
    if args.destination.exists() or args.destination.is_symlink() or destination.exists():
        raise FileExistsError(f"Destination already exists: {destination}")
    dependency_file = args.dependencies if args.dependencies.is_absolute() else root / args.dependencies
    dependency_file = dependency_file.resolve(strict=True)
    if not dependency_file.is_relative_to(root):
        raise ValueError("Dependency snapshot must be inside source checkout")
    files = select_files(root, dependency_file)
    total = sum(source_path(root, v).stat().st_size for v in files)
    free = shutil.disk_usage(validation_root).free
    if free - total < HEADROOM:
        raise OSError(f"Insufficient space: {free} free - {total} selected must leave {HEADROOM} bytes")
    print(json.dumps({"files": len(files), "selectedBytes": total, "selectedGiB": total / 1024**3,
                      "freeBytes": free, "headroomAfterCopyBytes": free - total,
                      "destination": str(destination)}, indent=2), flush=True)
    if args.estimate:
        return
    source_head = git(root, "rev-parse", "HEAD").decode().strip()
    source_dirty = git(root, "status", "--porcelain=v1", "-z").decode("utf-8")
    dependency_hash = digest(dependency_file)
    # Seal all initial live bytes before beginning any copy.
    sealed = {value: {"bytes": source_path(root, value).stat().st_size,
                      "sourceBeforeSha256": digest(source_path(root, value))} for value in files}
    destination.mkdir()  # Never reuse or delete a previous candidate.
    records = []
    for value in files:
        source = source_path(root, value)
        target = destination.joinpath(*PurePosixPath(value).parts)
        target.parent.mkdir(parents=True, exist_ok=True)
        if value.endswith(".meta") and source_path(root, value[:-5]).is_dir():
            destination.joinpath(*PurePosixPath(value[:-5]).parts).mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        copied = digest(target)
        if copied != sealed[value]["sourceBeforeSha256"]:
            raise RuntimeError(f"Source changed during copy: {value}; candidate retained for inspection")
        records.append({"path": value, **sealed[value], "copiedSha256": copied})
    # Final whole-source pass catches files changed after their individual copy.
    for record in records:
        record["sourceAfterSha256"] = digest(source_path(root, record["path"]))
        if record["sourceAfterSha256"] != record["sourceBeforeSha256"]:
            raise RuntimeError(f"Source changed after copy: {record['path']}; candidate retained")
    if digest(dependency_file) != dependency_hash:
        raise RuntimeError("Dependency snapshot changed during copy")
    settings = destination / "ProjectSettings/ProjectSettings.asset"
    original = settings.read_bytes()
    pattern = rb"(?m)^(  productName: )([^\r\n]*)"
    matches = list(re.finditer(pattern, original))
    if len(matches) != 1:
        raise RuntimeError("Expected exactly one serialized productName field")
    changed = re.sub(pattern, lambda m: m.group(1) + destination.name.encode("ascii"), original)
    settings.write_bytes(changed)
    free_after = shutil.disk_usage(validation_root).free
    if free_after < HEADROOM:
        raise OSError("Actual copy left less than 30 GiB; candidate retained for inspection")
    override = {"path": "ProjectSettings/ProjectSettings.asset", "field": "productName",
                "originalValue": matches[0].group(2).decode("utf-8"), "candidateValue": destination.name,
                "originalSha256": hashlib.sha256(original).hexdigest(), "changedSha256": digest(settings)}
    receipt = {"schemaVersion": 1, "createdUtc": datetime.now(timezone.utc).isoformat(),
               "source": str(root), "destination": str(destination), "sourceHead": source_head,
               "sourceDirtyPorcelainV1Z": source_dirty, "dependencySnapshot": str(dependency_file),
               "dependencySnapshotSha256": dependency_hash, "selectedBytes": total,
               "freeBytesBefore": free, "freeBytesAfter": free_after,
               "minimumHeadroomBytes": HEADROOM, "scenePath": SCENE,
               "sceneSha256": sealed[SCENE]["sourceBeforeSha256"],
               "candidateOnlyOverrides": [override], "files": records}
    logs = destination / "Logs"
    logs.mkdir(exist_ok=True)
    (logs / "input-profile-copy-receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(f"Sealed {len(records)} live files; receipt: {logs / 'input-profile-copy-receipt.json'}", flush=True)


if __name__ == "__main__":
    main()
