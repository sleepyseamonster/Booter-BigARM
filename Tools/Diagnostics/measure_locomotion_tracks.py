"""Offline tracked-vertex selection evidence; no Unity/player simulation."""
import argparse
import json
from pathlib import Path
import numpy as np


def measure(path, height_allowance):
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    frames = data["frames"]
    times = np.array([f["seconds"] for f in frames])
    positions = np.array([f["vertices"] for f in frames])
    dt = np.diff(times)
    sides = np.array([v["side"] for v in data["ids"]])
    floor = data["floor"]
    # Vertical projection is used only for contact classification. Horizontal
    # tracks always retain a fixed renderer/mesh vertex identity.
    minimum = positions[:, :, 1].min(axis=1)
    correction = np.maximum(0, floor - minimum)
    y = positions[:, :, 1] + correction[:, None]
    velocity = np.diff(positions, axis=0) / dt[:, None, None]
    supported_vertical_velocity = np.diff(y, axis=0) / dt[:, None]
    midpoint_y = (y[:-1] + y[1:]) * .5
    supported = (midpoint_y <= floor + height_allowance) & (np.abs(supported_vertical_velocity) <= .35) & (velocity[:, :, 2] < -.05)
    windows = []
    for vertex in range(len(sides)):
        mask = supported[:, vertex]
        edges = np.diff(np.r_[False, mask, False].astype(int))
        for first, stop in zip(np.flatnonzero(edges == 1), np.flatnonzero(edges == -1)):
            if times[stop] - times[first] < .06:
                continue
            points = positions[first:stop + 1, vertex][:, [0, 2]]
            ts = times[first:stop + 1] - times[first]
            # Least-squares forward speed minimizes tangent drift for this one
            # tracked patch, rather than relying on endpoint displacement.
            centered_t = ts - ts.mean()
            fit_speed = -float(np.dot(centered_t, points[:, 1] - points[:, 1].mean()) / np.dot(centered_t, centered_t))
            windows.append(dict(vertex=vertex, side=int(sides[vertex]), first=int(first), stop=int(stop),
                                start=float(times[first]), end=float(times[stop]), duration=float(ts[-1]),
                                fitSpeed=fit_speed))
    # Choose the longest persistent patch first per foot, with no overlapping
    # intervals. This avoids counting hundreds of coincident sole vertices as
    # hundreds of independent plants. Gaps/rolling are recorded, not filled in.
    chosen = []
    for side in (0, 1):
        for window in sorted((w for w in windows if w["side"] == side), key=lambda w: -w["duration"]):
            if any(w["side"] == side and max(w["start"], window["start"]) < min(w["end"], window["end"]) for w in chosen):
                continue
            chosen.append(window)
    chosen.sort(key=lambda w: w["start"])
    fit = float(np.median([w["fitSpeed"] for w in chosen])) if chosen else None
    nominal = 1.4 if "Walk" in path.stem else 2.7 if "Jog" in path.stem or path.stem in ("StandardRun", "RunTwoCycles") else 3.8
    for window in chosen:
        first, stop, vertex = window["first"], window["stop"], window["vertex"]
        ts = times[first:stop+1] - times[first]
        points = positions[first:stop+1, vertex][:, [0, 2]]
        for label, speed in (("nominal", nominal), ("fitted", fit)):
            world = points + np.column_stack((np.zeros(len(ts)), ts * speed))
            excursion = np.linalg.norm(world - world[0], axis=1)
            travel = np.linalg.norm(np.diff(world, axis=0), axis=1)
            drift = travel / np.diff(ts)
            window[label] = dict(maximumAnchorExcursion=float(excursion.max()), cumulativeTravel=float(travel.sum()),
                                 peakDriftSpeed=float(drift.max()), medianDriftSpeed=float(np.median(drift)))
        window["identity"] = data["ids"][vertex]
        window["maximumProjectedGroundGap"] = float(y[first:stop+1, vertex].max() - floor)
    bones = np.array([[f["left"], f["right"]] for f in frames])
    hips = np.array([f["hips"] for f in frames])
    summary = dict(name=path.stem, path=data["path"], duration=data["duration"], samples=len(times), vertices=len(sides),
                   floor=floor, rawMinimum=float(minimum.min()), maximumProposedProjection=float(correction.max()),
                   measurementMilliseconds=data["measurementMilliseconds"], nominalSpeed=nominal, fittedContactSpeed=fit,
                   contactPatches=chosen,
                   maximumNominalPatchExcursion=max((w["nominal"]["maximumAnchorExcursion"] for w in chosen), default=None),
                   maximumFittedPatchExcursion=max((w["fitted"]["maximumAnchorExcursion"] for w in chosen), default=None),
                   seamFootGap=float(np.linalg.norm(bones[-1] - bones[0], axis=1).max()),
                   seamHipsGap=float(np.linalg.norm(hips[-1] - hips[0])))
    return summary


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--height-allowance", type=float, default=.012)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError("Preserve prior selection evidence.")
    if not 0 < args.height_allowance <= .03:
        raise ValueError("Near-ground proposals are bounded to 30mm and remain unqualified.")
    rows = [measure(p, args.height_allowance) for p in sorted(args.directory.glob("*.json")) if p.name != "summary.json"]
    args.output.write_text(json.dumps(dict(state="source-selection-proposals-not-acceptance",
        definition=dict(sampleRate=120, influenceLimit=4, supportedHeightAllowance=args.height_allowance, verticalSpeedLimit=.35,
                        minimumPatchDuration=.06, selection="longest nonoverlapping fixed vertex patches per side",
                        rolling="patch gaps and replacement identities remain distinct; supported vertical velocity includes proposed plane projection",
                        targetMaximumAnchorExcursion=.02, limitations="Finite samples, no equipment, vertical classification projection only."),
        sources=rows), indent=2) + "\n", encoding="utf-8")
    print(json.dumps([{k: r[k] for k in ("name", "fittedContactSpeed", "maximumNominalPatchExcursion", "maximumFittedPatchExcursion", "seamFootGap")} for r in rows], indent=2))


if __name__ == "__main__":
    main()
