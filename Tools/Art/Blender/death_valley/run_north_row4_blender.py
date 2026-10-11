"""Hidden native launcher for six-step fourth-row authoring; no old-row dispatch changes."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import time
from north_row4_contract import contract

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--blender", type=Path, required=True)
    p.add_argument("--step", type=int, required=True)
    p.add_argument("--date", required=True)
    p.add_argument("--manifest", type=Path, required=True)
    p.add_argument("--mode", choices=("build", "verify", "render"), required=True)
    p.add_argument("--section", required=True)
    p.add_argument("--output", type=Path, required=True)
    p.add_argument("--log", type=Path, required=True)
    p.add_argument("--blend", type=Path)
    p.add_argument("--stride", type=int, choices=(2, 4), default=2)
    p.add_argument("--view", choices=("overhead", "oblique", "junction"), default="oblique")
    a = p.parse_args()
    c = contract(a.step, a.date)
    output, log = a.output.resolve(), a.log.resolve()
    if output.exists() or log.exists() or not a.blender.is_file() or not a.manifest.is_file():
        raise ValueError("Fresh outputs and working runtime/manifest required")
    if not output.is_relative_to(c["art"] if a.mode == "build" else c["data"]):
        raise ValueError("Output must stay inside its fresh source or proof owner")
    cmd = [str(a.blender.resolve()), "--background", "--factory-startup", "--disable-autoexec", "--python-exit-code", "1"]
    if a.mode != "build":
        if a.blend is None or not a.blend.is_file() or not a.blend.resolve().is_relative_to(c["art"]):
            raise ValueError("Reopen only a saved source in this fresh section owner")
        cmd.append(str(a.blend.resolve()))
    cmd += ["--python", str(Path(__file__).with_name("build_north_row4_scene.py").resolve()), "--", "--step", str(a.step), "--date", a.date, "--manifest", str(a.manifest.resolve()), "--mode", a.mode, "--section", a.section, "--output", str(output), "--stride", str(a.stride), "--view", a.view]
    env = dict(os.environ, TEMP="D:/BooterBigArmTools/Temp", TMP="D:/BooterBigArmTools/Temp")
    log.parent.mkdir(parents=True, exist_ok=True)
    start = time.monotonic()
    with log.open("x", encoding="utf-8") as handle:
        result = subprocess.run(cmd, stdout=handle, stderr=subprocess.STDOUT, env=env, creationflags=subprocess.CREATE_NO_WINDOW, timeout=3600)
    if result.returncode != 0 or not output.is_file():
        raise RuntimeError("Native authoring failed; inspect " + str(log))
    print(json.dumps({"mode": a.mode, "section": a.section, "seconds": time.monotonic() - start, "output": str(output), "bytes": output.stat().st_size, "exit_code": result.returncode}))
