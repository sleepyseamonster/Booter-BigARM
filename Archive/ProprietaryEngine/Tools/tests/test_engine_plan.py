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

    def reset_execution_fixture(self):
        # Keep transition tests independent of future completed batch states.
        for row in self.data["active_execution"]["batches"]:
            row["state"] = "planned"
            row["evidence"] = []

    def test_current_graph_and_coverage(self):
        self.assertEqual(self.inspect()["errors"], [])

    def test_corrections_override_unrelated_local_packages(self):
        self.reset_execution_fixture()
        historical = copy.deepcopy(self.data)
        historical.pop("active_execution")
        historical_result = self.inspect(historical)
        result = self.inspect()
        self.assertEqual(result["ready_local"], ["R1"])
        self.assertEqual(result["active_execution"]["findings"], 15)
        self.assertEqual(result["ready_windows"], historical_result["ready_windows"])
        self.assertEqual(result["deferred_roadmap_local"], historical_result["ready_local"])

    def test_missing_finding_blocks_execution(self):
        self.reset_execution_fixture()
        self.data["active_execution"]["batches"][0]["findings"].remove("A01")
        result = self.inspect()
        self.assertTrue(any("finding coverage" in e for e in result["errors"]))
        self.assertEqual(result["ready_local"], [])

    def test_execution_order_and_completion_require_proof(self):
        self.reset_execution_fixture()
        rows = self.data["active_execution"]["batches"]
        rows[0]["depends_on"] = ["R2"]
        rows[1]["state"] = "complete"
        errors = self.inspect()["errors"]
        self.assertTrue(any("nonpreceding" in e for e in errors))
        self.assertTrue(any("without execution evidence" in e for e in errors))
        self.assertTrue(any("before execution prerequisites" in e for e in errors))

    def test_execution_does_not_erase_windows_gates(self):
        self.reset_execution_fixture()
        self.data["active_execution"]["batches"][-1]["required_gates"] = []
        result = self.inspect()
        self.assertTrue(any("missing required Windows gates" in e for e in result["errors"]))
        self.assertEqual(result["ready_windows"], [])

    def test_corrected_batch_unlocks_next_with_evidence(self):
        self.reset_execution_fixture()
        row = self.data["active_execution"]["batches"][0]
        row["state"] = "complete"
        # Deliberately only file-presence proof: checker cannot certify runtime behavior.
        row["evidence"] = ["Docs/ENGINE_CORRECTION_PLAN.md"]
        result = self.inspect()
        self.assertEqual(result["errors"], [])
        self.assertEqual(result["ready_local"], ["R2"])

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
