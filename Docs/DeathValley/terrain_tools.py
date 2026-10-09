"""Offline, read-only terrain health, coordinate lookup and expansion planning.

Uses the last coverage audit; run audit_coverage.py to refresh after file moves.
No network, Unity launch, asset writes, generation or streaming is performed.
"""
import argparse
import json
import math
import re
from pathlib import Path

from coverage_validation import assessment_availability, sha256
from blender_retirement import load_retired_blender_records

HERE = Path(__file__).resolve().parent


def contained_path(root, name):
    root = Path(root).resolve()
    if not isinstance(name, str) or Path(name).is_absolute():
        raise ValueError("Expected a checkout-relative path")
    path = (root / name).resolve()
    if not path.is_relative_to(root):
        raise ValueError("Recorded path escapes checkout")
    return path


def bounds(record):
    b = record["bounds_m"]
    if len(b) != 4 or not all(math.isfinite(v) for v in b) or b[0] >= b[2] or b[1] >= b[3]:
        raise ValueError("Invalid projected bounds")
    return b


def locate(catalog, east, north):
    if not all(math.isfinite(v) for v in (east, north)):
        raise ValueError("Coordinates must be finite")
    def hits(records):
        # Closed bounds return both owners on a shared sample edge/corner.
        return [r for r in records if bounds(r)[0] <= east <= bounds(r)[2]
                and bounds(r)[1] <= north <= bounds(r)[3]]
    tiles = hits(catalog["unity_tiles"])
    return {"working_crs": "EPSG:26911", "point_m": [east, north],
            "unity_chunks": tiles, "study_footprints": hits(catalog["regions"]),
            "proposed_candidates": hits(catalog["expansion_candidates"]),
            "on_shared_chunk_boundary": len(tiles) > 1,
            "limits": "Snapshot footprint lookup; study coverage does not prove fine terrain exists."}


def plan(catalog, candidate_id, spacing=2):
    matches = [r for r in catalog["expansion_candidates"] if r["id"] == candidate_id]
    if len(matches) != 1:
        raise ValueError("Expected one named expansion candidate")
    candidate = matches[0]
    x0, y0, x1, y1 = bounds(candidate)
    base = next(r for r in catalog["regions"] if r["id"] == "badwater")
    bx, by, _, _ = bounds(base)
    size = 256
    if spacing not in (1, 2) or any(v % size for v in (x0-bx, y0-by, x1-x0, y1-y0)):
        raise ValueError("Candidate must align to retained 256 m grid; spacing must be 1 or 2 m")
    tiles = []
    for north in range(int(y1-size), int(y0)-1, -size):
        for east in range(int(x0), int(x1), size):
            tile = {"geographic_key": f"epsg26911/e{east}/n{north}/size256",
                    "bounds_m": [east, north, east+size, north+size], "retained_neighbors": []}
            for old in catalog["unity_tiles"]:
                a,b,c,d = bounds(old)
                if min(east+size,c)>max(east,a) and min(north+size,d)>max(north,b):
                    raise ValueError("Candidate overlaps retained Unity terrain")
                edges = (("west",east==c,min(north+size,d)-max(north,b)),
                         ("east",east+size==a,min(north+size,d)-max(north,b)),
                         ("south",north==d,min(east+size,c)-max(east,a)),
                         ("north",north+size==b,min(east+size,c)-max(east,a)))
                for side, touching, length in edges:
                    if touching and length > 0:
                        tile["retained_neighbors"].append({"side":side, "legacy_id":old["legacy_id"], "shared_length_m":length})
            tiles.append(tile)
    if not any(t["retained_neighbors"] for t in tiles):
        raise ValueError("Candidate does not adjoin retained terrain")
    source_samples = size // spacing + 1
    return {"candidate": candidate_id, "status": "proposal_only_not_selected_or_imported",
            "working_crs": "EPSG:26911", "bounds_m": candidate["bounds_m"],
            "chunk_count":len(tiles), "chunk_size_m":size, "source_spacing_m":spacing,
            "source_samples_per_side":source_samples, "render_samples_per_side":257,
            "raw_uint16_height_bytes":len(tiles)*source_samples**2*2,
            "retained_shared_edges":sum(len(t["retained_neighbors"]) for t in tiles),
            "chunks":tiles,
            "limits":"Height payload estimate only; excludes Unity memory, textures, colliders, overhead and performance. No source acquisition or integration approval is implied."}


