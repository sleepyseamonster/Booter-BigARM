"""Loopback-only, read-only workbook adapter and static prototype server."""
import argparse
import hashlib
import json
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

import openpyxl

ROOT = Path(__file__).resolve().parents[2]
WEB = Path(__file__).resolve().parent / "dist"
WORKBOOK = ROOT / "Docs/Design/Items/MASTER_ITEM_DATABASE.xlsx"
REFERENCE = ROOT / "Docs/Agents/Sable/work/2026-10-15-material-dioramas/refined_BC/yard_E_sunset_close.jpg"


def catalog():
    book = openpyxl.load_workbook(WORKBOOK, read_only=True, data_only=True)
    try:
        rows = iter(book["Items"].values)
        headers = next(row for row in rows if row and row[0] == "Item ID")
        items = [dict((key, value) for key, value in zip(headers, row) if key)
                 for row in rows if row and row[0]]
        ids = [item["Item ID"] for item in items]
        if len(ids) != len(set(ids)):
            raise ValueError("Duplicate item IDs in master workbook")
        return {"items": items, "source": str(WORKBOOK.relative_to(ROOT)),
                "sha256": hashlib.sha256(WORKBOOK.read_bytes()).hexdigest()}
    finally:
        book.close()


class Handler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        super().end_headers()

    def do_GET(self):
        path = self.path.split("?", 1)[0]
        if path == "/api/catalog":
            try:
                payload = json.dumps(catalog(), ensure_ascii=False, default=str).encode("utf-8")
            except Exception as error:
                self.send_error(500, f"Workbook could not be read: {type(error).__name__}")
                return
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        elif path == "/material-reference.jpg":
            if not REFERENCE.exists():
                self.send_error(404)
                return
            payload = REFERENCE.read_bytes()
            self.send_response(200)
            self.send_header("Content-Type", "image/jpeg")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        else:
            super().do_GET()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8087)
    args = parser.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", args.port), partial(Handler, directory=str(WEB)))
    print(f"Inventory visualizer: http://127.0.0.1:{args.port}", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
