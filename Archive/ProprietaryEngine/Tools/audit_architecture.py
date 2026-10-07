#!/usr/bin/env python3
"""Run bounded CPU architecture audit probes. Exit success means evidence collected, not engine acceptance."""
import argparse
import hashlib
import json
import platform
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ["Tests/ArchitectureAuditProbe.cpp", "Source/Authoring/SceneDocument.cpp",
           "Source/Authoring/SceneAuthoringAdapter.cpp", "Source/Persistence/Document.cpp"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT) or output.exists():
        parser.error("output must be a new path inside Engine")
    build = ROOT / "build/architecture-audit"
    build.mkdir(parents=True, exist_ok=True)
    binary = build / "architecture_audit_probe"
    command = ["clang++", "-std=c++20", "-O2", "-Wall", "-Wextra", "-I", "Source", "-isystem",
               ".cache/probe-sources/nlohmann_json/single_include", *SOURCES, "-o", str(binary)]
    subprocess.run(command, cwd=ROOT, check=True, timeout=120)
    result = subprocess.run([str(binary)], cwd=ROOT, check=True, text=True, capture_output=True, timeout=60)
    observed = json.loads(result.stdout)
    tracked_inputs = [*SOURCES, "Source/Authoring/SceneDocument.h", "Source/Authoring/SceneAuthoringAdapter.h",
                      "Source/Runtime/AuthoringOperations.h", "Source/Persistence/Document.h", "Research/runtime-lock.json"]
    evidence = {"purpose": "Observational audit; reproduced_issue=true identifies a defect, not a passing engine gate",
                "source_commit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
                "platform": platform.platform(), "compiler": subprocess.check_output(["clang++", "--version"], text=True).splitlines()[0],
                "command": command, "input_sha256": {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in tracked_inputs},
                "observations": observed}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(evidence, indent=2) + "\n")
    print(json.dumps(observed, indent=2))


if __name__ == "__main__":
    main()
