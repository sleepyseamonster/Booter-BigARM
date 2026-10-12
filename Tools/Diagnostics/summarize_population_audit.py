"""Analyze bounded population timing; manual offscreen measurements are not interactive FPS."""
import argparse
import hashlib
import json
import math
from pathlib import Path
from summarize_camera_transitions import statistics


def summarize(report):
    if (report.get("mode") != "manual_urp_population_reference_diagnostic" or not report.get("completed")
            or report.get("error") or not report.get("nonuniformPixels")
            or report.get("nativeTerrains") != 16 or report.get("enabledColliders") != 16):
        raise ValueError("A completed, rendered reference capture with resident collision is required.")
    samples = report.get("samples", [])
    if not samples or {s["phase"] for s in samples} != {0, 1, 2}:
        raise ValueError("All three demand phases must be sampled.")
    for sample in samples:
        if not all(math.isfinite(sample[k]) and sample[k] >= 0 for k in ("elapsed", "populationMs", "renderMs", "updateMs")):
            raise ValueError("Finite nonnegative timing required.")
        if not 0 <= sample["operations"] <= 64:
            raise ValueError("Operation cap exceeded.")
    phases = []
    for phase in range(3):
        selected = [s for s in samples if s["phase"] == phase]
        phases.append({"phase": phase, "update": statistics([s["updateMs"] for s in selected]),
                       "population_work": statistics([s["populationMs"] for s in selected]),
                       "manual_render": statistics([s["renderMs"] for s in selected]),
                       "active_minimum": min(s["active"] for s in selected), "active_maximum": max(s["active"] for s in selected),
                       "pending_maximum": max(s["pending"] for s in selected),
                       "nominal_1ms_overruns": sum(s["populationMs"] > 1 for s in selected),
                       "actor_wait_samples": sum(s["booterWaiting"] or s["bigArmWaiting"] for s in selected)})
    displacement = {}
    for actor in ("booter", "bigArm"):
        start, end = report[actor + "Start"], report[actor + "End"]
        displacement[actor] = math.hypot(end["x"] - start["x"], end["z"] - start["z"])
    return {"scope": "One manual offscreen reference run; actor motion, no injected input; not interactive FPS or traversal acceptance",
            "initial_ready_seconds": report["readySeconds"], "startup_maximum_population_ms": report["startupMaximumPopulationMs"],
            "phases": phases, "actor_horizontal_displacement_m": displacement,
            "maximum_synchronization_count": max(s["synchronizations"] for s in samples),
            "limitations": ["Nominal time budget can be exceeded by indivisible work units and demand updates",
                            "Physics synchronization runs in actor callbacks outside measured population work",
                            "Warmup excludes initial readiness from steady update statistics",
                            "Endpoint pixels do not establish population appearance along the whole run"]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    source = args.report.read_bytes()
    if len(source) > 20 * 1024 * 1024:
        raise ValueError("Bounded report required.")
    result = summarize(json.loads(source)); result["source_sha256"] = hashlib.sha256(source).hexdigest()
    with args.output.open("x", encoding="utf-8", newline="\n") as output:
        output.write(json.dumps(result, indent=2) + "\n")


if __name__ == "__main__":
    main()
