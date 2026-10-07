"""Copy a stable committed Unity snapshot with materialized LFS and the baseline capture tool.

Unrelated dirty tracked files use HEAD bytes; untracked owner work is excluded.
This is an isolated validation directory, never a Git worktree.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[4]
CAPTURE = "Assets/_Project/Scripts/Editor/TopDown3D/BadwaterTerrainBaselineCapture.cs"

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)

def digest(data):
    return hashlib.sha256(data).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if output.exists() or output == ROOT or ROOT in output.parents:
        raise ValueError("Snapshot must be a fresh directory outside the checkout")
    head = git("rev-parse", "HEAD").decode().strip()
    dirty = set(git("diff", "HEAD", "--name-only", "-z").decode().split("\0")) - {""}
    tracked = git("ls-tree", "-r", "--name-only", "-z", head, "Assets", "Packages", "ProjectSettings", "Docs").decode().split("\0")
    records = []
    output.mkdir(parents=True)
    for name in sorted(set(tracked) | {CAPTURE, CAPTURE + ".meta"}):
        if not name:
            continue
        source = ROOT / name
        if ROOT not in source.resolve().parents:
            raise ValueError("Source escaped checkout")
        if name in dirty:
            data = git("show", f"{head}:{name}")
            origin = "HEAD; unrelated dirty bytes excluded"
            if data.startswith(b"version https://git-lfs.github.com/spec/v1"):
                raise ValueError("Dirty LFS payload requires owner resolution: " + name)
        else:
            data = source.read_bytes()
            origin = "materialized checkout"
        if source.suffix.lower() != ".txt" and data.startswith(b"version https://git-lfs.github.com/spec/v1"):
            raise ValueError("Unmaterialized LFS source: " + name)
        dest = output / name
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        records.append({"path": name, "sha256": digest(data), "bytes": len(data), "origin": origin})
    if git("rev-parse", "HEAD").decode().strip() != head:
        raise RuntimeError("HEAD changed while copying; snapshot is not approved")
    for record in records:
        if record["origin"] == "materialized checkout" and digest((ROOT / record["path"]).read_bytes()) != record["sha256"]:
            raise RuntimeError("Source changed while copying: " + record["path"])
    receipt = {"schema_version": 1, "head": head, "source_root": str(ROOT), "snapshot": str(output),
               "excluded_dirty_files": sorted(dirty), "files": records}
    (output / "snapshot_receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"snapshot": str(output), "head": head, "files": len(records), "excluded_dirty_files": len(dirty)}))

if __name__ == "__main__":
    main()
