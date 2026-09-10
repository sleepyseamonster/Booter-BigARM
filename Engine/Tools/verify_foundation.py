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


def geometry_comparison(a, b):
    """Measure scene disagreement, ignoring the inspector and small rounding noise."""
    if a[:2] != b[:2]:
        raise ValueError("Comparison captures have different dimensions")
    width, height, ca, ra = a
    _, _, cb, rb = b
    compared = different = normal_subject = 0
    for y in range(height//5, height*4//5, 2):
        for x in range(width*2//5, width*4//5, 2):
            pa, pb = ra[y][x*ca:x*ca+3], rb[y][x*cb:x*cb+3]
            compared += 1
            different += max(abs(p-q) for p, q in zip(pa,pb)) > 3
            # Sloped front surfaces have positive Z and nonvertical Y normals.
            # This excludes the background, upward ground and axis-aligned marker.
            normal_subject += pa[2] > 170 and 135 < pa[1] < 245
    return {"samples": compared, "different": different, "normal_subject": normal_subject}


def check_geometry(captures):
    reference = captures["normals"]
    comparisons = {name: geometry_comparison(reference,captures[name])
        for name in ["baked", "unculled", "front-cull", "reverse-order"]}
    if comparisons["baked"]["normal_subject"] < 500:
        raise ValueError("Missing visible sloped normal reference; empty images cannot prove geometry")
    for name in ["baked", "unculled", "reverse-order"]:
        # Permit narrow silhouette rasterization differences, not a shifted normal field.
        if comparisons[name]["different"] > comparisons[name]["samples"]*0.002:
            raise ValueError("Geometry comparison failed: " + name)
    if comparisons["front-cull"]["different"] < 500:
        raise ValueError("Opposite face culling did not change scene output")
    return comparisons


def check_color(captures):
    linear = [0, .0031308, .2158605, .5, 1, 2, .1, .75]
    def encode(v):
        return round(255*min(1, 12.92*v if v <= .0031308 else 1.055*v**(1/2.4)-.055))
    result = {}
    for name, exposure in [("color-linear", 0), ("color-hdr", -2), ("color-restored", -2)]:
        w, h, c, rows = captures[name]
        # Bottom margin is outside the inspector; every stripe traversed the scene target.
        observed = [list(rows[h-8][int(w*(i+.5)/8)*c:int(w*(i+.5)/8)*c+3]) for i in range(8)]
        expected = [encode(value*2**exposure) for value in linear]
        if any(abs(channel-target)>2 for pixel,target in zip(observed,expected) for channel in pixel):
            raise ValueError(f"Linear/display calibration failed: {name}: {observed}, expected {expected}")
        result[name] = {"observed_rgb": observed, "expected_gray": expected, "exposure": exposure}
    # Opaque inspector title background must remain independent of scene exposure.
    def ui_pixel(name):
        w,h,c,rows=captures[name]
        scale=w/1000
        x,y=round(300*scale),round(30*scale)
        return rows[y][x*c:x*c+3]
    if ui_pixel("color-linear") != ui_pixel("color-hdr"):
        raise ValueError("UI changed with scene exposure")
    return result


def check_textures(captures,catalog):
    def sample(name,u,v):
        w,h,c,rows=captures[name];x,y=int(w*u),int(h*v)
        return list(rows[y][x*c:x*c+3])
    checks=[("texture-color",.4,.25,[255,0,0]),("texture-color",.8,.25,[0,255,0]),
            ("texture-color",.4,.75,[0,0,255]),("texture-color",.8,.75,[255,255,255]),
            ("texture-mip",.6,.6,[188,188,188]),("texture-normal",.4,.5,[128,128,255]),
            ("texture-normal",.8,.5,[255,128,128]),("texture-surface",.7,.5,[128,128,128])]
    observations=[]
    for name,u,v,expected in checks:
        observed=sample(name,u,v)
        if max(abs(a-b) for a,b in zip(observed,expected))>2:
            raise ValueError(f"Texture GPU calibration failed: {name}: {observed}, expected {expected}")
        observations.append({"capture":name,"uv":[u,v],"expected":expected,"observed":observed})
    # Compare original transferred PNG bytes against texture sampling through the display pipeline.
    sources={"texture-rock":"Assets/SurfaceLibrary/Textures/Rocks/Workbench/Layered/RockWorkbenchSide_Albedo.png",
             "texture-ground":"Assets/SurfaceLibrary/Textures/Ground/SandDirt/BrokenWorldSandDirtAlbedo.png"}
    for name,path in sources.items():
        sw,sh,sc,srows=png(ROOT/path);w,h,c,rows=captures[name]
        matched=0
        for u,v in [(0.37,.23),(.43,.64),(.56,.34),(.67,.78),(.79,.43),(.87,.66)]:
            x,y=int(w*u),int(h*v);sx,sy=int((x+.5)*sw/w),int((y+.5)*sh/h)
            expected=srows[sy][sx*sc:sx*sc+3];observed=rows[y][x*c:x*c+3]
            if max(abs(a-b) for a,b in zip(observed,expected))>2:
                raise ValueError(f"Transferred texture pixels disagree: {name}: {list(observed)}, expected {list(expected)}")
            matched+=1
        observations.append({"capture":name,"source":path,"matched_source_samples":matched})
    return {"observations":observations,"catalog_sha256":hashlib.sha256(catalog.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True)
    parser.add_argument("--shaders", required=True)
    parser.add_argument("--portable", action="store_true", help="Use executable-relative shaders and launch from a separate directory")
    parser.add_argument("--out", required=True)
    parser.add_argument("--catalog", help="Cooked texture catalog for native texture verification")
    args = parser.parse_args()
    executable, shaders, out = inside(args.executable), inside(args.shaders), inside(args.out)
    if not executable.is_file() or out.exists():
        parser.error("Executable must exist and output directory must be new")
    command = [sys.executable, str(ROOT/"Tools/record.py"), "--out", str(out/"run"), "--timeout", "120"]
    for path in [executable, ROOT/"CMakeLists.txt", ROOT/"Research/probe-lock.json", ROOT/"Research/runtime-lock.json", Path(__file__)]:
        command += ["--input", str(path)]
    for name in ["vs_scene.bin", "fs_scene.bin", "vs_shadow.bin", "fs_shadow.bin", "vs_inspector.bin", "fs_inspector.bin", "vs_fullscreen.bin", "fs_display.bin", "fs_calibration.bin", "fs_texture_preview.bin"]:
        path = shaders/name
        if not path.is_file():
            parser.error("Missing compiled shader: " + str(path))
        command += ["--input", str(path)]
    if args.catalog:
        catalog=inside(args.catalog)
        command += ["--input",str(catalog)]
        for source in ["Assets/SurfaceLibrary/Textures/Rocks/Workbench/Layered/RockWorkbenchSide_Albedo.png",
                       "Assets/SurfaceLibrary/Textures/Ground/SandDirt/BrokenWorldSandDirtAlbedo.png"]:
            command += ["--input",str(ROOT/source)]
        for row in json.loads(catalog.read_text())["payload"]["textures"]:
            command += ["--input",str(inside(catalog.parent/row["file"]))]
    for folder in ["Apps", "Source", "Shaders"]:
        for path in sorted((ROOT/folder).rglob("*")):
            if path.is_file():
                command += ["--input", str(path)]
    if args.portable:
        cwd=ROOT/".cache/portable-launch-cwd"
        cwd.mkdir(exist_ok=True)
        command += ["--cwd",str(cwd)]
    command += ["--", str(executable), "--verify", str(out/"captures"), "--save-inspection",str(out/"inspection.json")]
    if not args.portable:
        command += ["--shaders",str(shaders)]
    if args.catalog and not (args.portable and catalog == (executable.parent/"Assets/catalog.json").resolve()):
        command += ["--catalog",str(catalog)]
    subprocess.run(command, cwd=ROOT, check=True)
    report = json.loads((out/"captures/verification.json").read_text())
    if not report["passed"]:
        raise ValueError("Application verification failed")
    captures = {name: png(out/"captures"/(name+".png")) for name in
        ["baseline", "material", "orbit", "resized", "normals", "baked", "unculled", "front-cull", "reverse-order", "sphere", "color-linear", "color-hdr", "color-restored"]}
    if args.catalog:
        captures.update({name:png(out/"captures"/(name+".png")) for name in
            ["texture-color","texture-mip","texture-normal","texture-surface","texture-rock","texture-ground"]})
    changed, blue = changed_scene(captures["baseline"], captures["material"])
    orbit_changed, _ = changed_scene(captures["material"], captures["orbit"])
    if changed < 500 or blue < 500 or orbit_changed < 500:
        raise ValueError("GPU pixels do not substantiate the material/camera changes")
    if captures["resized"][:2] == captures["baseline"][:2]:
        raise ValueError("Resize did not change captured framebuffer dimensions")
    geometry = check_geometry(captures)
    color = check_color(captures)
    textures = check_textures(captures,catalog) if args.catalog else None
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
        "orbit_changed_sampled_pixels": orbit_changed, "geometry_comparisons": geometry, "linear_display_checks": color, "texture_checks": textures, "portable_paths": args.portable, "expected_failures": negative,
        "captures": {name: {"width": value[0], "height": value[1],
            "sha256": hashlib.sha256((out/"captures"/(name+".png")).read_bytes()).hexdigest()}
            for name, value in captures.items()},
        "proof_limit": "Native technical checks and GPU pixel changes; not physical input, gameplay, final visual acceptance or target-PC performance."}
    write_json(out/"result.json", result)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    raise SystemExit(main())
