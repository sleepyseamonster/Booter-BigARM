"""Reject incomplete or ambiguous two-axis catalog ownership before raster acquisition."""
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from acquire_north_row2_survey import source_catalog
from north_row2_contract import required_products

def catalog(keys):
    return json.dumps({"items": [{"downloadURL": f"https://example.invalid/USGS_1M_11_{k}_CA_FEMAR9Southeast_D24.tif"} for k in keys]}).encode()

class SourceCatalogTests(unittest.TestCase):
    def test_complete_measured_project(self):
        with patch("acquire_north_row2_survey.download", return_value=catalog(sorted(required_products()))):
            url, raw, chosen = source_catalog()
        self.assertEqual(set(chosen), required_products())
        self.assertIn("Digital+Elevation+Model", url)

    def test_missing_western_sliver_and_new_north_axis(self):
        for missing in ("x49y402", "x52y403"):
            with patch("acquire_north_row2_survey.download", return_value=catalog(required_products() - {missing})):
                with self.assertRaises(ValueError):
                    source_catalog()

    def test_duplicate_owner_is_not_silently_selected(self):
        with patch("acquire_north_row2_survey.download", return_value=catalog(list(required_products()) + ["x50y403"])):
            with self.assertRaises(ValueError):
                source_catalog()

if __name__ == "__main__":
    unittest.main()
