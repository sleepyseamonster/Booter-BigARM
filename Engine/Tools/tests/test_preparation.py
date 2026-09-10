import json
import subprocess
import sys
import tempfile
import tarfile
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))
from common import inside
from check_workspace import inspect
from prepare_probe import relative_member


class PreparationChecks(unittest.TestCase):
    def setUp(self):
        (ROOT / ".cache").mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=ROOT / ".cache")
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def record(self, code, extra=None, out="receipt"):
        output = self.root / out
        args = [sys.executable, str(TOOLS / "record.py"), "--out", str(output)]
        args += extra or []
        args += ["--", sys.executable, "-c", code]
        process = subprocess.run(args, capture_output=True, text=True)
        result = json.loads((output / "result.json").read_text()) if (output / "result.json").exists() else None
        return process, result

    def test_output_cannot_escape_engine(self):
        with self.assertRaises(ValueError):
            inside("../escape.json")

    def test_symlink_escape_rejected(self):
        link = self.root / "outside"
        try:
            link.symlink_to(ROOT.parent, target_is_directory=True)
        except OSError:
            self.skipTest("Symlinks not available on this host")
        with self.assertRaises(ValueError):
            inside(link / "escape.json")

    def test_pass_and_input_hash(self):
        source = self.root / "source.txt"
        source.write_text("original")
        process, result = self.record("print('observed')", ["--input", str(source)])
        self.assertEqual(process.returncode, 0)
        self.assertEqual(result["outcome"], "passed")
        self.assertTrue(result["inputs_unchanged"])
        self.assertEqual(len(result["output_sha256"]), 64)

    def test_failure_is_not_success(self):
        process, result = self.record("raise SystemExit(7)")
        self.assertNotEqual(process.returncode, 0)
        self.assertEqual(result["exit_code"], 7)
        self.assertEqual(result["outcome"], "failed")

    def test_timeout_is_not_success(self):
        process, result = self.record("import time; time.sleep(5)", ["--timeout", "0.1"])
        self.assertEqual(process.returncode, 124)
        self.assertEqual(result["outcome"], "timeout")

    def test_launch_failure_has_receipt(self):
        out = self.root / "missing"
        process = subprocess.run([sys.executable, str(TOOLS / "record.py"), "--out", str(out),
                                  "--", str(self.root / "no-such-program")], capture_output=True)
        self.assertNotEqual(process.returncode, 0)
        self.assertEqual(json.loads((out / "result.json").read_text())["outcome"], "launch_error")

    def test_receipt_cannot_be_overwritten(self):
        self.record("print('first')")
        original = (self.root / "receipt/result.json").read_bytes()
        process, _ = self.record("print('second')")
        self.assertNotEqual(process.returncode, 0)
        self.assertEqual((self.root / "receipt/result.json").read_bytes(), original)

    def test_input_mutation_invalidates_success(self):
        source = self.root / "source.txt"
        source.write_text("before")
        process, result = self.record("from pathlib import Path; Path(" + repr(str(source)) + ").write_text('after')",
                                      ["--input", str(source)])
        self.assertEqual(process.returncode, 2)
        self.assertFalse(result["inputs_unchanged"])

    def test_broken_link_and_unity_dependency(self):
        (self.root / "note.md").write_text("[missing](missing.md)\n")
        (self.root / "main.cpp").write_text('#include "UnityEngine.h"\n')
        errors, _, _ = inspect(self.root)
        self.assertEqual(len(errors), 2)

    def test_ignored_cache_is_not_scanned(self):
        folder = self.root / ".cache"
        folder.mkdir()
        (folder / "broken.json").write_text("invalid")
        errors, _, count = inspect(self.root)
        self.assertEqual((errors, count), ([], 0))

    def test_archive_traversal_rejected(self):
        for name in ["/absolute/file", "root/../../outside", "root/..\\outside"]:
            with self.assertRaises(ValueError):
                relative_member(tarfile.TarInfo(name))

    def test_archive_links_rejected(self):
        item = tarfile.TarInfo("root/link")
        item.type = tarfile.SYMTYPE
        with self.assertRaises(ValueError):
            relative_member(item)


if __name__ == "__main__":
    unittest.main()