def doctor(catalog, root, assessment=None):
    checks = []
    def check(name, path, expected):
        try:
            path = contained_path(root, path)
            if not re.fullmatch(r"[0-9a-f]{64}", expected):
                raise ValueError("Invalid SHA-256 identity")
            state = "missing" if not path.is_file() else "ok" if sha256(path)==expected else "changed"
            checks.append({"name":name,"status":state,"path":str(path)})
        except (ValueError, OSError) as error:
            checks.append({"name":name,"status":"error","detail":str(error)})
    scene = catalog["unity_scene"]
    check("Unity scene snapshot",scene["path"],scene["sha256"])
    for file in catalog["blender_files"]:
        check("Blender source",file["path"],file["sha256"])
    for tile in catalog["unity_tiles"]:
        for kind in ("height", "color"):
            check(tile["legacy_id"]+" "+kind,tile[kind+"_source"],tile[kind+"_sha256"])
    for path in sorted({r["config"] for r in catalog["regions"]}):
        try:
            present = contained_path(root,path).is_file()
            checks.append({"name":"Footprint configuration", "path":path, "status":"present_unhashed" if present else "missing"})
        except ValueError as error:
            checks.append({"name":"Footprint configuration","status":"error","detail":str(error)})
    if assessment is not None:
        try:
            west = next(r for r in catalog["expansion_candidates"] if r["id"]=="candidate_west")
            availability = assessment_availability(assessment,west["bounds_m"],root)
            checks.append({"name":"West rebuild inputs", "status":"ok" if availability["status"]=="hash_verified" else "missing",
                           "availability":availability["status"], "missing_artifacts":availability["missing_artifacts"]})
        except (ValueError, OSError, StopIteration) as error:
            checks.append({"name":"West rebuild inputs","status":"error","detail":str(error)})
    retired=catalog.get("retired_blender_files",[])
    if retired:
        try:
            canonical=load_retired_blender_records(root)
            if {r["path"]:r for r in retired}!={r["path"]:r for r in canonical} or len(retired)!=len(canonical):
                raise ValueError("Catalog retirement differs from sealed cleanup receipt")
        except (ValueError,OSError,KeyError) as error:
            checks.append({"name":"Blender retirement provenance","status":"error","detail":str(error)})
    issues = [c for c in checks if c["status"] not in ("ok","present_unhashed")]
    return {"status":"attention_required" if issues else "ok", "snapshot_utc":catalog["audited_utc"],
            "checked_records":len(checks), "issues":issues,"retired_blender_source_count":len(retired),
            "limits":"Current materialized bytes versus retained hashes; retired Blender identities use historical cleanup evidence, not verified deleted bytes. Scene edits may be intentional; refresh the audit after review. Blender readability, Editor validation and Player acceptance remain separate."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", action="store_true", help="Emit complete machine-readable result")
    subs = parser.add_subparsers(dest="command", required=True)
    subs.add_parser("doctor", help="Verify local sources against the last audit")
    point = subs.add_parser("locate", help="Find chunks and studies covering a position")
    point.add_argument("east_or_x", type=float)
    point.add_argument("north_or_z", type=float)
    point.add_argument("--unity", action="store_true", help="Interpret inputs as Unity X/Z using explicit origin")
    point.add_argument("--origin", nargs=2, type=float, metavar=("EAST","NORTH"), help="Required with --unity; use the scene's verified projected origin")
    batch = subs.add_parser("plan", help="Inspect a proposed adjoining batch")
    batch.add_argument("candidate", choices=("west","north","east"))
    batch.add_argument("--spacing", type=int, choices=(1,2), default=2)
    args = parser.parse_args()
    try:
        catalog = json.loads((HERE/"coverage_catalog.json").read_text(encoding="utf-8"))
        if catalog["working_crs"] != "EPSG:26911" or catalog["schema_version"] != 1:
            raise ValueError("Unsupported coverage catalog")
        if args.command == "doctor":
            root = next(p for p in HERE.parents if (p/"ProjectSettings/ProjectVersion.txt").exists())
            assessment_path = HERE/"west_source_assessment.json"
            assessment = json.loads(assessment_path.read_text(encoding="utf-8")) if assessment_path.exists() else None
            result = doctor(catalog,root,assessment)
            if assessment is None:
                result["issues"].append({"name":"West source assessment","status":"missing"})
                result["status"] = "attention_required"
        elif args.command == "locate":
            if args.unity != bool(args.origin):
                raise ValueError("Use --unity and --origin together; no origin is guessed")
            east,north = args.east_or_x,args.north_or_z
            if args.unity:
                east += args.origin[0]
                north += args.origin[1]
            result = locate(catalog,east,north)
        else:
            result = plan(catalog,"candidate_"+args.candidate,args.spacing)
        if args.json:
            print(json.dumps(result,indent=2))
        elif args.command == "doctor":
            print(f"{result['status']}: {result['checked_records']} records checked; {len(result['issues'])} issues")
            for issue in result["issues"]:
                print(f"  {issue['name']}: {issue['status']} {issue.get('path',issue.get('detail',''))}")
                for missing in issue.get("missing_artifacts",[]):
                    print(f"    unavailable: {missing}")
        elif args.command == "locate":
            print(f"EPSG:26911 point: {result['point_m']}")
            for key in ("unity_chunks","study_footprints","proposed_candidates"):
                print(key+": "+(", ".join(r.get("legacy_id",r.get("id")) for r in result[key]) or "none"))
            if result["on_shared_chunk_boundary"]: print("Shared edge/corner: multiple chunks own this sample.")
        else:
            print(f"{result['candidate']}: {result['chunk_count']} proposed chunks; {result['retained_shared_edges']} retained shared edges")
            print(f"{result['source_spacing_m']} m source; {result['source_samples_per_side']} samples/side; {result['raw_uint16_height_bytes']:,} raw height bytes")
        if not args.json: print(result["limits"])
        return 1 if result.get("status")=="attention_required" else 0
    except (ValueError, KeyError, OSError, StopIteration) as error:
        parser.exit(2,f"Terrain tools: {error}\n")


if __name__ == "__main__":
    raise SystemExit(main())
