"""Read-only SHA-256 verification of every destination in a repository transfer manifest."""

import argparse
import collections
import hashlib
import json
from pathlib import Path


def contained_path(root, relative):
    path = Path(relative)
    if path.is_absolute() or ".." in path.parts:
        raise ValueError(f"Expected a repository-relative path: {relative}")
    resolved = (root / path).resolve()
    if not resolved.is_relative_to(root):
        raise ValueError(f"Path escapes its root: {relative}")
    return resolved


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(4 * 1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", help="JSON manifest path relative to the repository root")
    parser.add_argument("--incoming", action="store_true", help="Also verify the original inventory, including an explicitly retained local holding copy")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[4]
    manifest = json.loads(contained_path(root, args.manifest).read_text(encoding="utf-8"))
    incoming = contained_path(root, manifest.get("retained_original_folder", manifest["incoming_folder"]))
    records = manifest["records"]
    failures = []
    cache = {}
    source_names = set()
    actions = collections.Counter()

    def verify(path, expected_size, expected_hash):
        if not path.is_file() or path.stat().st_size != expected_size:
            raise ValueError(f"Missing file or incorrect size: {path.relative_to(root)}")
        if path not in cache:
            cache[path] = sha256(path)
        if cache[path] != expected_hash:
            raise ValueError(f"Hash mismatch: {path.relative_to(root)}")

    for row in records:
        try:
            if row["source"] in source_names:
                raise ValueError(f"Duplicate source record: {row['source']}")
            source_names.add(row["source"])
            actions[row["action"]] += 1
            verify(contained_path(root, row["destination"]), row["size"], row["sha256"])
            if args.incoming:
                verify(contained_path(incoming, row["source"]), row["size"], row["sha256"])
            if row["action"] == "lfs-reference-archive":
                pointer = contained_path(root, row["destination"]).read_text(encoding="utf-8")
                size = int(pointer.split("size ", 1)[1].strip())
                oid = pointer.split("oid sha256:", 1)[1].splitlines()[0]
                if oid != row["lfs_oid"]:
                    raise ValueError("LFS reference and manifest OIDs differ")
                verify(contained_path(root, row["materialized_source"]), size, oid)
        except (KeyError, OSError, ValueError) as exc:
            failures.append(str(exc))

    if len(records) != manifest["source_file_count"] or sum(row["size"] for row in records) != manifest["source_bytes"]:
        failures.append("Manifest file count or byte total differs from its records")
    if dict(actions) != manifest["actions"]:
        failures.append("Manifest action counts differ from its records")
    if args.incoming:
        actual = {path.relative_to(incoming).as_posix() for path in incoming.rglob("*") if path.is_file()}
        if actual != source_names:
            failures.append("Incoming folder changed or contains files omitted from the manifest")
    for failure in failures:
        print("FAIL", failure)
    if failures:
        raise SystemExit(1)
    print(f"PASS: all {len(records)} transferred files are accounted for with exact SHA-256 hashes.")
    print("Actions:", dict(actions))
    if args.incoming:
        print("PASS: original incoming inventory is unchanged and complete.")


if __name__ == "__main__":
    main()
