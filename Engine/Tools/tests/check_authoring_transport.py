#!/usr/bin/env python3
"""Technical CLI/Workbench authoring check. Does not initialize SDL or run gameplay."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import socket
import stat
import subprocess
import sys
import tempfile
import time

ENGINE = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ENGINE / "Tools"))
from author_scene import exchange


def wait_endpoint(process, endpoint):
    deadline = time.monotonic() + 4
    while not endpoint.exists():
        if process.poll() is not None or time.monotonic() >= deadline:
            raise RuntimeError("Workbench did not expose authoring endpoint")
        time.sleep(0.01)


def completed(endpoint, identity, request_id):
    deadline = time.monotonic() + 3
    while time.monotonic() < deadline:
        receipt = exchange(str(endpoint), dict(identity, action="receipt", request_id=request_id))
        if receipt["status"] not in ("queued", "preparing"):
            return receipt
        time.sleep(0.01)
    raise RuntimeError("Authoring request did not complete")


def run(binary, output):
    output.mkdir(parents=True, exist_ok=False)
    cli = ENGINE / "Tools/author_scene.py"
    records = {}
    with tempfile.TemporaryDirectory(prefix="ah-", dir=ENGINE / "build") as temporary:
        root = Path(temporary)
        endpoint = root / "s"
        args = [str(binary), "--authoring-socket", str(endpoint), "--authoring-headless-ms", "5000"]
        with (output / "workbench.txt").open("w") as log:
            process = subprocess.Popen(args, cwd=root, stdout=log, stderr=subprocess.STDOUT)
            try:
                wait_endpoint(process, endpoint)
                assert stat.S_IMODE(endpoint.stat().st_mode) == 0o600

                def cli_call(request=None, wait=False):
                    command = [sys.executable, str(cli), "--endpoint", str(endpoint)]
                    if request is not None:
                        source = root / "request.json"
                        source.write_text(json.dumps(request))
                        command += ["--request", str(source)]
                    if wait:
                        command += ["--wait", "3"]
                    result = subprocess.run(command, cwd=root, capture_output=True, text=True, timeout=4)
                    if result.returncode:
                        raise RuntimeError(result.stdout + result.stderr)
                    return json.loads(result.stdout)

                info = cli_call()
                identity = {"epoch": info["epoch"], "document_id": info["document_id"]}
                entity = info["next_entity_id"]
                request = dict(identity, action="submit", request_id=1, expected_revision=info["revision"],
                               operation="apply_transaction", payload={"operations": [
                                   {"type": "create", "name": "Technical proxy"},
                                   {"type": "set_transform", "entity": entity, "transform": {
                                       "translation": [2, 3, 4], "rotation": [0, 0, 0, 1], "scale": [1, 2, 1]}}]})
                result = cli_call(request, wait=True)
                assert result["status"] == "applied"
                inspected = cli_call(dict(identity, action="inspect_entity", revision=result["revision"], entity=entity))
                assert inspected["world_matrix"][12:15] == [2, 3, 4]
                assert cli_call(request) == result
                stale = exchange(str(endpoint), dict(request, epoch="stale-session", request_id=2))
                assert stale["status"] == "rejected"
                # Delivery interruption does not lose the authoritative applied receipt.
                disconnected = dict(request, request_id=2, expected_revision=result["revision"],
                                    payload={"operations": [{"type": "duplicate", "entity": entity}]})
                with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as client:
                    client.connect(str(endpoint))
                    client.sendall(json.dumps(disconnected).encode() + b"\n")
                interrupted = completed(endpoint, identity, 2)
                assert interrupted["status"] == "applied"
                assert exchange(str(endpoint), disconnected) == interrupted
                # Binding a second host must not delete or steal the running socket.
                collision = subprocess.run(args[:-1] + ["100"], cwd=root, capture_output=True, text=True, timeout=3)
                assert collision.returncode == 1 and endpoint.exists()
                assert exchange(str(endpoint), {"action": "describe"})["epoch"] == info["epoch"]
                with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as malformed:
                    malformed.settimeout(3)
                    malformed.connect(str(endpoint))
                    malformed.sendall(b'{"action":"describe","action":"describe"}\n')
                    assert json.loads(malformed.recv(4096))["status"] == "rejected"
                records = {"initial": info, "applied": result, "inspected": inspected, "interrupted_reply": interrupted,
                           "stale_epoch": stale, "final": exchange(str(endpoint), {"action": "describe"})}
                # Incomplete client input cannot hold shutdown hostage.
                with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as slow:
                    slow.connect(str(endpoint))
                    slow.sendall(b'{"action":')
                    assert process.wait(timeout=7) == 0
                assert not endpoint.exists()
            finally:
                if process.poll() is None:
                    process.terminate()
                    process.wait(timeout=3)
            restarted = subprocess.Popen(args[:-1] + ["750"], cwd=root, stdout=log, stderr=subprocess.STDOUT)
            try:
                wait_endpoint(restarted, endpoint)
                new_info = exchange(str(endpoint), {"action": "describe"})
                assert new_info["epoch"] != records["initial"]["epoch"]
                assert exchange(str(endpoint), request)["status"] == "rejected"
                assert restarted.wait(timeout=3) == 0
                assert not endpoint.exists()
                records["restart"] = new_info
            finally:
                if restarted.poll() is None:
                    restarted.terminate()
                    restarted.wait(timeout=3)
    records.update({"passed": True, "binary_sha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
                    "mode": "real workbench binary; bounded headless authoring mode; no SDL/gameplay",
                    "checks": ["CLI transaction/inspection", "retained retry", "stale epoch", "interrupted delivery",
                               "private endpoint", "no endpoint replacement", "duplicate JSON rejection",
                               "shutdown with incomplete client", "fresh restart epoch and socket cleanup"]})
    (output / "transport.json").write_text(json.dumps(records, indent=2) + "\n")
    print(json.dumps({"passed": True, "output": str(output)}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--workbench", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    options = parser.parse_args()
    run(options.workbench.resolve(), options.output.resolve())
