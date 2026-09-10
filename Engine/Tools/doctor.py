"""Read-only environment inventory. No installs, environment dumps, or Unity calls."""
import argparse
import json
import platform
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from common import ROOT, write_json


def probe(name, args):
    executable = shutil.which(name)
    if not executable:
        return {"status": "missing", "path": None}
    try:
        result = subprocess.run([executable, *args], cwd=ROOT, capture_output=True,
                                text=True, timeout=15)
        return {"status": "available" if result.returncode == 0 else "probe_failed",
                "path": executable, "exit_code": result.returncode,
                "version_output": (result.stdout + result.stderr).strip()[:1800]}
    except (OSError, subprocess.TimeoutExpired) as error:
        return {"status": "probe_failed", "path": executable, "error": str(error)}


def collect():
    commands = {"git": ["--version"], "cmake": ["--version"], "ninja": ["--version"]}
    if sys.platform == "darwin":
        commands.update({"clang++": ["--version"], "xcodebuild": ["-version"],
                         "xcrun": ["--show-sdk-version"]})
    elif sys.platform == "win32":
        commands.update({"cl": ["/?"], "clang-cl": ["--version"]})
    else:
        commands.update({"c++": ["--version"]})
    tools = {name: probe(name, args) for name, args in commands.items()}
    return {"schema_version": 1, "observed_at_utc": datetime.now(timezone.utc).isoformat(),
            "platform": platform.system(), "os_release": platform.release(),
            "architecture": platform.machine(), "python": platform.python_version(),
            "tools": tools,
            "engine_build_configured": (ROOT / "CMakeLists.txt").exists(),
            "interpretation": "Inventory only; a found compiler is not a successful build. "
                              "Missing Ninja does not preclude another CMake generator. "
                              "Windows MSVC may require a Developer Command Prompt."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", help="Optional JSON destination inside Engine")
    args = parser.parse_args()
    result = collect()
    if args.output:
        write_json(args.output, result)
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
