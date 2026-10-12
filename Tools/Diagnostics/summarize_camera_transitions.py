"""Summarize accepted manual Player camera sweeps; never report interactive FPS."""
import argparse
import hashlib
import json
import math
from pathlib import Path


def statistics(values):
    ordered = sorted(values)
    if not ordered:
        return {"samples": 0}
    return {"samples": len(ordered), "mean_ms": sum(ordered) / len(ordered),
            "median_ms": ordered[math.ceil(len(ordered) * .5) - 1],
            "p95_ms": ordered[math.ceil(len(ordered) * .95) - 1],
            "p99_ms": ordered[math.ceil(len(ordered) * .99) - 1], "maximum_ms": ordered[-1]}


def summarize(report):
    if (report.get("mode") != "standalone_player_manual_urp_offscreen_diagnostic"
            or not report.get("cameraSweep") or not report.get("completed") or report.get("error")
            or not report.get("nonuniformPixels") or not report.get("savesDisabled")
            or report.get("enabledColliders") != report.get("residentTerrains")):
        raise ValueError("An accepted manual sweep with resident collision is required.")
    frames = report.get("transitionFrames", [])
    times = report.get("frameTimesMs", [])
    if not frames or len(frames) != len(times) or len(frames) != report.get("samples"):
        raise ValueError("Per-frame trace must match retained update samples.")
    tile_events, sector_events, count_changes = [], [], 0
    previous_tile = previous_sector = previous_count = None
    previous_elapsed = -math.inf
    for index, (frame, duration) in enumerate(zip(frames, times)):
        position = frame["camera"]
        values = [frame["elapsed"], frame["requestMs"], frame["updateMs"], duration, *position.values()]
        if not all(isinstance(v, (int, float)) and math.isfinite(v) for v in values):
            raise ValueError("Trace values must be finite.")
        if (frame["elapsed"] < previous_elapsed or frame["requestMs"] < 0 or duration <= 0
                or not math.isclose(frame["updateMs"], duration, abs_tol=1e-9)
                or not isinstance(frame["terrainRenderers"], int)
                or not 0 < frame["terrainRenderers"] <= report["residentTerrains"]):
            raise ValueError("Invalid trace ordering, timing or renderer count.")
        previous_elapsed = frame["elapsed"]
        tile = (math.floor(position["x"] / 256), math.floor(position["z"] / 256))
        sector = (math.floor(position["x"] / 1024), math.floor(position["z"] / 1024))
        if previous_tile is not None and tile != previous_tile:
            tile_events.append({"sample": index, "elapsed": frame["elapsed"], "from": previous_tile, "to": tile})
        if previous_sector is not None and sector != previous_sector:
            sector_events.append({"sample": index, "elapsed": frame["elapsed"], "from": previous_sector, "to": sector})
        if previous_count is not None and previous_count != frame["terrainRenderers"]:
            count_changes += 1
        previous_tile, previous_sector, previous_count = tile, sector, frame["terrainRenderers"]
    if len(sector_events) < 4:
        raise ValueError("The square sweep did not prove four sector crossings.")
    positions = [frame["camera"] for frame in frames]
    spans = [max(p[axis] for p in positions) - min(p[axis] for p in positions) for axis in ("x", "z")]
    closure = math.hypot(positions[-1]["x"] - positions[0]["x"], positions[-1]["z"] - positions[0]["z"])
    if frames[-1]["elapsed"] < 29.5 or min(spans) < 1000 or closure > 100:
        raise ValueError("Trace must cover the full 30-second 1 km square and return near its origin.")
    boundary = [any(abs(f["elapsed"] - e["elapsed"]) <= .25 for e in tile_events) for f in frames]
    return {"scope": "Manual offscreen camera sweep; not interactive FPS or actor traversal acceptance",
            "all_updates": statistics(times),
            "within_250ms_of_native_cell_crossing": statistics([t for t, near in zip(times, boundary) if near]),
            "away_from_native_cell_crossing": statistics([t for t, near in zip(times, boundary) if not near]),
            "manual_request_cpu": statistics([f["requestMs"] for f in frames]),
            "minimum_terrain_renderers": min(f["terrainRenderers"] for f in frames),
            "maximum_terrain_renderers": max(f["terrainRenderers"] for f in frames),
            "renderer_count_changes": count_changes, "tile_crossings": tile_events, "sector_crossings": sector_events,
            "camera_horizontal_spans_m": spans, "route_closure_m": closure,
            "resident_colliders": report["enabledColliders"], "gpu_timing_available": report.get("validGpuTimings", 0) > 0}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if args.report.stat().st_size > 10 * 1024 * 1024:
        raise ValueError("Use a bounded sweep report, at most 10 MiB.")
    source = args.report.read_bytes()
    result = summarize(json.loads(source))
    result["source_sha256"] = hashlib.sha256(source).hexdigest()
    encoded = json.dumps(result, indent=2) + "\n"
    if args.output:
        with args.output.open("x", encoding="utf-8", newline="\n") as output:
            output.write(encoded)
    else:
        print(encoded, end="")


if __name__ == "__main__":
    main()
