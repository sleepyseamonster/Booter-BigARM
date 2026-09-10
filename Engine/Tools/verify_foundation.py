"""Run the native foundation's bounded technical verification and inspect GPU captures.

Uses only Python's standard library. Captures come from our application surface.
This is not a gameplay smoke test or physical-input/Windows performance proof.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys
import zlib
from common import ROOT, inside, write_json


def png(path):
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("Not a PNG: " + str(path))
    offset, compressed = 8, bytearray()
    width = height = channels = 0
    while offset < len(data):
        length = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset+4:offset+8]
        payload = data[offset+8:offset+8+length]
        expected = struct.unpack_from(">I", data, offset+8+length)[0]
        if zlib.crc32(kind+payload) & 0xffffffff != expected:
            raise ValueError("PNG checksum mismatch")
        if kind == b"IHDR":
            width, height, depth, color, compression, filtering, interlace = struct.unpack(">IIBBBBB", payload)
            if depth != 8 or color not in (2, 6) or compression or filtering or interlace:
                raise ValueError("Expected a noninterlaced RGB/RGBA8 capture")
            if not 0 < width <= 8192 or not 0 < height <= 8192:
                raise ValueError("Capture dimensions exceed fixture limits")
            channels = 3 if color == 2 else 4
        elif kind == b"IDAT":
            compressed.extend(payload)
        elif kind == b"IEND":
            break
        offset += 12 + length
    decoded = zlib.decompress(compressed)
    stride = width*channels
    if len(decoded) != (stride+1)*height:
        raise ValueError("Unexpected capture payload size")
    rows, previous, offset = [], bytearray(stride), 0
    for _ in range(height):
        filter_type = decoded[offset]
        row = bytearray(decoded[offset+1:offset+1+stride]); offset += stride+1
        for i in range(stride):
            left = row[i-channels] if i >= channels else 0
            up = previous[i]
            upper_left = previous[i-channels] if i >= channels else 0
            if filter_type == 0:
                predictor = 0
            elif filter_type == 1:
                predictor = left
            elif filter_type == 2:
                predictor = up
            elif filter_type == 3:
                predictor = (left+up)//2
            elif filter_type == 4:
                p = left+up-upper_left
                a, b, c = abs(p-left), abs(p-up), abs(p-upper_left)
                predictor = left if a <= b and a <= c else up if b <= c else upper_left
            else:
                raise ValueError("Unsupported PNG row filter")
            row[i] = (row[i]+predictor) & 255
        rows.append(row); previous = row
    return width, height, channels, rows


def changed_scene(a, b):
    if a[:2] != b[:2]:
        raise ValueError("Comparison captures have different dimensions")
    width, height, ca, ra = a
    _, _, cb, rb = b
    changed, became_blue = 0, 0
    # Exclude the inspector on the left and compare scene pixels only.
    for y in range(height//5, height*9//10, 2):
        for x in range(width*2//5, width*9//10, 2):
            pa, pb = ra[y][x*ca:x*ca+3], rb[y][x*cb:x*cb+3]
            if max(abs(p-q) for p, q in zip(pa, pb)) > 20:
                changed += 1
                if pb[2] > pb[0]+25:
                    became_blue += 1
    return changed, became_blue


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True)
    parser.add_argument("--shaders", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    executable, shaders, out = inside(args.executable), inside(args.shaders), inside(args.out)
    if not executable.is_file() or out.exists():
        parser.error("Executable must exist and output directory must be new")
    command = [sys.executable, str(ROOT/"Tools/record.py"), "--out", str(out/"run"), "--timeout", "120"]
    for path in [executable, ROOT/"CMakeLists.txt", ROOT/"Research/probe-lock.json", Path(__file__)]:
        command += ["--input", str(path)]
    for name in ["vs_scene.bin", "fs_scene.bin", "vs_inspector.bin", "fs_inspector.bin"]:
        path = shaders/name
        if not path.is_file():
            parser.error("Missing compiled shader: " + str(path))
        command += ["--input", str(path)]
    for folder in ["Apps", "Source", "Shaders"]:
        for path in sorted((ROOT/folder).rglob("*")):
            if path.is_file():
                command += ["--input", str(path)]
    command += ["--", str(executable), "--shaders", str(shaders), "--verify", str(out/"captures")]
    subprocess.run(command, cwd=ROOT, check=True)
    report = json.loads((out/"captures/verification.json").read_text())
    if not report["passed"]:
        raise ValueError("Application verification failed")
    captures = {name: png(out/"captures"/(name+".png")) for name in ["baseline", "material", "orbit", "resized"]}
    changed, blue = changed_scene(captures["baseline"], captures["material"])
    orbit_changed, _ = changed_scene(captures["material"], captures["orbit"])
    if changed < 500 or blue < 500 or orbit_changed < 500:
        raise ValueError("GPU pixels do not substantiate the material/camera changes")
    if captures["resized"][:2] == captures["baseline"][:2]:
        raise ValueError("Resize did not change captured framebuffer dimensions")
    negative = []
    for name, arguments, expected in [
        ("missing-shaders", ["--shaders", str(out/"absent-shaders")], "Missing shader:"),
        ("invalid-argument", ["--unknown"], "Usage:")]:
        result = subprocess.run([sys.executable, str(ROOT/"Tools/record.py"), "--out", str(out/name),
            "--timeout", "30", "--input", str(executable), "--", str(executable), *arguments], cwd=ROOT)
        receipt = json.loads((out/name/"result.json").read_text())
        log = (out/name/"output.log").read_text()
        if result.returncode != 1 or receipt["outcome"] != "failed" or expected not in log:
            raise ValueError("Expected useful failure did not occur: " + name)
        if name == "missing-shaders" and "SHUTDOWN renderer resources released" not in log:
            raise ValueError("Shader initialization failure did not clean up renderer")
        negative.append(name)
    result = {"schema_version": 1, "result": "passed", "backend": report["backend"],
        "material_changed_sampled_pixels": changed, "became_blue_sampled_pixels": blue,
        "orbit_changed_sampled_pixels": orbit_changed, "expected_failures": negative,
        "captures": {name: {"width": value[0], "height": value[1],
            "sha256": hashlib.sha256((out/"captures"/(name+".png")).read_bytes()).hexdigest()}
            for name, value in captures.items()},
        "proof_limit": "Native technical checks and GPU pixel changes; not physical input, gameplay, final visual acceptance or target-PC performance."}
    write_json(out/"result.json", result)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    raise SystemExit(main())
