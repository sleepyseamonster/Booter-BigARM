"""Bounded six-section third northern row; standard-library imports for Blender."""
from pathlib import Path
import hashlib

ROOT = Path(__file__).resolve().parents[4]
CRS = "EPSG:26911"
ORIGIN = (522448, 4008248)
BASELINE_BOUNDS = (499920, 4006200, 524496, 4022584)
ROW_BOUNDS = (499920, 4022584, 524496, 4026680)
SURVEY = ROOT / "SourceData/Terrain/DeathValley/NorthRow3Survey2026-10-10"
SOUTH_IDS = tuple(f"north_row2_e{e}_n4018488" for e in range(499920, 524496, 4096))

def sha256(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(4 * 1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def contract(step, execution_date="2026-10-10"):
    import datetime
    if type(step) is not int or not 1 <= step <= 6:
        raise ValueError("Only ordered third-row steps 1..6 are accepted")
    if datetime.date.fromisoformat(execution_date).isoformat() != execution_date:
        raise ValueError("An explicit ISO execution date is required")
    east = ROW_BOUNDS[0] + (step - 1) * 4096
    batch = f"NorthRow3E{east}N4022584_{execution_date}"
    retained = 6144 + (step - 1) * 256
    edges = 12128 if step == 1 else 12624 + (step - 2) * 512
    return dict(family="north_row3", schema_version=1, step=step, batch=batch,
                section=f"north_row3_e{east}_n4022584", bounds=[east, 4022584, east + 4096, 4026680],
                south=SOUTH_IDS[step - 1], west=None if step == 1 else f"north_row3_e{east - 4096}_n4022584",
                absent_west_reason="No accepted terrain west of the first section" if step == 1 else None,
                retained_count=retained, total=retained + 256, baseline_edges=edges,
                edges=12624 + (step - 1) * 512, exterior_slots=352, collider_samples=(retained + 256) * 25,
                data=ROOT / f"SourceData/Terrain/DeathValley/{batch}",
                art=ROOT / f"SourceArt/Blender/Studies/DeathValley/{batch}",
                asset=f"Assets/_Project/Art/Terrain/{batch}")

def occupied_cells(accepted_steps):
    if type(accepted_steps) is not int or not 0 <= accepted_steps <= 6:
        raise ValueError("Unsupported accepted row extent")
    cells = {(e, n) for e in range(499920, 524496, 256) for n in range(4006200, 4022584, 256)}
    cells.update((e, n) for e in range(499920, 499920 + accepted_steps * 4096, 256) for n in range(4022584, 4026680, 256))
    return cells

def validate_occupancy(step, cells):
    cells = list(cells)
    if len(cells) != len(set(cells)) or set(cells) != occupied_cells(step):
        raise ValueError("Missing, duplicate, shifted or prematurely filled third-row cell")

def validate_geographic_records(step, records):
    records = list(records)
    cells = []
    for t in records:
        bounds = t.get("bounds_m", [])
        if len(bounds) != 4:
            raise ValueError("Complete geographic bounds required")
        east, north = bounds[:2]
        if east != int(east) or north != int(north):
            raise ValueError("Integer projected coordinates required")
        east, north = int(east), int(north)
        if bounds != [east, north, east + 256, north + 256] or t.get("geographic_key") != f"epsg26911/e{east}/n{north}/size256":
            raise ValueError("Shifted bounds or incorrect geographic key")
        cells.append((east, north))
    validate_occupancy(step, cells)

def product_for(east, north):
    """Half-open ownership on both axes; y labels identify the northern domain edge."""
    if not ROW_BOUNDS[0] <= east <= ROW_BOUNDS[2] or not ROW_BOUNDS[1] <= north <= ROW_BOUNDS[3]:
        raise ValueError("Sample outside the approved strip")
    return f"x{int(east // 10000)}y{int(north // 10000) + 1}"

def required_products(bounds=ROW_BOUNDS):
    if tuple(bounds) != ROW_BOUNDS:
        raise ValueError("Survey is confined to the complete approved strip")
    return {f"x{x}y{y}" for x in range(49, 53) for y in (403,)}
