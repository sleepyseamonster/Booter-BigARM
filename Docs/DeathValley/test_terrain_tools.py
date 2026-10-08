"""Offline coordinate, planning and health-report regressions."""
import copy
import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from terrain_tools import doctor, locate, plan

HERE = Path(__file__).resolve().parent


class TerrainToolsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalog = json.loads((HERE/"coverage_catalog.json").read_text(encoding="utf-8"))
        cls.baseline_catalog = copy.deepcopy(cls.catalog)
        cls.baseline_catalog['unity_tiles'] = [t for t in cls.catalog['unity_tiles'] if not t.get('section')]
        assert len(cls.baseline_catalog['unity_tiles']) == 256

    def test_point_inside_focus_chunk(self):
        result = locate(self.catalog,520700,4007100)
        self.assertEqual([t["legacy_id"] for t in result["unity_chunks"]],["r12_c01"])
        self.assertEqual(result["unity_chunks"][0]["source_spacing_m"],1)
        self.assertFalse(result["on_shared_chunk_boundary"])

    def test_shared_corner_returns_four_sample_owners(self):
        result = locate(self.catalog,520656,4010040)
        self.assertEqual({t["legacy_id"] for t in result["unity_chunks"]}, {"r00_c00","r00_c01","r01_c00","r01_c01"})
        self.assertTrue(result["on_shared_chunk_boundary"])

    def test_former_proposal_location_tracks_built_coverage_and_retains_study_geography(self):
        result = locate(self.catalog,519000,4007000)
        if any(t.get('section') == 'west' for t in self.catalog['unity_tiles']):
            self.assertEqual([t['geographic_key'] for t in result['unity_chunks']],
                             ['epsg26911/e518864/n4006968/size256'])
            self.assertEqual(result['unity_chunks'][0]['section'],'west')
        else:
            self.assertFalse(result['unity_chunks'])
        self.assertFalse(locate(self.baseline_catalog,519000,4007000)['unity_chunks'])
        self.assertEqual([c["id"] for c in result["proposed_candidates"]],["candidate_west"])
        self.assertTrue(result["study_footprints"])
        self.assertFalse(locate(self.catalog,0,0)["study_footprints"])
        with self.assertRaises(ValueError): locate(self.catalog,float("nan"),0)

    def test_all_proposals_have_64_unique_aligned_chunks_and_eight_joins(self):
        for direction in ("west","north","east"):
            result = plan(self.baseline_catalog,"candidate_"+direction)
            self.assertEqual(result["chunk_count"],64)
            self.assertEqual(len({t["geographic_key"] for t in result["chunks"]}),64)
            self.assertEqual(result["retained_shared_edges"],8)
            self.assertEqual(result["raw_uint16_height_bytes"],64*129*129*2)
            self.assertEqual(result["status"],"proposal_only_not_selected_or_imported")
        west = plan(self.baseline_catalog,"candidate_west")
        self.assertEqual({n["side"] for t in west["chunks"] for n in t["retained_neighbors"]},{"east"})
        self.assertEqual(plan(self.baseline_catalog,"candidate_west",1)["raw_uint16_height_bytes"],64*257*257*2)

    def test_overlap_alignment_and_disconnected_batches_rejected(self):
        for box in ([520400,4006200,522448,4008248], [518353,4006200,520401,4008248], [516304,4006200,518352,4008248]):
            catalog = copy.deepcopy(self.baseline_catalog)
            catalog["expansion_candidates"][0]["bounds_m"] = box
            with self.assertRaises(ValueError): plan(catalog,"candidate_west")
        with self.assertRaises(ValueError): plan(self.baseline_catalog,"unknown")
        with self.assertRaises(ValueError): plan(self.baseline_catalog,"candidate_west",3)
        if any(t.get('section') == 'west' for t in self.catalog['unity_tiles']):
            with self.assertRaises(ValueError): plan(self.catalog,'candidate_west')

    def test_doctor_reports_missing_changed_and_unsafe_records(self):
        with tempfile.TemporaryDirectory(prefix="TerrainTools-") as directory:
            root = Path(directory)
            file = root/"scene.unity"
            file.write_bytes(b"scene")
            catalog = {"audited_utc":"fixture", "unity_scene":{"path":"scene.unity","sha256":hashlib.sha256(b"scene").hexdigest()},
                       "blender_files":[{"path":"missing.blend","sha256":"a"*64}], "unity_tiles":[],
                       "regions":[{"config":"../escape.json"}]}
            report = doctor(catalog,root)
            self.assertEqual({c["status"] for c in report["issues"]},{"missing","error"})
            file.write_bytes(b"changed scene")
            report = doctor(catalog,root)
            self.assertEqual(report["issues"][0]["status"],"changed")
            self.assertEqual(file.read_bytes(),b"changed scene")

    def test_healthy_report_and_malformed_hash(self):
        with tempfile.TemporaryDirectory(prefix="TerrainTools-") as directory:
            root = Path(directory)
            (root/"scene").write_bytes(b"scene")
            (root/"config").write_bytes(b"config")
            catalog = {"audited_utc":"fixture", "unity_scene":{"path":"scene","sha256":hashlib.sha256(b"scene").hexdigest()},
                       "blender_files":[], "unity_tiles":[], "regions":[{"config":"config"}]}
            self.assertEqual(doctor(catalog,root)["status"],"ok")
            catalog["unity_scene"]["sha256"] = "bad hash"
            self.assertEqual(doctor(catalog,root)["issues"][0]["status"],"error")

    def test_cli_unity_translation_matches_projected_lookup(self):
        def run(*arguments):
            return subprocess.run([sys.executable,str(HERE/"terrain_tools.py"),"--json",*arguments],capture_output=True,text=True,check=True)
        projected = json.loads(run("locate","520700","4007100").stdout)
        unity = json.loads(run("locate","-1748","-1148","--unity","--origin","522448","4008248").stdout)
        self.assertEqual(projected,unity)
        self.assertEqual(json.loads(run("plan","east").stdout)["chunk_count"],64)

    def test_cli_rejects_missing_origin_nonfinite_points_and_invalid_spacing(self):
        for arguments in (("locate","0","0","--unity"), ("locate","0","0","--origin","0","0"),
                          ("locate","nan","0"), ("plan","west","--spacing","3")):
            result = subprocess.run([sys.executable,str(HERE/"terrain_tools.py"),*arguments],capture_output=True,text=True)
            self.assertEqual(result.returncode,2)
            self.assertFalse(result.stdout)


if __name__ == "__main__":
    unittest.main()
