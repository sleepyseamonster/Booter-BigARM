"""Focused EditMode verification bound to exact author/candidate inputs."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(author, candidate, code_manifest, input_manifest, output):
    output.mkdir(exist_ok=False)
    code = json.loads(code_manifest.read_text())
    inputs = json.loads(input_manifest.read_text(encoding="utf-8-sig"))
    previous = json.loads((author / "Docs/Evidence/Gameplay/LocomotionImplementation20261014/CampMotionContinuation20261015/verification.json").read_text())
    fixtures = [r["fixture"] for r in previous["fixtures"]]
    fixtures += ["AnimationPlaygroundDirectionalTests", "AnimationPlaygroundNativeGroundTests", "AnimationPlaygroundGarmentFloorTests"]
    if len(fixtures) != len(set(fixtures)):
        raise ValueError("Duplicate fixture.")
    cli = Path(os.environ["LOCALAPPDATA"]) / "Unity/bin/unity.exe"

    def check():
        for row in code:
            for root in (author, candidate):
                if sha(root / row["path"]) != row["sha256"]:
                    raise ValueError(f"Changed paired source: {root / row['path']}")
        for row in inputs["inputs"]:
            if sha(candidate / row["path"]) != row["sha256"]:
                raise ValueError(f"Changed candidate input: {row['path']}")

    def command(*arguments):
        result = subprocess.run([str(cli), "command", *arguments, "--timeout", "30", "--project-path", str(candidate), "--json"],
                                text=True, encoding="utf-8", errors="replace", capture_output=True, timeout=45)
        value = json.loads(result.stdout)
        if result.returncode or not value.get("success"):
            raise RuntimeError(result.stdout + result.stderr)
        data = value["data"]["result"]
        if data.get("success") is False:
            raise RuntimeError(json.dumps(data))
        return value

    check()
    records = []
    for fixture in fixtures:
        check()
        prefix = "BooterBigArm.Tests." + fixture
        started = command("run_tests", "--mode", "editor", "--filter", prefix, "--filter_type", "testName", "--async_tests", "true")
        if started["data"]["result"].get("result") != "running":
            raise RuntimeError("Expected an asynchronous EditMode run.")
        deadline = time.monotonic() + 600
        while True:
            time.sleep(2)
            value = command("test_status")
            data = value["data"]["result"]
            if data.get("status") == "running":
                if time.monotonic() > deadline:
                    raise TimeoutError(fixture)
                continue
            if data.get("status") != "completed":
                raise RuntimeError(json.dumps(data))
            results = data.get("results", [])
            if not results or not all(r["FullName"].startswith((prefix + ".", prefix + "(")) for r in results):
                raise RuntimeError("The receipt does not match the requested fixture.")
            receipt = output / ("tests-" + fixture + ".json")
            receipt.write_bytes((json.dumps(value, indent=2) + "\n").encode())
            summary = data["summary"]
            records.append(dict(fixture=fixture, **summary, receipt=receipt.name, sha256=sha(receipt)))
            print(f"{fixture}: {summary['passed']}/{summary['total']} passed, {summary['failed']} failed, {summary['skipped']} skipped", flush=True)
            if summary["failed"] or summary["skipped"] or summary.get("inconclusive") or summary["total"] != summary["passed"]:
                raise RuntimeError("Resolve this fixture before continuing publication verification.")
            break
    check()
    result = dict(state="verified-source-bound-editmode-motion-review", fixtures=records,
                  total=sum(r["total"] for r in records), passed=sum(r["passed"] for r in records),
                  codeManifestSha256=sha(code_manifest), inputManifestSha256=sha(input_manifest),
                  scope="EditMode unit, owner, integration and offline equipped pose checks only. No Play Mode, physics stepping or gameplay smoke test.")
    (output / "verification.json").write_bytes((json.dumps(result, indent=2) + "\n").encode())
    print("COMPLETE", result["passed"], flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--author", type=Path, required=True)
    parser.add_argument("--candidate", type=Path, required=True)
    parser.add_argument("--code-manifest", type=Path, required=True)
    parser.add_argument("--input-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    run(args.author, args.candidate, args.code_manifest, args.input_manifest, args.output)
