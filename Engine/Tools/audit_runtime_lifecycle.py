#!/usr/bin/env python3
"""Bounded Mac CPU audit; successful evidence collection is not engine acceptance."""
import argparse
import hashlib
import json
from pathlib import Path
import platform
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if platform.system() != "Darwin":
        parser.error("This diagnostic runner uses the existing Mac build/core libraries")
    if not output.is_relative_to(ROOT) or output.exists():
        parser.error("output must be a new path inside Engine")
    build = ROOT / "build/core"
    rebuild = ["cmake", "--build", str(build), "--target", "engine_snapshot_tests", "engine_physics_tests", "-j2"]
    built = subprocess.run(rebuild, cwd=ROOT, check=True, text=True, capture_output=True, timeout=120)
    libraries = ["libengine_game.a", "libengine_physics.a", "jolt/libJolt.a", "libengine_simulation.a",
                 "libengine_animation.a", "libengine_texture_data.a", "libengine_core.a",
                 "ozz/src/animation/offline/libozz_animation_offline.a",
                 "ozz/src/animation/runtime/libozz_animation.a", "ozz/src/base/libozz_base.a"]
    with tempfile.TemporaryDirectory(prefix="lifecycle-audit-", dir=ROOT / "build") as temporary:
        scratch = Path(temporary)
        command = ["clang++", "-std=c++20", "-O2", "-DNDEBUG", "-Wall", "-Wextra", "-pthread",
                   "-I", "Source", "-isystem", ".cache/probe-sources/nlohmann_json/single_include",
                   "Tests/RuntimeLifecycleAuditProbe.cpp", *[str(build / p) for p in libraries],
                   "-framework", "Foundation", "-framework", "Metal", "-framework", "MetalKit",
                   "-o", str(scratch / "probe")]
        subprocess.run(command, cwd=ROOT, check=True, timeout=60)
        probe = subprocess.run([str(scratch / "probe"), str(scratch / "profile")], cwd=ROOT,
                               check=True, text=True, capture_output=True, timeout=30)
        checks = {}
        for target, arguments in [("engine_physics_tests", []),
                                  ("engine_snapshot_tests", [str(scratch / "snapshot-contract")])]:
            checked = subprocess.run([str(build / target), *arguments], cwd=ROOT, check=True,
                                     text=True, capture_output=True, timeout=30)
            checks[target] = {"exit_code": checked.returncode, "stdout": checked.stdout}
    paths = ["Tests/RuntimeLifecycleAuditProbe.cpp", "Tools/audit_runtime_lifecycle.py",
             "Source/Persistence/PlayerSnapshot.cpp", "Source/Persistence/Document.cpp",
             "Source/Physics/PhysicsWorld.cpp", "Source/Physics/PhysicsWorld.h",
             "Source/Game/CalibrationRuntime.cpp", "Source/Simulation/FixedClock.h",
             "Source/Rendering/StreamingScene.cpp", "Research/runtime-lock.json",
             ".cache/probe-sources/jolt/Jolt/Physics/Body/BodyInterface.cpp"]
    evidence = {"purpose": "Observational audit; reproduced_issue=true identifies a defect",
                "source_commit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
                "platform": platform.platform(), "compiler": subprocess.check_output(["clang++", "--version"], text=True).splitlines()[0],
                "rebuild_command": rebuild, "rebuild_output": built.stdout, "probe_command": command,
                "input_sha256": {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in paths},
                "observations": json.loads(probe.stdout), "existing_contract_checks": checks}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(evidence, indent=2) + "\n")
    print(json.dumps(evidence["observations"], indent=2))


if __name__ == "__main__":
    main()
