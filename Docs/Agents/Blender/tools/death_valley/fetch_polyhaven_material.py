"""Fetch one optional CC0 Poly Haven material from a saved API file manifest.

Save https://api.polyhaven.com/files/<asset-id> to JSON first. This script uses
that snapshot and checks the provider's MD5 for every downloaded file.
The API Terms of Service require a small 'Powered by Poly Haven' credit when
building on the live API. The assets themselves are CC0.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
from pathlib import Path


CHOICES = {"Diffuse": "jpg", "nor_gl": "jpg", "Rough": "jpg", "Displacement": "png"}


def md5(path: Path) -> str:
    digest = hashlib.md5()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asset-id", required=True)
    parser.add_argument("--files-json", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    listing = json.loads(args.files_json.read_text())
    args.output.mkdir(parents=True, exist_ok=True)
    results = {}
    for map_name, extension in CHOICES.items():
        item = listing[map_name]["1k"][extension]
        source = item["url"]
        if not source.startswith("https://dl.polyhaven.org/file/ph-assets/"):
            raise RuntimeError(f"Unexpected provider URL: {source}")
        destination = args.output / f"{args.asset_id}_{map_name.lower()}.{extension}"
        if not destination.exists():
            temp = destination.with_suffix(destination.suffix + ".download")
            subprocess.run(["curl", "-fL", "--silent", "--show-error", source, "-o", str(temp)], check=True)
            temp.replace(destination)
        if md5(destination) != item["md5"]:
            raise RuntimeError(f"MD5 mismatch: {destination}")
        results[map_name] = {"path": destination.name, "url": source, "md5": item["md5"], "bytes": destination.stat().st_size}
    manifest = {"asset_id": args.asset_id, "asset_page": f"https://polyhaven.com/a/{args.asset_id}",
                "license": "CC0", "api_credit": "Powered by Poly Haven", "resolution": "1k", "maps": results}
    (args.output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps({"asset": args.asset_id, "files": list(results), "bytes": sum(value["bytes"] for value in results.values())}))


if __name__ == "__main__":
    main()
