"""Focused HTTP sync regression using a disposable workbook, never the master."""
import hashlib
import json
import tempfile
import threading
import unittest
from functools import partial
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import urlopen

import openpyxl
import server


def write_fixture(path, item_name="Test iron", duplicate=False):
    book = openpyxl.Workbook()
    sheet = book.active
    sheet.title = "Items"
    sheet.append(["Item ID", "Item name", "Category", "Tier", "Design status"])
    sheet.append(["test.iron", item_name, "Mineral resources", "T0", "Proposal"])
    if duplicate:
        sheet.append(["test.iron", "Duplicate", "Mineral resources", "T0", "Proposal"])
    book.save(path)
    book.close()


class SpreadsheetSyncTests(unittest.TestCase):
    def test_saved_changes_failed_save_and_recovery(self):
        original = server.WORKBOOK
        with tempfile.TemporaryDirectory(dir=server.ROOT / "Logs") as directory:
            fixture = Path(directory) / "fixture.xlsx"
            write_fixture(fixture)
            server.WORKBOOK = fixture
            http = server.ThreadingHTTPServer(("127.0.0.1", 0), partial(server.Handler, directory=str(server.WEB)))
            worker = threading.Thread(target=http.serve_forever, daemon=True)
            worker.start()
            base = f"http://127.0.0.1:{http.server_port}"

            def get(path):
                with urlopen(base + path) as response:
                    return json.load(response)

            try:
                initial = get("/api/catalog")
                self.assertEqual(initial["sha256"], get("/api/revision")["sha256"])
                write_fixture(fixture, "Updated iron")
                revision = get("/api/revision")["sha256"]
                self.assertNotEqual(revision, initial["sha256"])
                updated = get("/api/catalog")
                self.assertEqual(updated["items"][0]["Item name"], "Updated iron")
                self.assertEqual(updated["sha256"], hashlib.sha256(fixture.read_bytes()).hexdigest())
                fixture.write_bytes(b"save not finished")
                with self.assertRaises(HTTPError) as failed:
                    get("/api/catalog")
                self.assertEqual(failed.exception.code, 500)
                write_fixture(fixture, duplicate=True)
                with self.assertRaises(HTTPError):
                    get("/api/catalog")
                write_fixture(fixture, "Recovered iron")
                self.assertEqual(get("/api/catalog")["items"][0]["Item name"], "Recovered iron")
            finally:
                http.shutdown()
                http.server_close()
                worker.join()
                server.WORKBOOK = original


if __name__ == "__main__":
    unittest.main()
