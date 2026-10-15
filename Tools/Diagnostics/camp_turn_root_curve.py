"""Fit a bounded offline actor-root curve to a persistent source sole point.

Uses existing NumPy only. No Unity/motor/physics execution or runtime activation.
The fitted curve is C1 with constant acceleration between native sample times.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

import numpy as np

BUDGETS = {"contactResidual": .019, "speed": 1.4, "acceleration": 8.5,
           "maximumRootOffset": .4, "endpointSpeed": 0, "nativeRate": 1}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def quadratic_peak(position, velocity, half_acceleration, duration):
    # Normalize to [0,1] rather than finding roots in a tiny native time interval.
    p, v, a = position, velocity * duration, half_acceleration * duration * duration
    coefficients = np.trim_zeros([4 * np.dot(a, a), 6 * np.dot(v, a),
                                 2 * (np.dot(v, v) + 2 * np.dot(p, a)), 2 * np.dot(p, v)], "f")
    candidates = [0., 1.]
    if len(coefficients) > 1:
        candidates += [float(r.real) for r in np.roots(coefficients)
                       if abs(r.imag) < 1e-7 and 0 < r.real < 1]
    return max(float(np.linalg.norm(p + v * t + a * t * t)) for t in candidates)


def fit(times, target, maximum_iterations=20000):
    dt, count = np.diff(times), len(times)
    if count < 3 or not np.isfinite(times).all() or not np.isfinite(target).all() or not (dt > 0).all():
        raise ValueError("Require finite chronological source samples.")
    lift = np.eye(count)[:, 1:-1]  # Initial and final velocity are exactly zero.
    integral = np.zeros((count, count))
    for index in range(1, count):
        integral[index] = integral[index - 1]
        integral[index, index - 1:index + 1] += dt[index - 1] / 2
    position = integral @ lift
    midpoints = position[:-1] + dt[:, None] * (3 * lift[:-1] + lift[1:]) / 8
    acceleration = np.diff(lift, axis=0) / dt[:, None]
    contact = position / BUDGETS["contactResidual"]
    # Root-position rows use the same scale as contact rows to avoid weak
    # ADMM convergence at the root-radius boundary. The feasible set is unchanged.
    root = np.vstack((position, midpoints)) / BUDGETS["contactResidual"]
    # Tight internal margins reserve room for solver residual and between-node
    # radius extrema. Acceptance below uses the original hard budgets.
    operator = np.vstack((contact, lift / (BUDGETS["speed"] - .001), acceleration / (BUDGETS["acceleration"] - .001), root))
    center = np.vstack((target / BUDGETS["contactResidual"], np.zeros((len(operator) - count, 2))))
    radii = np.ones(len(operator))
    radii[-len(root):] = (BUDGETS["maximumRootOffset"] - .001) / BUDGETS["contactResidual"]
    system = operator.T @ operator + contact.T @ contact
    back = np.linalg.solve(system, operator.T)
    constant = np.linalg.solve(system, contact.T @ (target / BUDGETS["contactResidual"]))
    projected, dual = center.copy(), np.zeros_like(center)
    previous_velocity = np.zeros((count - 2, 2))
    for iteration in range(maximum_iterations):
        velocity = back @ (projected - dual) + constant
        actual = operator @ velocity
        relaxed = 1.5 * actual - .5 * projected
        raw = relaxed + dual - center
        projected = center + raw / np.maximum(1, np.linalg.norm(raw, axis=1) / radii)[:, None]
        dual += relaxed - projected
        violation = float(np.maximum(np.linalg.norm(actual - center, axis=1) / radii - 1, 0).max())
        change = float(np.linalg.norm(velocity - previous_velocity, axis=1).max())
        previous_velocity = velocity
        if violation < 1e-7 and change < 1e-8 and iteration > 1000:
            break
    positions, velocities = position @ velocity, lift @ velocity
    accelerations = np.diff(velocities, axis=0) / dt[:, None]
    error = positions - target
    radius_peak = max(quadratic_peak(positions[i], velocities[i], accelerations[i] / 2, h) for i, h in enumerate(dt))
    error_peak = max(quadratic_peak(error[i], velocities[i] - (target[i + 1] - target[i]) / h,
                                    accelerations[i] / 2, h) for i, h in enumerate(dt))
    metrics = {"iterations": iteration + 1, "normalizedViolation": violation,
               "maximumRootOffset": radius_peak, "maximumContactResidual": error_peak,
               "maximumSpeed": float(np.linalg.norm(velocities, axis=1).max()),
               "maximumAcceleration": float(np.linalg.norm(accelerations, axis=1).max()),
               "rmsContactResidual": float(np.sqrt(np.mean(np.linalg.norm(error, axis=1) ** 2))),
               "sampledContactCumulativeTravel": float(np.linalg.norm(np.diff(error, axis=0), axis=1).sum()),
               "endpointSpeed": float(np.linalg.norm(velocities[[0, -1]], axis=1).max()),
               "solverConverged": violation < 1e-7 and change < 1e-8}
    # Feasibility follows the independently checked curve, not an optimizer flag
    # or a claim of optimality. A finite iteration cap is not proof of infeasibility.
    metrics["boundedCurveEstablished"] = (radius_peak <= BUDGETS["maximumRootOffset"] and error_peak <= BUDGETS["contactResidual"]
                                          and metrics["maximumSpeed"] <= BUDGETS["speed"]
                                          and metrics["maximumAcceleration"] <= BUDGETS["acceleration"])
    return positions, velocities, metrics


def run(project, name, output):
    project = project.resolve()
    if (project / ".git").exists() or not (project / ".animation-playground-validation-project").is_file():
        raise ValueError("Use the marked isolated validation project.")
    if not all(re.fullmatch(r"[A-Za-z0-9]+", v) for v in (name, output)):
        raise ValueError("Use immutable simple run names.")
    base = project / "LocomotionImplementation20261014"
    source, destination = base / name, base / output
    if destination.exists():
        raise FileExistsError("Preserve earlier fits.")
    summary = read(source / "summary.json")
    if not summary["applyPlanarRoot"] or not summary["recenter"]:
        raise ValueError("Use the origin-precision native-root source observation.")
    if not re.fullmatch(r"[A-Za-z0-9]+", summary["basis"]) or not summary["source"].startswith("Assets/") or ".." in summary["source"]:
        raise ValueError("Require project-local measured source paths.")
    for suffix in ["", ".meta"]:
        key = summary["source"] + suffix
        if digest(project / key) != summary["inputs"][key]:
            raise ValueError("Measured source import changed.")
    curves, results = [], []
    names = set()
    for row in summary["rows"]:
        if row["quality"] != 4:
            continue
        path = source / row["privateTrack"]["file"]
        if not path.resolve().is_relative_to(source.resolve()) or row["clip"] not in {"RapidTurningLeft", "RapidTurningRight_Mirror"} or row["clip"] in names:
            raise ValueError("Require the two distinct named source tracks inside the measured run.")
        names.add(row["clip"])
        if digest(path) != row["privateTrack"]["sha256"]:
            raise ValueError("Sole track receipt mismatch.")
        track = read(path)
        if track["clip"] != row["clip"] or track["quality"] != 4:
            raise ValueError("Named source/influence pairing mismatch.")
        times = np.array([f["seconds"] for f in track["frames"]])
        points = np.array([[[v["x"], v["y"], v["z"]] for v in f["points"]] for f in track["frames"]])
        tape_path = base / summary["basis"] / (row["clip"] + "-private-applied-poses.json")
        tape = read(tape_path)
        tape_hash = digest(tape_path)
        if tape["clipPath"] != summary["source"] or not np.array_equal(times.astype(np.float32),
                np.array([f["seconds"] for f in tape["frames"]], dtype=np.float32)):
            raise ValueError("Native yaw/root and sole samples must pair exactly.")
        native = np.array([[f["root"][key] for key in ["x", "z"]] for f in tape["frames"]])
        native -= native[0]
        candidates = np.where(((points[:, :, 1] >= -.001) & (points[:, :, 1] <= .001)).all(axis=0))[0]
        if not len(candidates):
            raise ValueError("No persistent near-floor point; this model cannot establish a curve.")
        vertex = min(candidates, key=lambda v: (float(np.linalg.norm(native + points[0, v, [0, 2]]
                                                                   - points[:, v][:, [0, 2]], axis=1).max()), int(v)))
        tangent = points[:, vertex][:, [0, 2]]
        desired = native + tangent[0] - tangent
        positions, velocities, metrics = fit(times, desired)
        if digest(path) != row["privateTrack"]["sha256"] or digest(tape_path) != tape_hash:
            raise ValueError("Measured input changed while fitting.")
        results.append({"clip": row["clip"], "quality": 4, "vertex": int(vertex), **metrics})
        curves.append({"schema": 1, "qualified": False, "rootSpace": "actor-origin", "clip": row["clip"],
                       "source": summary["source"], "sourceMetaSha256": digest(project / (summary["source"] + ".meta")),
                       "sourceSha256": summary["inputs"][summary["source"]],
                       "trackSha256": digest(path), "nativeTapeSha256": digest(tape_path),
                       "vertex": int(vertex), "side": int(track["sides"][vertex]), "budgets": BUDGETS,
                       "anchorXZ": tangent[0].tolist(),
                       "metrics": metrics, "frames": [{"seconds": float(t), "position": p.tolist(), "velocity": v.tolist()}
                                                       for t, p, v in zip(times, positions, velocities)]})
    if len(results) != 2:
        raise ValueError("Require both four-influence named turns.")
    destination.mkdir()
    for curve in curves:
        (destination / (curve["clip"] + "-private-root.json")).write_text(json.dumps(curve, allow_nan=False) + "\n", encoding="utf-8")
    payload = {"state": "offline-bounded-root-fit-not-contact-qualified-or-installed", "budgets": BUDGETS,
               "sourceSummarySha256": digest(source / "summary.json"), "rows": results,
               "scope": "Native 1.50s sources, zero endpoint velocity, one persistent sole point. Analytic quadratic root/velocity/acceleration bounds. Contact-error bound assumes linear interpolation of recorded sole motion; dense equipped replay and actual motor/collision/handoff proof remain required. Cumulative contact travel is reported, not accepted."}
    (destination / "summary.json").write_text(json.dumps(payload, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    return payload


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--run", default="TurnNativeContactOriginV1")
    parser.add_argument("--output", default="TurnRootCurveV1")
    args = parser.parse_args()
    print(json.dumps(run(args.project, args.run, args.output), indent=2))
