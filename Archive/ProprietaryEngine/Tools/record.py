"""Run an explicit command and preserve its exit result, logs and input hashes.

The working directory and evidence destination stay inside Engine. This is not a
sandbox for the supplied command. Commands are executed as argv, never a shell string.
"""
import argparse
import hashlib
import os
import signal
import subprocess
import time
from datetime import datetime, timezone
from common import ROOT, inside, write_json


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", required=True, help="New evidence directory inside Engine")
    parser.add_argument("--cwd", default=".")
    parser.add_argument("--timeout", type=float, default=600)
    parser.add_argument("--input", action="append", default=[])
    parser.add_argument("command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    command = args.command[1:] if args.command[:1] == ["--"] else args.command
    if not command or args.timeout <= 0:
        parser.error("An explicit command and positive timeout are required")
    try:
        directory, cwd = inside(args.out), inside(args.cwd)
        inputs = [inside(value) for value in args.input]
        before = {str(path.relative_to(ROOT)): digest(path) for path in inputs}
    except (ValueError, OSError) as error:
        parser.error(str(error))
    if directory.exists():
        parser.error("Evidence directory already exists; use a new name")
    if not cwd.is_dir():
        parser.error("Working directory does not exist")
    head = git("rev-parse", "HEAD")
    dirty = git("status", "--porcelain=v1", "--", ".").splitlines()
    directory.mkdir(parents=True)
    started = datetime.now(timezone.utc).isoformat()
    start = time.monotonic()
    code, outcome = None, "launch_error"
    with (directory / "output.log").open("wb") as log:
        try:
            process = subprocess.Popen(command, cwd=cwd, stdout=log, stderr=subprocess.STDOUT,
                                       start_new_session=os.name != "nt")
            try:
                code = process.wait(timeout=args.timeout)
                outcome = "passed" if code == 0 else "failed"
            except subprocess.TimeoutExpired:
                if os.name != "nt":
                    os.killpg(process.pid, signal.SIGKILL)
                else:
                    process.kill()
                process.wait()
                outcome = "timeout"
        except OSError as error:
            log.write((str(error) + "\n").encode())
    after = {str(path.relative_to(ROOT)): digest(path) if path.is_file() else None
             for path in inputs}
    result = {"schema_version": 1, "started_at_utc": started,
              "duration_seconds": round(time.monotonic() - start, 3),
              "git_head": head, "dirty_engine_at_start": dirty,
              "command": command, "cwd_relative_to_engine": str(cwd.relative_to(ROOT)),
              "outcome": outcome, "exit_code": code, "timeout_seconds": args.timeout,
              "input_sha256_before": before, "input_sha256_after": after,
              "inputs_unchanged": before == after,
              "output_sha256": digest(directory / "output.log"),
              "proof_limit": "Process result only. Inspect output and semantic assertions; "
                             "this does not establish visual quality or GPU performance."}
    write_json(directory / "result.json", result)
    print(outcome + ": " + str(directory.relative_to(ROOT)))
    if before != after:
        return 2
    return 0 if outcome == "passed" else 124 if outcome == "timeout" else 1


if __name__ == "__main__":
    raise SystemExit(main())
