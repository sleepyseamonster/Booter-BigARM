"""Retired bytes are historical identity, never a successful current file check."""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from blender_retirement import (RECEIPT, digest, find_retired_blend,
                                load_retired_blender_records, retired_record)
import audit_coverage
from terrain_tools import doctor


class RetirementInventoryTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(dir="D:/BooterBigArmValidation/Temp")
        self.root=Path(self.temp.name)
        self.path="SourceArt/Blender/Studies/DeathValley/BadwaterGameSlice.blend"
        self.item={"path":self.path,"sha256":hashlib.sha256(b"reviewed native source").hexdigest(),
                   "size_bytes":22,"batch":"BadwaterFourSlices"}
        self.manifest=self.root/"Assets/_Project/Art/Terrain/Final/Source/manifest.json"
        self.manifest.parent.mkdir(parents=True);self.manifest.write_text("sealed manifest")
        proof=self.manifest.parent/"unity_production_validation.json"
        proof.write_text(json.dumps({"status":"validated_full_grid_and_collider_samples",
            "scene":"Assets/_Project/Scenes/Production/GreaterWasteland.unity",
            "scene_sha256":"a"*64,"manifest_sha256":digest(self.manifest),
            "terrains":4608,"edges":9072,"outer_edges":288,"collider_samples":115200}))
        inventory=self.root/"Docs/Evidence/pre_cleanup.json"
        inventory.parent.mkdir(parents=True);inventory.write_text(json.dumps({"schema_version":1,"files":[self.item]}))
        self.receipt={"schema_version":1,"status":"complete","production_unchanged":True,
            "production_scene":"Assets/_Project/Scenes/Production/GreaterWasteland.unity",
            "production_sha256":"a"*64,"final_runtime_manifest_sha256":digest(self.manifest),
            "production_proof":{"path":proof.relative_to(self.root).as_posix(),"sha256":digest(proof)},
            "pre_cleanup_inventory":{"path":inventory.relative_to(self.root).as_posix(),"sha256":digest(inventory)},
            "removed":[dict(self.item)],"removed_count":1}
        self.save()

    def save(self):
        p=self.root/RECEIPT;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(self.receipt))

    def tearDown(self):self.temp.cleanup()

    def test_valid_retirement_is_historical_and_excluded_from_current_byte_checks(self):
        records=load_retired_blender_records(self.root)
        self.assertEqual(records[0]["historical_sha256"],self.item["sha256"])
        self.assertIsNone(records[0]["current_sha256"])
        self.assertNotIn("sha256",records[0])
        self.assertEqual(find_retired_blend(self.root,"BadwaterGameSlice.blend"),self.root/self.path)
        with patch.object(audit_coverage,"ROOT",self.root):
            record=audit_coverage.asset_record(audit_coverage.find("BadwaterGameSlice.blend"))
        self.assertEqual(record["storage"],"intentionally_retired")
        scene=self.root/"scene.unity";scene.write_text("future expanded scene")
        catalog={"audited_utc":"fixture","unity_scene":{"path":"scene.unity","sha256":digest(scene)},
                 "blender_files":[],"retired_blender_files":records,"unity_tiles":[],"regions":[]}
        report=doctor(catalog,self.root)
        self.assertEqual(report["status"],"ok")
        self.assertEqual(report["retired_blender_source_count"],1)
        self.assertEqual(report["checked_records"],1)
        self.assertEqual(load_retired_blender_records(self.root),records)

    def test_unlisted_missing_source_is_not_retired(self):
        with self.assertRaises(FileNotFoundError):retired_record(self.root,self.root/"SourceArt/Blender/Studies/DeathValley/Unexpected.blend")
        with patch.object(audit_coverage,"ROOT",self.root):
            with self.assertRaises(ValueError):audit_coverage.find("Unexpected.blend")

    def test_tampered_retired_hash_or_unlisted_path_is_rejected(self):
        for key,value in (("sha256","0"*64),("path","SourceArt/Blender/Studies/DeathValley/Other.blend")):
            with self.subTest(key=key):
                self.receipt["removed"][0]=dict(self.item)|{key:value};self.save()
                with self.assertRaises(ValueError):load_retired_blender_records(self.root)
        self.receipt["removed"]=[dict(self.item),dict(self.item)];self.receipt["removed_count"]=2;self.save()
        with self.assertRaises(ValueError):load_retired_blender_records(self.root)

    def test_changed_native_proof_or_inventory_cannot_attest_retirement(self):
        for field in ("production_proof","pre_cleanup_inventory"):
            target=self.root/self.receipt[field]["path"];original=target.read_bytes();target.write_bytes(b"changed")
            with self.assertRaises(ValueError):load_retired_blender_records(self.root)
            target.write_bytes(original)
        self.receipt["production_sha256"]="b"*64;self.save()
        with self.assertRaises(ValueError):load_retired_blender_records(self.root)

    def test_materialized_source_or_partial_cleanup_rejects_retirement(self):
        target=self.root/self.path;target.parent.mkdir(parents=True);target.write_bytes(b"unexpected materialized source")
        with self.assertRaises(ValueError):load_retired_blender_records(self.root)
        target.unlink();self.receipt["status"]="partial_requires_root_review";self.save()
        with self.assertRaises(ValueError):load_retired_blender_records(self.root)

    def test_doctor_rejects_tampered_catalog_retirement_and_missing_current_files(self):
        scene=self.root/"scene";scene.write_bytes(b"scene")
        records=load_retired_blender_records(self.root);records[0]["historical_sha256"]="0"*64
        catalog={"audited_utc":"fixture","unity_scene":{"path":"scene","sha256":digest(scene)},
                 "blender_files":[{"path":"missing.blend","sha256":"a"*64}],
                 "retired_blender_files":records,"unity_tiles":[],"regions":[]}
        report=doctor(catalog,self.root)
        self.assertEqual(report["status"],"attention_required")
        self.assertEqual({r["status"] for r in report["issues"]},{"missing","error"})

if __name__=="__main__":unittest.main()
