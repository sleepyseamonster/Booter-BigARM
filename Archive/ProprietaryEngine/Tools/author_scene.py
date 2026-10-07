#!/usr/bin/env python3
"""Bounded local authoring client; select an endpoint explicitly. One JSON line/request."""
import argparse
import json
import socket
import sys
import time

REQUEST_BYTES = 256 * 1024
RESULT_BYTES = 64 * 1024


def exchange(endpoint, request):
    wire = json.dumps(request, separators=(",", ":"), allow_nan=False).encode("utf-8")
    if len(wire) > REQUEST_BYTES:
        raise ValueError("Request exceeds 256 KiB")
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as client:
        client.settimeout(3)
        client.connect(endpoint)
        client.sendall(wire + b"\n")
        response = bytearray()
        while b"\n" not in response:
            part = client.recv(min(4096, RESULT_BYTES + 2 - len(response)))
            if not part:
                raise RuntimeError("Endpoint closed without a complete receipt; retry the same request ID")
            response.extend(part)
            if len(response) > RESULT_BYTES + 1:
                raise ValueError("Response exceeds 64 KiB")
    return json.loads(response)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--endpoint", required=True, help="Absolute workbench Unix socket path")
    parser.add_argument("--request", help="JSON request file; omit for describe, use - for stdin")
    parser.add_argument("--wait", type=float, default=0, help="Poll admitted request for up to this many seconds (max 60)")
    args = parser.parse_args()
    if not 0 <= args.wait <= 60:
        parser.error("--wait must be 0..60 seconds")
    if args.request == "-":
        wire = sys.stdin.buffer.read(REQUEST_BYTES + 1)
    elif args.request:
        with open(args.request, "rb") as source:
            wire = source.read(REQUEST_BYTES + 1)
    else:
        wire = b'{"action":"describe"}'
    if len(wire) > REQUEST_BYTES:
        raise ValueError("Request exceeds 256 KiB")
    request = json.loads(wire)
    response = exchange(args.endpoint, request)
    deadline = time.monotonic() + args.wait
    while response.get("status") in ("queued", "preparing", "cancellation_requested") and time.monotonic() < deadline:
        time.sleep(0.02)
        response = exchange(args.endpoint, {"action": "receipt", "epoch": request["epoch"],
                            "document_id": request["document_id"], "request_id": request["request_id"]})
    print(json.dumps(response, indent=2, allow_nan=False))
    return 1 if response.get("status") in ("rejected", "expired_or_unknown", "request_id_conflict") else 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, RuntimeError) as error:
        print(json.dumps({"status": "client_error", "error": str(error)}), file=sys.stderr)
        sys.exit(2)
