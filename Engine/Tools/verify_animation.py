"""One bounded render-only comparison of GPU skinning with the CPU reference."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
from common import ROOT, inside, write_json
from verify_foundation import png


def compare(a, b):
    if a[:2] != b[:2]:
        raise ValueError("Capture dimensions differ")
    w, h, ca, ra = a
    _, _, cb, rb = b
    changed = samples = 0
    for y in range(0, h, 3):
        for x in range(0, w, 3):
            samples += 1
            changed += max(abs(ra[y][x*ca+c]-rb[y][x*cb+c]) for c in range(3)) > 3
    return {"samples": samples, "changed_over_3": changed}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    model, out = map(inside, (args.model, args.out))
    exe, shaders = ROOT/"build/foundation/engine_workbench", ROOT/"build/foundation/Shaders"
    if out.exists():
        parser.error("Choose a new evidence directory")
    command = [sys.executable, str(ROOT/"Tools/record.py"), "--out", str(out/"run"), "--timeout", "60"]
    for path in [exe, model, model.parent/"mesh.bin", Path(__file__), *sorted(shaders.glob("*.bin"))]:
        command += ["--input", str(path)]
    command += ["--", str(exe), "--shaders", str(shaders), "--model", str(model), "--verify-animation", str(out/"captures")]
    subprocess.run(command, cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
    report = json.loads((out/"captures/animation.json").read_text())["payload"]
    if not report["passed"]:
        raise ValueError("Native capture failed")
    images = {name: png(out/"captures"/(name+".png")) for name in
              ["rest", "gpu-walk", "cpu-walk", "gpu-normals", "cpu-normals"]}
    comparisons = {name: compare(images[a], images[b]) for name, a, b in
                   [("deformation", "rest", "gpu-walk"), ("lit_and_shadow_reference", "gpu-walk", "cpu-walk"),
                    ("normal_reference", "gpu-normals", "cpu-normals")]}
    passed = comparisons["deformation"]["changed_over_3"] > 1000 and all(
        comparisons[name]["changed_over_3"] <= comparisons[name]["samples"]*.0005
        for name in ["lit_and_shadow_reference", "normal_reference"])
    result = {"result": "passed" if passed else "failed", "backend": report["backend"], "comparisons": comparisons,
              "limits": "One calibration proxy on Mac Metal; render-only, no gameplay, Windows or final art acceptance."}
    write_json(out/"result.json", result)
    print(json.dumps(result, indent=2))
    if not passed:
        raise ValueError("Skin deformation/reference comparison failed")


if __name__ == "__main__":
    main()
