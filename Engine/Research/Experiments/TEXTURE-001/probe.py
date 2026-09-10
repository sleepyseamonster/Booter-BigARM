"""Offline semantic measurements against the pinned tool; no renderer is launched."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import zlib

ROOT = Path(__file__).resolve().parents[3]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def png(path, width, height, pixel):
    def chunk(kind, data):
        return (struct.pack(">I", len(data)) + kind + data
                + struct.pack(">I", zlib.crc32(kind + data)))
    raw = b"".join(b"\0" + b"".join(bytes(pixel(x, y)) for x in range(width))
                   for y in range(height))
    path.write_bytes(b"\x89PNG\r\n\x1a\n"
                     + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
                     + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", required=True, help="New Engine-relative evidence directory")
    args = parser.parse_args()
    out = (ROOT / args.out).resolve()
    if ROOT not in out.parents or out.exists():
        parser.error("Use a new directory inside Engine")
    out.mkdir(parents=True)
    work = ROOT / "out/texture-research"
    work.mkdir(parents=True, exist_ok=True)
    candidates = list((ROOT / "build/texture-research").rglob("texturec"))
    texturec = next(path for path in candidates if path.is_file())
    inspector = ROOT / "build/texture-research/texture_inspect"
    log = []

    def run(argv, success=True):
        result = subprocess.run([str(x) for x in argv], cwd=ROOT, capture_output=True,
                                text=True, timeout=120)
        log.append({"argv": [str(x) for x in argv], "exit_code": result.returncode,
                    "stdout": result.stdout, "stderr": result.stderr})
        if success and result.returncode:
            raise RuntimeError(log[-1])
        return result

    checker = work / "checker.png"
    normal = work / "normal.png"
    png(checker, 32, 32, lambda x, y: [255 * ((x + y) % 2)] * 3 + [255])
    png(normal, 32, 32, lambda x, y: [204 if (x + y) % 2 else 51, 128, 230, 255])
    cases = []

    def convert(name, source, fmt, flags):
        dest = work / (name + ".ktx")
        run([texturec, "-f", source, "-o", dest, "-t", fmt, "--validate", *flags])
        info = json.loads(run([inspector, dest]).stdout)
        cases.append({"name": name, "input": str(source.relative_to(ROOT)),
                      "source_sha256_before": digest(source), "output_sha256": digest(dest),
                      "output_bytes": dest.stat().st_size, "flags": flags, "decoded": info})
        return cases[-1]

    for fmt in ("RGBA8", "BC7", "BC4"):
        for linear in (False, True):
            convert(fmt.lower() + ("-linear" if linear else "-color"), checker, fmt,
                    ["--mips"] + (["--linear"] if linear else []))
    convert("normal-bc5", normal, "BC5", ["--normalmap", "--mips"])
    ktx2 = work / "ktx2-dispatch.ktx2"
    run([texturec, "-f", checker, "-o", ktx2, "-t", "RGBA8", "--validate"])
    ktx2_dispatch = run([inspector, ktx2], success=False)
    repeat = convert("repeat-bc7-linear", checker, "BC7", ["--mips", "--linear"])
    manifest_path = ROOT / "Assets/SurfaceLibrary/manifest.json"
    manifest = json.loads(manifest_path.read_text())
    textures = [a for a in manifest["assets"] if a["category"] == "texture"]
    chosen = next(a for a in textures if a["role"] == "height" and a["width"] == 1254)
    source = ROOT / "Assets/SurfaceLibrary" / chosen["path"]
    assert digest(source) == chosen["sha256"]
    npot = convert("real-npot-height-bc4", source, "BC4", ["--linear", "--mips"])
    npot["source_sha256_after"] = digest(source)
    assert npot["source_sha256_after"] == chosen["sha256"]
    bad = work / "invalid.png"
    bad.write_bytes(b"not a PNG")
    rejected = run([texturec, "-f", bad, "-o", work / "invalid.ktx2"], success=False)

    def mip_bytes(width, height, bytes_per_pixel=None, block_bytes=None):
        total = 0
        while True:
            total += (width * height * bytes_per_pixel if bytes_per_pixel else
                      ((width + 3) // 4) * ((height + 3) // 4) * block_bytes)
            if width == height == 1:
                return total
            width, height = max(1, width // 2), max(1, height // 2)

    rgba_bytes = sum(mip_bytes(a["width"], a["height"], bytes_per_pixel=4) for a in textures)
    role_bytes = {"base_color": 4, "normal": 4, "packed_surface": 4, "mask": 4, "height": 1}
    uncompressed = sum(mip_bytes(a["width"], a["height"], bytes_per_pixel=role_bytes[a["role"]])
                       for a in textures)
    # Compare the tool's observed block-aligned resize policy, not native NPOT storage.
    compressed = sum(mip_bytes((a["width"] + 3) // 4 * 4, (a["height"] + 3) // 4 * 4,
                               block_bytes=8 if a["role"] == "height" else 16) for a in textures)
    by_name = {case["name"]: case for case in cases}
    linear_checks = {fmt: abs(by_name[fmt + "-linear"]["decoded"]["mips"][1]["mean"][0] - 127.5) <= 2
                     for fmt in ("rgba8", "bc7", "bc4")}
    color_checks = {fmt: abs(by_name[fmt + "-color"]["decoded"]["mips"][1]["mean"][0] - 187.516) <= 2
                    for fmt in ("rgba8", "bc7")}
    deterministic = repeat["output_sha256"] == by_name["bc7-linear"]["output_sha256"]
    assert deterministic and rejected.returncode != 0
    report = {"schema_version": 1, "proof": "Offline CPU conversion and decode only; no GPU or Windows proof",
              "tool_sha256": digest(texturec), "inspector_sha256": digest(inspector),
              "source_lock_sha256": digest(ROOT / "Research/probe-lock.json"),
              "surface_manifest_sha256": digest(manifest_path), "cases": cases,
              "linear_mip_semantic_pass": linear_checks,
              "srgb_mip_semantic_pass": color_checks,
              "repeated_bc7_conversion_identical": deterministic,
              "invalid_input_rejected": rejected.returncode != 0,
              "ktx2_generic_parser_exit": ktx2_dispatch.returncode,
              "memory_estimates": {"png_disk_bytes": sum(a["bytes"] for a in textures),
                                   "all_rgba8_full_mips_bytes": rgba_bytes,
                                   "rgba8_plus_r8_height_full_mips_bytes": uncompressed,
                                   "bc7_bc5_bc4_aligned_full_mips_bytes": compressed,
                                   "scope": "All 38 textures; no allocator overhead, temporary buffers or residency claim"}}
    (out / "measurements.json").write_text(json.dumps(report, indent=2) + "\n")
    (out / "commands.json").write_text(json.dumps({"schema_version": 1, "commands": log}, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "cases"}, indent=2))


if __name__ == "__main__":
    main()
