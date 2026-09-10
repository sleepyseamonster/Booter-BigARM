"""Reject broken dependency and completion claims in the roadmap index."""
import copy
import json
from pathlib import Path
import sys
import unittest

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
from check_engine_plan import inspect_plan


class EnginePlanTests(unittest.TestCase):
    def setUp(self):
        self.root = TOOLS.parent.resolve()
        self.data = json.loads((self.root / "Docs/ENGINE_ROADMAP.json").read_text())
        self.master = (self.root / "Docs/FOUNDATION_PLAN.md").read_text()
        self.audit = (self.root / "Research/ENGINE_SYSTEM_AUDIT.md").read_text()

    def inspect(self, data=None):
        return inspect_plan(data or self.data, self.root, self.master, self.audit)

    def test_current_graph_and_coverage(self):
        self.assertEqual(self.inspect()["errors"], [])

    def test_cycle_rejected(self):
        self.data["work_packages"][0]["depends_on"] = ["P02"]
        self.assertTrue(any("cycle" in item for item in self.inspect()["errors"]))

    def test_unknown_prerequisite_rejected(self):
        self.data["work_packages"][0]["depends_on"] = ["P99"]
        self.assertTrue(any("unknown prerequisite" in item for item in self.inspect()["errors"]))

    def test_uncovered_capability_rejected(self):
        for row in self.data["work_packages"]:
            row["capabilities"] = [item for item in row["capabilities"] if item != "CAP-24"]
        self.assertTrue(any("Uncovered" in item for item in self.inspect()["errors"]))

    def test_completion_requires_evidence_and_prerequisites(self):
        next(row for row in self.data["work_packages"] if row["id"] == "P01")["state"] = "planned"
        row = next(row for row in self.data["work_packages"] if row["id"] == "P02")
        row["state"] = "complete"
        row["evidence"] = []  # Exercise missing evidence even after P02 has actually completed.
        errors = self.inspect()["errors"]
        self.assertTrue(any("without evidence" in item for item in errors))
        self.assertTrue(any("before prerequisites" in item for item in errors))

    def test_windows_gate_cannot_be_removed(self):
        row = next(row for row in self.data["work_packages"] if row["id"] == "P34")
        row["depends_on"].remove("W02")
        result = self.inspect()
        self.assertTrue(any("missing required Windows gate" in item for item in result["errors"]))
        self.assertEqual(result["ready_local"], [])

    def test_duplicate_or_undocumented_package_rejected(self):
        duplicate = copy.deepcopy(self.data["work_packages"][0])
        self.data["work_packages"].append(duplicate)
        self.assertTrue(any("Duplicate" in item for item in self.inspect()["errors"]))
        duplicate["id"] = "P99"
        self.assertTrue(any("package rows differ" in item for item in self.inspect()["errors"]))


if __name__ == "__main__":
    unittest.main()
