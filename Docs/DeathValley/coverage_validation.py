"""Offline validation shared by the coverage inventory and source verifier."""
import hashlib
import json
import math
import re
from pathlib import Path


def sha256(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def validate_boundary(data):
    sr = data.get("spatialReference", {})
    if sr.get("latestWkid", sr.get("wkid")) != 26911:
        raise ValueError("NPS boundary must use EPSG:26911")
    features = data.get("features", [])
    if data.get("error") or data.get("exceededTransferLimit") or not features:
        raise ValueError("Incomplete NPS boundary response")
    for feature in features:
        if feature.get("attributes", {}).get("UNIT_CODE") != "DEVA":
            raise ValueError("Unexpected park in boundary response")
        rings = feature.get("geometry", {}).get("rings", [])
        if not rings:
            raise ValueError("Park boundary has no polygon rings")
        for ring in rings:
            if len(ring) < 4 or ring[0] != ring[-1]:
                raise ValueError("Park polygon ring must be closed")
            if any(len(point) != 2 or not all(math.isfinite(v) for v in point) for point in ring):
                raise ValueError("Park polygon contains invalid coordinates")


def source_records(records):
    """Return known source products in geographic order, regardless of input order."""
    indexed = {}
    for record in records:
        matches = re.findall(r"\bx5[12]y401\b", record.get("title", ""))
        if len(matches) != 1:
            raise ValueError("Unexpected or ambiguous west candidate source product")
        ident = matches[0]
        expected = f"USGS_1M_11_{ident}_CA_FEMAR9Southeast_D24.tif"
        expected_url = "https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/1m/Projects/CA_FEMAR9Southeast_D24/TIFF/" + expected
        if record.get("url") != expected_url:
            raise ValueError("Source title and download product disagree")
        if ident in indexed:
            raise ValueError("Duplicate candidate source product")
        if record.get("epsg") != 26911 or record.get("resolution_m") != [1.0, 1.0]:
            raise ValueError("Unexpected recorded source grid")
        indexed[ident] = record
    if set(indexed) != {"x51y401", "x52y401"}:
        raise ValueError("Both candidate source products are required")
    return [(ident, indexed[ident]) for ident in ("x51y401", "x52y401")]


def validate_terrain_contract(provenance, config):
    encoding = {"dtype": "little-endian uint16", "row_order": "north to south",
                "column_order": "west to east", "min_m": -100, "max_m": 1700}
    if provenance.get("bounds_m") != config["bounds_m"] or provenance.get("working_crs") != "EPSG:26911" or provenance.get("height_encoding") != encoding:
        raise ValueError("Terrain provenance disagrees with configured geography or height encoding")
    if provenance.get("tile_count") != 256 or provenance.get("focus_tile_count") != 4:
        raise ValueError("Terrain provenance has unexpected tile counts")
    if not re.fullmatch(r"[0-9a-f]{64}", provenance.get("terrain_manifest_sha256", "")):
        raise ValueError("Terrain source version is not a SHA-256 identity")


def fresh_output(root, output):
    """Reserve a new diagnostic directory; never overwrite a partial acquisition."""
    output = Path(output).resolve()
    allowed = (Path(root) / "Logs/DeathValleyInventory").resolve()
    if not output.is_relative_to(allowed) or output == allowed:
        raise ValueError("Source proof output must be a new subdirectory of Logs/DeathValleyInventory")
    try:
        output.mkdir(parents=True, exist_ok=False)
    except FileExistsError as error:
        raise FileExistsError(f"Source proof directory already exists: {output}. Pass --output with a new directory; incomplete acquisitions are preserved.") from error
    return output


def assessment_availability(record, bounds, root):
    """Distinguish retained verification evidence from currently usable source files."""
    if record.get("status") != "source_candidate_verified_not_imported_not_selected":
        raise ValueError("Assessment is not a passing candidate source record")
    if record.get("working_crs") != "EPSG:26911" or record.get("bounds_m") != bounds:
        raise ValueError("Assessment describes different candidate geography")
    spacing = record.get("prepared_spacing_m")
    if spacing != 2 or record.get("source_spacing_m") != 1:
        raise ValueError("Unexpected candidate sample spacing")
    expected_shape = [(bounds[3] - bounds[1]) // spacing + 1, (bounds[2] - bounds[0]) // spacing + 1]
    if record.get("shape") != expected_shape or record.get("sample_count") != math.prod(expected_shape) or record.get("nodata_count") != 0:
        raise ValueError("Assessment does not establish complete candidate sample coverage")
    difference = record.get("retained_boundary_max_difference_m", math.inf)
    tolerance = record.get("boundary_tolerance_m", math.nan)
    if not math.isfinite(difference) or difference < 0 or not math.isclose(tolerance, 1800 / 65535) or difference > tolerance:
        raise ValueError("Assessment border check failed or uses an unexpected tolerance")
    sources = record.get("sources", [])
    if len(sources) != 2 or {r.get("id") for r in sources} != {"x51y401", "x52y401"}:
        raise ValueError("Assessment source identities are incomplete")
    files = [(record.get("grid_path"), record.get("grid_sha256"))]
    for source in sources:
        files.extend((source.get(name + "_path"), source.get(name + "_sha256")) for name in ("window", "metadata"))
    missing = []
    root = Path(root).resolve()
    archive = root / "SourceData/Terrain/DeathValley/WestCandidate2026-10-06/manifest.json"
    archived = {}
    if archive.is_file():
        for entry in json.loads(archive.read_text(encoding="utf-8"))["records"]:
            if entry["source"] in archived:
                raise ValueError("Ambiguous archived candidate source")
            archived[entry["source"]] = entry
    resolved = []
    for name, expected in files:
        if not isinstance(name, str) or Path(name).is_absolute() or not isinstance(expected, str) or not re.fullmatch(r"[0-9a-f]{64}", expected):
            raise ValueError("Invalid local artifact path or checksum in assessment")
        path = (root / name).resolve()
        if not path.is_relative_to(root):
            raise ValueError("Assessment artifact escapes the checkout")
        if name in archived:
            entry = archived[name]
            if entry["sha256"] != expected:
                raise ValueError("Archived candidate version differs from assessment")
            destination = entry["destination"]
            if not isinstance(destination, str) or Path(destination).is_absolute():
                raise ValueError("Invalid archived candidate path")
            path = (root / destination).resolve()
            if not path.is_relative_to(root):
                raise ValueError("Archived candidate path escapes the checkout")
        resolved.append(path.relative_to(root).as_posix())
        if not path.is_file():
            missing.append(name)
        elif sha256(path) != expected:
            raise ValueError(f"Candidate source artifact changed: {name}")
    return {"status": "unavailable" if missing else "hash_verified", "missing_artifacts": missing,
            "checked_artifact_count": len(files), "resolved_artifacts": resolved,
            "limits": "Historical acquisition proof is separate from local file availability and Unity import."}
