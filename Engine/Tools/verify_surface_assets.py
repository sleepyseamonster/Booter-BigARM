"""Read-only verification of the transferred rock/ground source collection.

Default checks require only Engine. --sources additionally compares the preserved
Unity source files; it never imports assets or runs Unity.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zlib
from common import ROOT


def confined(base, name):
    path = (base/name).resolve(strict=True)
    path.relative_to(base.resolve())
    if not path.is_file():
        raise ValueError("Expected file: " + str(name))
    return path


def inspect_image(data, row):
    if row["format"] == "PSD":
        if data[:6] != b"8BPS\x00\x01":
            raise ValueError("Invalid PSD header")
        _, height, width, depth, _ = struct.unpack(">HIIHH", data[12:26])
    elif row["format"] == "PNG":
        if data[:8] != b"\x89PNG\r\n\x1a\n":
            raise ValueError("Invalid PNG signature")
        width, height, depth = struct.unpack(">IIB", data[16:25])
        offset, ended = 8, False
        while offset < len(data):
            length = struct.unpack_from(">I", data, offset)[0]
            end = offset+12+length
            if end > len(data):
                raise ValueError("Truncated PNG chunk")
            kind, payload = data[offset+4:offset+8], data[offset+8:offset+8+length]
            expected = struct.unpack_from(">I", data, offset+8+length)[0]
            if zlib.crc32(kind+payload) & 0xffffffff != expected:
                raise ValueError("PNG chunk checksum mismatch")
            offset = end
            if kind == b"IEND":
                ended = True
                break
        if not ended or offset != len(data):
            raise ValueError("Invalid PNG ending")
    else:
        raise ValueError("Unsupported collection format")
    if (width, height, depth) != (row["width"], row["height"], row["bit_depth"]):
        raise ValueError("Image dimensions differ from manifest")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sources", action="store_true")
    args = parser.parse_args()
    library = ROOT/"Assets/SurfaceLibrary"
    manifest = json.loads((library/"manifest.json").read_text())
    reference = json.loads((library/"material-reference.json").read_text())
    if manifest["schema_version"] != 1 or reference["schema_version"] != 1:
        raise ValueError("Unsupported manifest schema")
    keys, paths, counts, total = set(), set(), {}, 0
    for row in manifest["assets"]:
        path = confined(library, row["path"])
        data = path.read_bytes()
        if row["asset_key"] in keys or row["path"] in paths:
            raise ValueError("Duplicate asset key or path")
        keys.add(row["asset_key"]); paths.add(row["path"])
        if len(data) != row["bytes"] or hashlib.sha256(data).hexdigest() != row["sha256"]:
            raise ValueError("Copied asset differs: " + row["path"])
        inspect_image(data, row)
        counts[row["category"]] = counts.get(row["category"], 0)+1
        total += len(data)
    actual = {str(p.relative_to(library)) for p in library.rglob("*") if p.suffix.lower() in (".png", ".psd")}
    if actual != paths or counts != manifest["counts"] or total != manifest["total_bytes"]:
        raise ValueError("Collection inventory differs from manifest")
    binding_count = 0
    for material in reference["materials"]:
        for slot in material["textures"].values():
            if slot["asset_key"] is not None:
                if slot["asset_key"] not in keys:
                    raise ValueError("Unresolved material texture")
                binding_count += 1
    checked_sources = 0
    if args.sources:
        source_root = ROOT.parent/"Assets/_Project"
        for row in manifest["source_records"]:
            name = Path(row["repo_path"]).relative_to("Assets/_Project")
            data = confined(source_root, name).read_bytes()
            if len(data) != row["bytes"] or hashlib.sha256(data).hexdigest() != row["sha256"]:
                raise ValueError("Original source changed: " + row["repo_path"])
            checked_sources += 1
    print(json.dumps({"schema_version": 1, "result": "passed", "assets": len(keys),
        "counts": counts, "bytes": total, "material_records": len(reference["materials"]),
        "resolved_texture_bindings": binding_count, "unchanged_source_files": checked_sources,
        "proof_limit": "Byte integrity, PNG chunk checks/PSD header, inventory and serialized references; not shader porting, full image decoding or engine visual parity."}, indent=2))


if __name__ == "__main__":
    main()
