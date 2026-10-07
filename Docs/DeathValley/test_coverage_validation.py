"""Focused offline regressions for source evidence and acquisition preservation."""
import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from coverage_validation import assessment_availability, fresh_output, source_records, validate_boundary, validate_terrain_contract

HERE = Path(__file__).resolve().parent


class CoverageValidationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="DeathValleyCoverageTests-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.record = json.loads((HERE / "west_source_assessment.json").read_text(encoding="utf-8"))
        self.bounds = self.record["bounds_m"]

    def materialize(self):
        fields = [(self.record, "grid")]
        fields.extend((source, name) for source in self.record["sources"] for name in ("window", "metadata"))
        for container, name in fields:
            path = self.root / container[name + "_path"]
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes((name + " fixture").encode())
            container[name + "_sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()

    def test_fresh_clone_reports_missing_local_inputs(self):
        result = assessment_availability(self.record, self.bounds, self.root)
        self.assertEqual(result["status"], "unavailable")
        self.assertEqual(len(result["missing_artifacts"]), 5)

    def test_available_inputs_require_all_five_hashes(self):
        self.materialize()
        self.assertEqual(assessment_availability(self.record, self.bounds, self.root)["status"], "hash_verified")
        (self.root / self.record["sources"][1]["metadata_path"]).write_bytes(b"changed")
        with self.assertRaisesRegex(ValueError, "artifact changed"):
            assessment_availability(self.record, self.bounds, self.root)

    def test_durable_candidate_archive_works_without_ignored_logs(self):
        self.materialize()
        pairs=[(self.record,"grid")]
        pairs.extend((source,name) for source in self.record["sources"] for name in ("window","metadata"))
        folder=self.root/"SourceData/Terrain/DeathValley/WestCandidate2026-10-06";folder.mkdir(parents=True)
        records=[]
        for container,name in pairs:
            old=container[name+"_path"];new=folder/Path(old).name
            (self.root/old).replace(new)
            records.append({"source":old,"destination":new.relative_to(self.root).as_posix(),"sha256":container[name+"_sha256"]})
        manifest=folder/"manifest.json";manifest.write_text(json.dumps({"records":records}))
        result=assessment_availability(self.record,self.bounds,self.root)
        self.assertEqual(result["status"],"hash_verified")
        self.assertTrue(all(p.startswith("SourceData/") for p in result["resolved_artifacts"]))
        records[0]["destination"]="../outside.npz";manifest.write_text(json.dumps({"records":records}))
        with self.assertRaisesRegex(ValueError,"escapes"):
            assessment_availability(self.record,self.bounds,self.root)

    def test_different_geography_does_not_inherit_verification(self):
        bounds = self.bounds.copy()
        bounds[0] -= 256
        with self.assertRaisesRegex(ValueError, "geography"):
            assessment_availability(self.record, bounds, self.root)

    def test_missing_samples_or_failed_border_are_rejected(self):
        for change in ({"nodata_count": 1}, {"retained_boundary_max_difference_m": 1.0}, {"shape": [129, 129]}):
            record = copy.deepcopy(self.record)
            record.update(change)
            with self.assertRaises(ValueError):
                assessment_availability(record, self.bounds, self.root)

    def test_artifact_paths_cannot_escape_checkout(self):
        self.record["grid_path"] = "../outside.npz"
        with self.assertRaisesRegex(ValueError, "escapes"):
            assessment_availability(self.record, self.bounds, self.root)

    def test_source_order_is_independent_of_catalog_order(self):
        records = json.loads((HERE / "west_raster_headers.json").read_text(encoding="utf-8"))["sources"]
        ordered = source_records(list(reversed(records)))
        self.assertEqual([ident for ident, _ in ordered], ["x51y401", "x52y401"])

    def test_wrong_duplicate_or_mislabeled_products_are_rejected(self):
        records = json.loads((HERE / "west_raster_headers.json").read_text(encoding="utf-8"))["sources"]
        for bad in ([records[0], records[0]], records[:1], [dict(records[0], title="Unknown source"), records[1]], [dict(records[0], url=records[1]["url"]), records[1]]):
            with self.assertRaises(ValueError):
                source_records(bad)

    def test_partial_acquisition_is_preserved(self):
        output = fresh_output(self.root, self.root / "Logs/DeathValleyInventory/new-proof")
        sentinel = output / "partial_window.tif"
        sentinel.write_bytes(b"preserve partial source")
        with self.assertRaises(FileExistsError):
            fresh_output(self.root, output)
        self.assertEqual(sentinel.read_bytes(), b"preserve partial source")

    def test_output_must_stay_in_diagnostic_lane(self):
        with self.assertRaises(ValueError):
            fresh_output(self.root, self.root / "Assets/new-proof")
        self.assertFalse((self.root / "Assets").exists())

    def test_boundary_validates_crs_park_and_polygon_on_cached_reads(self):
        data = {"spatialReference": {"wkid": 26911}, "features": [{"attributes": {"UNIT_CODE": "DEVA"}, "geometry": {"rings": [[[0, 0], [1, 0], [1, 1], [0, 0]]]}}]}
        validate_boundary(data)
        for mutation in ("crs", "park", "ring", "incomplete"):
            changed = copy.deepcopy(data)
            if mutation == "crs": changed["spatialReference"]["wkid"] = 4326
            if mutation == "park": changed["features"][0]["attributes"]["UNIT_CODE"] = "OTHER"
            if mutation == "ring": changed["features"][0]["geometry"]["rings"][0][-1] = [2, 2]
            if mutation == "incomplete": changed["exceededTransferLimit"] = True
            with self.assertRaises(ValueError): validate_boundary(changed)

    def test_terrain_contract_rejects_shifted_bounds_or_reversed_rows(self):
        provenance = {"bounds_m": [520400,4006200,524496,4010296], "working_crs": "EPSG:26911",
                      "height_encoding": {"dtype":"little-endian uint16", "row_order":"north to south", "column_order":"west to east", "min_m":-100, "max_m":1700},
                      "tile_count":256, "focus_tile_count":4, "terrain_manifest_sha256":"a"*64}
        config = {"bounds_m": provenance["bounds_m"].copy()}
        validate_terrain_contract(provenance, config)
        shifted = copy.deepcopy(provenance)
        shifted["bounds_m"] = [value+256 for value in provenance["bounds_m"]]
        with self.assertRaises(ValueError): validate_terrain_contract(shifted, config)
        provenance["height_encoding"]["row_order"] = "south to north"
        with self.assertRaises(ValueError): validate_terrain_contract(provenance, config)


if __name__ == "__main__":
    unittest.main()
