"""Destructive behavior is tested exclusively on disposable workbook fixtures."""
import json
import tempfile
import unittest
import threading
from functools import partial
from urllib.request import Request, urlopen
from urllib.error import HTTPError
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile

import openpyxl
import workbook_edits as edits
import server


class ItemReviewTests(unittest.TestCase):
    def test_retiring_row_preserves_adjacent_blank_cell_styles(self):
        book=openpyxl.Workbook();sheet=book.active;sheet.title='Items'
        sheet.append(['Item ID','Item name','Tier','Category'])
        sheet.append(['test.retire','Retire me','T0','Armor'])
        sheet['E2'].number_format='0.00';sheet['F2'].number_format='0%'
        sheet['G2']='Retired note';sheet['G2'].font=openpyxl.styles.Font(bold=True)
        snapshot=BytesIO();book.save(snapshot)
        candidate,_=edits.build_deletion(snapshot.getvalue(),'test.retire')
        after=openpyxl.load_workbook(BytesIO(candidate))
        for address in ['E2','F2','G2']:
            self.assertEqual(after['Items'][address].style_id,sheet[address].style_id)
            self.assertIsNone(after['Items'][address].value)
        book.close();after.close()

    def fixture(self, path):
        book = openpyxl.Workbook()
        items = book.active
        items.title = "Items"
        items.append(["Item ID", "Item name", "Tier", "Category"])
        items.append(["test.remove", "Remove me", "T0", "Mineral resources"])
        items.append(["test.retain", "Retain me", "T1", "Equipment"])
        items["B3"].font = openpyxl.styles.Font(bold=True, color="008800")
        routes = book.create_sheet("Material routes")
        routes.append(["Link ID", "Recipe ID", "Relationship", "Item ID"])
        routes.append(["link.1", "recipe.1", "Ingredient candidate", "test.remove"])
        routes.append(["link.2", "recipe.1", "Output", "test.retain"])
        routes.append(["link.3", "recipe.2", "Output", "test.retain"])
        recipes = book.create_sheet("Crafting recipes")
        recipes.append(["Recipe ID", "Recipe name"])
        recipes.append(["recipe.1", "Dependent recipe"])
        recipes.append(["recipe.2", "Unrelated recipe"])
        notes = book.create_sheet("Regional demand")
        notes.append(["Region", "Candidate item IDs"])
        notes.append(["Test region", "test.remove; test.retain"])
        book.save(path)
        book.close()

    def test_keep_blocks_delete_stale_reject_and_exact_deletion(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            path, review, backups = root / "book.xlsx", root / "review.json", root / "backups"
            self.fixture(path)
            original = path.read_bytes()
            sha = edits.digest(original)
            edits.mutate(path, review, backups, "protect", "test.remove", sha, True)
            self.assertEqual(json.loads(review.read_text())["protectedIds"], ["test.remove"])
            with self.assertRaisesRegex(edits.EditError, "kept"):
                edits.mutate(path, review, backups, "delete", "test.remove", sha)
            self.assertEqual(path.read_bytes(), original)
            with self.assertRaisesRegex(edits.EditError, "changed"):
                edits.mutate(path, review, backups, "delete", "test.retain", "stale")
            edits.mutate(path, review, backups, "protect", "test.remove", sha, False)
            result = edits.mutate(path, review, backups, "delete", "test.remove", sha)
            book = openpyxl.load_workbook(path)
            try:
                self.assertIsNone(book["Items"]["A2"].value)
                self.assertEqual(book["Items"]["A3"].value, "test.retain")
                self.assertTrue(book["Items"]["B3"].font.bold)
                self.assertIsNone(book["Material routes"]["A2"].value)
                self.assertIsNone(book["Material routes"]["A3"].value)
                self.assertEqual(book["Material routes"]["A4"].value, "link.3")
                self.assertIsNone(book["Crafting recipes"]["A2"].value)
                self.assertEqual(book["Crafting recipes"]["A3"].value, "recipe.2")
                self.assertEqual(book["Regional demand"]["B2"].value, "[removed item]; test.retain")
                self.assertTrue(book.calculation.fullCalcOnLoad)
            finally:
                book.close()
            self.assertEqual((backups / (sha + ".xlsx")).read_bytes(), original)
            self.assertEqual(result["itemName"], "Remove me")
            with ZipFile(BytesIO(original)) as before, ZipFile(path) as after:
                for member in before.namelist():
                    if not member.startswith("xl/worksheets/") and member != "xl/workbook.xml":
                        self.assertEqual(before.read(member), after.read(member), member)

    def test_review_persists_and_replaces_keep_without_editing_workbook(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            path, review, backups = root / "book.xlsx", root / "review.json", root / "backups"
            self.fixture(path)
            original = path.read_bytes()
            sha = edits.digest(original)
            # Older state files remain readable and retain their keep choices.
            review.write_text(json.dumps({"version": 1, "protectedIds": ["test.remove"]}))
            self.assertEqual(edits.review_state(review)["reviewIds"], [])
            result = edits.mutate(path, review, backups, "review", "test.remove", sha, reviewed=True)
            self.assertEqual(result, {"protectedIds": [], "reviewIds": ["test.remove"]})
            self.assertEqual(edits.review_state(review)["reviewIds"], ["test.remove"])
            with self.assertRaisesRegex(edits.EditError, "Unmark"):
                edits.mutate(path, review, backups, "protect", "test.remove", sha, True)
            with self.assertRaises(edits.EditError):
                edits.mutate(path, review, backups, "review", "test.remove", sha, reviewed="yes")
            with self.assertRaises(edits.EditError):
                edits.mutate(path, review, backups, "review", "test.retain", "stale", reviewed=True)
            edits.mutate(path, review, backups, "review", "test.remove", sha, reviewed=False)
            self.assertEqual(edits.review_state(review)["reviewIds"], [])
            self.assertEqual(edits.review_state(review)["protectedIds"], [])
            self.assertEqual(path.read_bytes(), original)

    def test_current_master_format_in_memory_only(self):
        # Includes shared strings, namespaced XML, cached formula links and tables.
        original = server.WORKBOOK.read_bytes()
        item_id = edits.master_items(edits.parse_sheets(original))[0][1][0]
        updated, result = edits.build_deletion(original, item_id)
        self.assertEqual(server.WORKBOOK.read_bytes(), original)
        items = edits.master_items(edits.parse_sheets(updated))
        self.assertEqual(len(items), len(edits.master_items(edits.parse_sheets(original))) - 1)
        self.assertNotIn(item_id, [row[0] for _, row in items])
        with ZipFile(BytesIO(original)) as before, ZipFile(BytesIO(updated)) as after:
            for member in before.namelist():
                if member.startswith("xl/tables/") or member == "xl/styles.xml":
                    self.assertEqual(before.read(member), after.read(member), member)

    def test_http_write_guards_and_protection(self):
        previous = server.WORKBOOK, server.REVIEW, server.BACKUPS
        with tempfile.TemporaryDirectory(dir=server.ROOT / "Logs") as folder:
            root = Path(folder)
            server.WORKBOOK, server.REVIEW, server.BACKUPS = root / "fixture.xlsx", root / "review.json", root / "backups"
            self.fixture(server.WORKBOOK)
            http = server.ThreadingHTTPServer(("127.0.0.1", 0), partial(server.Handler, directory=str(server.WEB)))
            worker = threading.Thread(target=http.serve_forever, daemon=True)
            worker.start()
            base = f"http://127.0.0.1:{http.server_port}"
            sha = edits.digest(server.WORKBOOK.read_bytes())

            def post(action, token=server.TOKEN, origin=None, **fields):
                body = {"itemId": "test.remove", "sourceHash": sha, **fields}
                request = Request(base + "/api/items/" + action, data=json.dumps(body).encode(),
                                  headers={"Content-Type": "application/json", "Origin": origin or base,
                                           "X-Inventory-Token": token})
                try:
                    with urlopen(request) as response:
                        return response.status, json.load(response)
                except HTTPError as error:
                    return error.code, json.load(error)

            try:
                self.assertEqual(post("delete", token="invalid")[0], 403)
                self.assertEqual(post("delete", origin="http://unrelated.example")[0], 403)
                self.assertEqual(post("protect", protected=True)[0], 200)
                self.assertEqual(post("delete")[0], 409)
                self.assertEqual(post("protect", protected=False)[0], 200)
                status, result = post("review", reviewed=True)
                self.assertEqual(status, 200)
                self.assertEqual(result["reviewIds"], ["test.remove"])
                with urlopen(base + "/api/catalog") as response:
                    self.assertEqual(json.load(response)["reviewIds"], ["test.remove"])
                self.assertEqual(post("protect", protected=True)[0], 409)
                self.assertEqual(post("review", reviewed=False)[1]["reviewIds"], [])
                self.assertEqual(post("delete", sourceHash="outdated")[0], 409)
                self.assertEqual(post("delete")[0], 200)
                self.assertEqual(len(edits.master_items(edits.parse_sheets(server.WORKBOOK.read_bytes()))), 1)
            finally:
                http.shutdown()
                http.server_close()
                worker.join()
                server.WORKBOOK, server.REVIEW, server.BACKUPS = previous


if __name__ == "__main__":
    unittest.main()
