"""Resolve explicitly retired Blender sources through immutable cleanup evidence."""
import hashlib
import json
from pathlib import Path
import re

RECEIPT = "Docs/Evidence/DeathValley/BLENDER_TERRAIN_CLEANUP_2026-10-09.json"
SCOPE = "SourceArt/Blender/Studies/DeathValley/"


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024*1024), b""):
            h.update(block)
    return h.hexdigest()


def contained(root, relative):
    root = Path(root).resolve()
    if not isinstance(relative, str) or Path(relative).is_absolute():
        raise ValueError("Expected a relative retirement evidence path")
    path = (root/relative).resolve()
    if not path.is_relative_to(root):
        raise ValueError("Retirement evidence escaped checkout")
    return path


def evidence(root, record):
    if not re.fullmatch(r"[0-9a-f]{64}", record.get("sha256", "")):
        raise ValueError("Invalid evidence SHA-256")
    path = contained(root, record["path"])
    if digest(path) != record["sha256"]:
        raise ValueError("Changed retirement evidence")
    return path, json.loads(path.read_text(encoding="utf-8"))


def load_retired_blender_records(root):
    root = Path(root).resolve()
    receipt_path = root/RECEIPT
    if not receipt_path.exists():
        return []
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    if receipt.get("schema_version") != 1 or receipt.get("status") != "complete" or not receipt.get("production_unchanged"):
        raise ValueError("Blender cleanup is not complete and production-preserving")
    proof_path, proof = evidence(root, receipt["production_proof"])
    expected = {"status": "validated_full_grid_and_collider_samples",
                "scene": receipt["production_scene"], "scene_sha256": receipt["production_sha256"],
                "manifest_sha256": receipt["final_runtime_manifest_sha256"],
                "terrains": 4608, "edges": 9072, "outer_edges": 288, "collider_samples": 115200}
    if any(proof.get(k) != v for k,v in expected.items()):
        raise ValueError("Retirement native production proof mismatch")
    if digest(proof_path.parent/"manifest.json") != receipt["final_runtime_manifest_sha256"]:
        raise ValueError("Retirement native manifest changed")
    _, inventory = evidence(root, receipt["pre_cleanup_inventory"])
    if inventory.get("schema_version") != 1:
        raise ValueError("Unsupported pre-cleanup inventory")
    originals = {}
    for item in inventory["files"]:
        if item["path"] in originals:
            raise ValueError("Duplicate pre-cleanup identity")
        originals[item["path"]] = item
    removed = receipt["removed"]
    if not removed or len(removed) != len(originals) or receipt.get("removed_count",len(removed)) != len(removed):
        raise ValueError("Incomplete retirement inventory")
    records = []; seen = set()
    for item in removed:
        relative = item["path"]
        path = contained(root, relative)
        if (relative in seen or not relative.startswith(SCOPE) or path.suffix.lower() != ".blend"
                or not isinstance(item.get("size_bytes"),int) or item["size_bytes"] <= 0
                or not re.fullmatch(r"[0-9a-f]{64}",item.get("sha256",""))):
            raise ValueError("Invalid retired source identity")
        original = originals.get(relative)
        if original is None or any(item.get(k) != original.get(k) for k in ("path","sha256","size_bytes","batch")):
            raise ValueError("Retired identity differs from reviewed pre-cleanup source")
        if path.exists():
            raise ValueError("Retired source is still materialized")
        seen.add(relative)
        records.append({"path":relative, "storage":"intentionally_retired",
                        "historical_sha256":item["sha256"], "historical_bytes":item["size_bytes"],
                        "batch":item.get("batch"), "current_sha256":None,
                        "readability":"deleted_native_bytes_not_reopened_by_this_audit",
                        "receipt":RECEIPT, "receipt_sha256":digest(receipt_path),
                        "native_scene_at_retirement_sha256":receipt["production_sha256"],
                        "identity_evidence":"reviewed_pre_cleanup_inventory_and_sealed_native_proof"})
    # The native proof above seals the historical retirement scene, not a future
    # expanded scene. Normal coverage dispatch independently verifies current
    # scene/native proof; subsequent authorized additions need no receipt rewrite.
    return records


def retired_record(root, path):
    relative = Path(path).resolve().relative_to(Path(root).resolve()).as_posix()
    matches = [r for r in load_retired_blender_records(root) if r["path"] == relative]
    if len(matches) != 1:
        raise FileNotFoundError("Missing Blender source is not an explicitly retired path: " + relative)
    return matches[0]


def find_retired_blend(root, name):
    matches = [r for r in load_retired_blender_records(root) if Path(r["path"]).name == name]
    if len(matches) != 1:
        raise ValueError("Missing or ambiguous unlisted Blender source: " + name)
    return contained(root,matches[0]["path"])
