"""Fetch/verify the exact EXP-001 archives and prepare ignored source directories.

No Git operations, global installs, or mutable branch resolution. Existing extracted
sources must match the archives; local changes are never overwritten.
"""
import argparse
import hashlib
import json
import re
import tarfile
import urllib.request
from pathlib import Path, PurePosixPath
from common import ROOT, inside, write_json


def sha(data):
    return hashlib.sha256(data).hexdigest()


def relative_member(member):
    parts = PurePosixPath(member.name).parts
    if not parts or member.name.startswith("/") or ".." in parts or "\\" in member.name:
        raise ValueError("Unsafe archive path: " + member.name)
    if member.issym() or member.islnk() or not (member.isdir() or member.isfile()):
        raise ValueError("Unsupported archive member: " + member.name)
    return Path(*parts[1:]) if len(parts) > 1 else None


def prepare(package, fetch):
    if not re.fullmatch(r"[a-z0-9_]+", package["name"]):
        raise ValueError("Invalid package name")
    if not re.fullmatch(r"[0-9a-f]{40}", package["revision"]):
        raise ValueError("Expected immutable Git revision")
    cache = inside(".cache/downloads")
    cache.mkdir(parents=True, exist_ok=True)
    archive = inside(cache / (package["name"] + "-" + package["revision"] + ".tar.gz"))
    if not archive.exists():
        if not fetch:
            raise ValueError("Archive missing; rerun with --fetch: " + package["name"])
        if not package["url"].startswith("https://codeload.github.com/"):
            raise ValueError("Unexpected archive host")
        with urllib.request.urlopen(package["url"], timeout=60) as response:
            data = response.read()
        if sha(data) != package["sha256"]:
            raise ValueError("Downloaded archive hash mismatch: " + package["name"])
        archive.write_bytes(data)
    if sha(archive.read_bytes()) != package["sha256"]:
        raise ValueError("Cached archive hash mismatch: " + package["name"])
    destination = inside(ROOT / ".cache/probe-sources" / package["name"])
    existing = destination.exists()
    expected = {}
    with tarfile.open(archive, "r:gz") as source:
        members = [(item, relative_member(item)) for item in source.getmembers()]
        for item, relative in members:
            if relative is None or item.isdir():
                continue
            name = relative.as_posix()
            if name in expected:
                raise ValueError("Duplicate archive file: " + name)
            target = destination / relative
            if destination.resolve() not in target.resolve().parents:
                raise ValueError("Extraction path escapes source root")
            with source.extractfile(item) as stream:
                data = stream.read()
            expected[name] = sha(data)
            if existing:
                if target.is_symlink() or not target.is_file() or sha(target.read_bytes()) != expected[name]:
                    raise ValueError("Existing source differs; preserve/inspect it: " + str(target))
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
                target.chmod(item.mode & 0o777)
    actual = {str(path.relative_to(destination)).replace("\\", "/")
              for path in destination.rglob("*") if path.is_file()}
    if actual != set(expected):
        raise ValueError("Unexpected files in prepared source: " + package["name"])
    tree = "".join(name + "\0" + digest + "\n" for name, digest in sorted(expected.items()))
    return {"name": package["name"], "revision": package["revision"],
            "archive_sha256": package["sha256"], "file_count": len(expected),
            "content_tree_sha256": sha(tree.encode()), "verified": True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fetch", action="store_true", help="Allow downloads into Engine/.cache")
    parser.add_argument("--output", default=".cache/probe-sources/inventory.json")
    args = parser.parse_args()
    lock = json.loads((ROOT / "Research/probe-lock.json").read_text())
    try:
        packages = [prepare(package, args.fetch) for package in lock["packages"]]
        result = {"schema_version": 1, "lock_sha256": sha((ROOT / "Research/probe-lock.json").read_bytes()),
                  "packages": packages, "proof_limit": "Archive/content verification only; not a build or license clearance"}
        write_json(args.output, result)
    except (ValueError, OSError) as error:
        parser.error(str(error))
    print("Verified " + str(len(packages)) + " exact source archives and extracted trees")


if __name__ == "__main__":
    raise SystemExit(main())
