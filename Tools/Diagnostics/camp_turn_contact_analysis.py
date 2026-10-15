"""Offline persistent-contact and fixed-pivot feasibility screen; no Unity simulation.

Requires the existing NumPy runtime. Writes scalar evidence only, never pose tracks.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

import numpy as np


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def fixed_pivot_fit(tangent, orbit):
    requested_shift = tangent[0] - tangent
    matrix = orbit.reshape(-1, 2)
    pivot = np.linalg.lstsq(matrix, requested_shift.reshape(-1), rcond=None)[0]
    shift = np.einsum("tij,j->ti", orbit, pivot)
    residual = np.linalg.norm(tangent + shift - tangent[0], axis=1)
    return {
        "rms": float(np.sqrt(np.mean(residual ** 2))),
        "maximumResidual": float(residual.max()), "pivotRadius": float(np.linalg.norm(pivot)),
        "maximumRootShift": float(np.linalg.norm(shift, axis=1).max()),
        "rootPathLength": float(np.linalg.norm(np.diff(shift, axis=0), axis=1).sum()),
    }


def episodes(points, times, ceiling):
    near = (points[:, :, 1] >= -.001) & (points[:, :, 1] <= ceiling)
    touching = near.any(axis=1)
    result = []
    start = None
    for index in range(len(times) + 1):
        active = index < len(times) and touching[index]
        if active and start is None:
            start = index
        if not active and start is not None:
            valid = near[start:index].all(axis=0)
            delta = points[start:index][:, :, [0, 2]] - points[start][:, [0, 2]]
            excursion = np.linalg.norm(delta, axis=2).max(axis=0)
            result.append({
                "startSeconds": float(times[start]), "endSeconds": float(times[index - 1]),
                "durationSeconds": float(times[index - 1] - times[start]),
                "persistentPoints": int(valid.sum()),
                "bestFixedPointExcursion": float(excursion[valid].min()) if valid.any() else None,
            })
            start = None
    return {"nearGroundFrames": int(touching.sum()), "episodes": result}


def analyze(project, run, output):
    project = project.resolve()
    if (project / ".git").exists() or not (project / ".animation-playground-validation-project").is_file():
        raise ValueError("Use the marked isolated validation project.")
    if not all(re.fullmatch(r"[A-Za-z0-9]+", value) for value in (run, output)):
        raise ValueError("Use simple immutable run names.")
    base = project / "LocomotionImplementation20261014"
    source = base / run
    destination = base / output
    if destination.exists():
        raise FileExistsError("Preserve prior analysis.")
    summary = read(source / "summary.json")
    results, inputs = [], {"summary.json": digest(source / "summary.json")}
    total_points = 0
    for row in summary["rows"]:
        track_path = source / row["privateTrack"]["file"]
        if not track_path.resolve().is_relative_to(source.resolve()):
            raise ValueError("Track must stay inside its measured run.")
        if digest(track_path) != row["privateTrack"]["sha256"]:
            raise ValueError("Private track hash mismatch.")
        track = read(track_path)
        if track["clip"] != row["clip"] or track["quality"] != row["quality"]:
            raise ValueError("Named source/influence pairing mismatch.")
        points = np.array([[[p["x"], p["y"], p["z"]] for p in f["points"]] for f in track["frames"]])
        times = np.array([f["seconds"] for f in track["frames"]])
        sides = np.array(track["sides"])
        expected = (row["samples"], row["privateTrack"]["pointsPerFrame"], 3)
        if points.shape != expected or len(sides) != expected[1] or not np.isin(sides, [0, 1]).all():
            raise ValueError("Incomplete or unclassified sole geometry.")
        if not np.isfinite(points).all() or not np.isfinite(times).all() or not (np.diff(times) > 0).all():
            raise ValueError("Require finite complete chronological frames.")
        total_points += expected[0] * expected[1]
        tape_path = base / "TurnSourceContactBasisV1" / (track["clip"] + "-private-applied-poses.json")
        tape = read(tape_path)
        if not np.allclose(times, [f["seconds"] for f in tape["frames"]], rtol=0, atol=1e-6):
            raise ValueError("Yaw and sole samples must use identical native times.")
        rotations = np.array([[f["rootRotation"][k] for k in ["x", "y", "z", "w"]] for f in tape["frames"]])
        if not np.isfinite(rotations).all():
            raise ValueError("Require finite evaluated source rotations.")
        x, y, z, w = rotations.T
        angles = np.unwrap(np.arctan2(2 * (w * y + x * z), 1 - 2 * (y * y + z * z)))
        angles -= angles[0]
        if abs(abs(float(np.degrees(angles[-1]))) - 180) > .001:
            raise ValueError("Retain the complete native half turn.")
        # Unity's yaw rotates x/z by [[cos, sin], [-sin, cos]]. A root
        # orbit about a fixed world pivot P adds (I - R) P to every point.
        cosine, sine = np.cos(angles), np.sin(angles)
        orbit = np.array([[1 - cosine, -sine], [sine, 1 - cosine]]).transpose(2, 0, 1)
        matrix = orbit.reshape(-1, 2)
        if np.linalg.matrix_rank(matrix) != 2:
            raise ValueError("Degenerate pivot fit.")
        floor_points = np.where(((points[:, :, 1] >= -.001) & (points[:, :, 1] <= .001)).all(axis=0))[0]
        fits = []
        for vertex in floor_points:
            tangent = points[:, vertex][:, [0, 2]]
            fits.append({"side": int(sides[vertex]), **fixed_pivot_fit(tangent, orbit)})
        best = min(fits, key=lambda f: f["rms"]) if fits else None
        # The unconstrained least-squares RMS lower-bounds the maximum error
        # of every fixed-pivot fit, including pivots constrained to the capsule.
        results.append({
            "clip": track["clip"], "quality": track["quality"], "samples": len(times),
            "nativeSeconds": float(times[-1] - times[0]), "nativeSignedYaw": float(np.degrees(angles[-1])),
            "persistentFloorPoints": len(fits), "bestUnconstrainedFixedPivot": best,
            "fixedPivotRejectedForTestedPoints": best is None or best["rms"] > .02,
            "feet": [{"side": side, "nearFloor1mm": episodes(points[:, sides == side], times, .001),
                      "optimisticNearFloor8mm": episodes(points[:, sides == side], times, .008)} for side in [0, 1]],
        })
        inputs[track_path.name] = digest(track_path)
        inputs[str(tape_path.relative_to(base))] = digest(tape_path)
    if len(results) != 6 or total_points == 0:
        raise ValueError("Require both named sources at one/two/four influences.")
    payload = {"state": "persistent-contact-and-fixed-pivot-screen-not-qualified-or-installed",
               "inputHashes": inputs, "trackedPointsAcrossFrames": total_points,
               "contactCeiling": .001, "optimisticContactCeiling": .008, "driftBudget": .02,
               "rows": results,
               "scope": "Complete recorded native turns. Sole height does not establish force/support. One fixed-pivot model and recorded persistent sole points only; not all IK/root-trajectory adaptations. Unconstrained RMS lower-bounds worst drift. No runtime, motor, collision, handoff or author-feel qualification."}
    destination.mkdir()
    path = destination / "summary.json"
    path.write_text(json.dumps(payload, indent=2, allow_nan=False) + "\n", encoding="utf-8", newline="\n")
    return path


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--run", default="TurnContactTracksV2")
    parser.add_argument("--output", default="TurnPersistentContactV1")
    arguments = parser.parse_args()
    print(analyze(arguments.project, arguments.run, arguments.output))
