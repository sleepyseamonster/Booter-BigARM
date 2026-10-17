"""Offline fixed-vertex sprint timing trials; writes a new recipe, never Unity assets."""
import argparse
import json
import re
from pathlib import Path
import numpy as np
from calibrate_forward_gaits import calibrate


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("tracks", type=Path)
    parser.add_argument("profile", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError("Preserve earlier trials")
    gait = args.profile.read_text().split("  - role:")[-1]
    old_distance = float(re.search(r"metresPerCycle: (.+)", gait)[1])
    speed = float(re.search(r"nominalSpeed: (.+)", gait)[1])
    old_map = np.array([(float(a), float(b)) for a, b in re.findall(r"semantic: ([^\n]+)\n\s+native: ([^\n]+)", gait)])
    source = json.loads((args.tracks / "FullSprintStandardReview.json").read_text(encoding="utf-8-sig"))
    times = np.array([f["seconds"] for f in source["frames"]]); period = times[-1]
    xyz = np.array([f["vertices"] for f in source["frames"]])
    xyz = np.concatenate((xyz[:-1], xyz[:-1], xyz))
    times = np.r_[times[:-1] - period, times[:-1], times + period]
    measured = calibrate(args.tracks, True,
        (("Sprint", "FullSprintStandardReview", speed, (.078, .333, .578, .833), .05),),
        maximum_lift=.08, regularization=.08)["gaits"][0]
    patches = measured["patches"]; contacts = np.array(measured["nativeContacts"])

    def score(distance, mapping):
        rates = np.diff(mapping[:, 1]) / np.diff(mapping[:, 0]) * period * speed / distance
        drifts = []
        for patch in patches:
            first, stop, vertex = patch["first"], patch["stop"], patch["vertex"]
            native = times[first:stop + 1] / period
            native = np.where(native < contacts[0], native + 1, native)
            semantic = np.unwrap(np.interp(native, mapping[:, 1], mapping[:, 0]) * 2*np.pi)/(2*np.pi)
            world = xyz[first:stop + 1, vertex][:, [0, 2]] + np.column_stack((np.zeros(len(native)), distance*semantic))
            drifts.append(float(np.linalg.norm(world-world[0], axis=1).max()))
        return dict(maximumExcursion=max(drifts), excursions=drifts, minimumRate=float(rates.min()), maximumRate=float(rates.max()))

    trials = []
    for distance in [old_distance, old_distance*1.025, old_distance*1.05]:
        for regularization in np.linspace(0, 1, 101):
            knots = [(0., contacts[0]), (1., contacts[0]+1)]
            for plant, patch in enumerate(patches):
                first, stop, vertex = patch["first"], patch["stop"], patch["vertex"]
                center = np.interp(patch["centerPhase"]*period, times, xyz[:, vertex, 2])
                semantic = plant*.25+(center-xyz[first:stop+1, vertex, 2])/distance
                for u, n in zip(semantic, times[first:stop+1]/period):
                    if u < 0: u += 1; n += 1
                    baseline = np.interp(u, np.arange(5)*.25, np.r_[contacts, contacts[0]+1])
                    knots.append((float(u), float(regularization*n+(1-regularization)*baseline)))
            mapping = np.array(sorted(knots))
            keep = np.r_[True, np.all(np.diff(mapping, axis=0) > 1e-6, axis=1)]
            mapping = mapping[keep]
            if np.any(np.diff(mapping, axis=0) <= 0): continue
            result = score(distance, mapping)
            if result["minimumRate"] < .65 or result["maximumRate"] > 1.35: continue
            trials.append(dict(distance=distance, regularization=float(regularization), mapping=mapping.tolist(), **result))
    baseline = dict(distance=old_distance, **score(old_distance, old_map))
    # Prefer the requested slower cadence only when the persistent contact screen improves.
    eligible = [t for t in trials if t["distance"] > old_distance and t["maximumExcursion"] < baseline["maximumExcursion"]]
    selected = min(eligible or trials, key=lambda t: t["maximumExcursion"])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(dict(scope="Offline source-only trial; actual graph/blends require separate verification",
        baseline=baseline, selected=selected, trials=trials), indent=2)+"\n")
    print(json.dumps(dict(baseline=baseline, selected={k:v for k,v in selected.items() if k != "mapping"}, trials=len(trials)), indent=2))


if __name__ == "__main__":
    main()
