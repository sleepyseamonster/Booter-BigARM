"""Section-local terrain names must not collide in the atlas selection map."""
import copy
import json
from pathlib import Path
import re
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import audit_coverage


class CoverageAtlasTests(unittest.TestCase):
    def test_same_local_name_in_distinct_sections_selects_each_geographic_record(self):
        catalog = json.loads((audit_coverage.HERE / "coverage_catalog.json").read_text(encoding="utf-8"))
        first = copy.deepcopy(catalog["unity_tiles"][0])
        second = copy.deepcopy(first)
        second.update(section="west_next", bounds_m=[508112, 4014136, 508368, 4014392],
                      geographic_key="epsg26911/e508112/n4014136/size256")
        catalog["unity_tiles"] = [first, second]
        original = copy.deepcopy(catalog)
        with tempfile.TemporaryDirectory(dir="D:/BooterBigArmValidation/Temp") as directory:
            with patch.object(audit_coverage, "HERE", Path(directory)):
                audit_coverage.make_atlas(catalog, None)
            svg = (Path(directory) / "coverage_badwater.svg").read_text(encoding="utf-8")
            page = (Path(directory) / "coverage_atlas.html").read_text(encoding="utf-8")
        records = json.loads(re.search(r"const records=(.*?);function select", page).group(1))
        self.assertEqual(first["legacy_id"], second["legacy_id"])
        self.assertNotIn(first["legacy_id"], records)
        for tile in (first, second):
            key = tile["geographic_key"]
            self.assertEqual(svg.count('data-id="' + key + '"'), 1)
            self.assertEqual(records[key]["bounds_m"], tile["bounds_m"])
            self.assertEqual(records[key]["label"], tile.get("section", "badwater") + "/" + tile["legacy_id"])
        self.assertIn("Unity renders all 2 chunks", page)
        self.assertNotIn("Unity renders all 256 chunks", page)
        self.assertEqual(catalog, original)

    def test_baseline_and_expanded_tile_rectangles_fit_inside_the_detail_view(self):
        catalog = json.loads((audit_coverage.HERE / "coverage_catalog.json").read_text(encoding="utf-8"))
        baseline = [copy.deepcopy(t) for t in catalog["unity_tiles"] if not t.get("section")]
        expanded = []
        template = baseline[0]
        for row in range(32):
            for col in range(64):
                e, n = 508112 + 256 * col, 4006200 + 256 * row
                tile = copy.deepcopy(template)
                tile.update(bounds_m=[e, n, e + 256, n + 256],
                            geographic_key=f"epsg26911/e{e}/n{n}/size256")
                expanded.append(tile)
        far_expanded = list(expanded)
        for row in range(32):
            for col in range(16):
                e, n = 504016 + 256 * col, 4006200 + 256 * row
                tile = copy.deepcopy(template)
                tile.update(bounds_m=[e, n, e + 256, n + 256],
                            geographic_key=f"epsg26911/e{e}/n{n}/size256")
                far_expanded.append(tile)
        ridge_expanded = list(far_expanded)
        for row in range(32):
            for col in range(16):
                e, n = 499920 + 256 * col, 4006200 + 256 * row
                tile = copy.deepcopy(template)
                tile.update(bounds_m=[e, n, e + 256, n + 256],
                            geographic_key=f"epsg26911/e{e}/n{n}/size256")
                ridge_expanded.append(tile)
        for label, tiles in (("baseline", baseline), ("expanded", expanded), ("far_expanded", far_expanded), ("ridge_expanded", ridge_expanded)):
            with self.subTest(coverage=label):
                catalog["unity_tiles"] = tiles
                with tempfile.TemporaryDirectory(dir="D:/BooterBigArmValidation/Temp") as directory:
                    with patch.object(audit_coverage, "HERE", Path(directory)):
                        audit_coverage.make_atlas(catalog, None)
                    svg = ET.fromstring((Path(directory) / "coverage_badwater.svg").read_text(encoding="utf-8"))
                keys = {t["geographic_key"] for t in tiles}
                rects = [r for r in svg.iter() if r.attrib.get("data-id") in keys]
                self.assertEqual(len(rects), len(tiles))
                # The 24px inner margin and geographic 512m padding must leave
                # every geographic rectangle visible, including all four corners.
                for rect in rects:
                    x, y, w, h = (float(rect.attrib[k]) for k in ("x", "y", "width", "height"))
                    self.assertGreaterEqual(x, 24)
                    self.assertGreaterEqual(y, 24)
                    self.assertLessEqual(x + w, 876.001)
                    self.assertLessEqual(y + h, 696.001)
                occupied_width = max(float(r.attrib["x"]) + float(r.attrib["width"]) for r in rects) - min(float(r.attrib["x"]) for r in rects)
                self.assertGreater(occupied_width, 500)


if __name__ == "__main__":
    unittest.main()
