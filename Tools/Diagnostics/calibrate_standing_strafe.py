"""Private lateral source proposal; does not qualify a runtime movement family."""
import argparse
import json
from pathlib import Path
import numpy as np
from calibrate_forward_gaits import calibrate


def screen_sources(root: Path, output: Path):
    if output.exists():
        raise FileExistsError("Preserve the previous source screen.")
    rows = []
    for path in sorted(root.glob("*.json")):
        if path.name == "summary.json":
            continue
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        times = np.array([s["time"] for s in data["samples"]])
        points = np.array([s["points"] for s in data["samples"]])
        sign = -1 if "Left" in path.stem else 1
        count = round(.1 / float(np.median(np.diff(times))))
        sides = []
        for side in (".L", ".R"):
            ids = np.array([i for i, v in enumerate(data["vertices"]) if any(side in p["bone"] for p in v)], dtype=int)
            best = None
            for first in range(len(times) - count):
                stop = first + count
                part = points[first:stop + 1]
                low = part[:, ids, 1].min(axis=1)
                if max(low - part[:, :, 1].min(axis=1)) > .002:
                    continue
                t = times[first:stop + 1] - times[first]
                centered = t - t.mean()
                for vertex in ids:
                    if max(part[:, vertex, 1] - low) > .008:
                        continue
                    speed = -sign * float(np.dot(centered, part[:, vertex, 0]) / np.dot(centered, centered))
                    if not .2 < speed < 7:
                        continue
                    world = part[:, vertex][:, [0, 2]] + np.outer(t, [sign * speed, 0])
                    excursion = float(np.linalg.norm(world - world[0], axis=1).max())
                    travel = float(np.linalg.norm(np.diff(world, axis=0), axis=1).sum())
                    score = excursion + .15 * travel
                    if best is None or score < best[0]:
                        best = (score, dict(side=side, vertex=int(vertex), nativeLateralSpeed=speed,
                                           excursion=excursion, cumulativeTravel=travel,
                                           start=float(times[first]), end=float(times[stop])))
            sides.append(best[1] if best else dict(side=side, unavailable=True))
        correction = -.91 - points[:, :, 1].min(axis=1)
        rows.append(dict(path=data["path"], sha256=data["sha256"], metaSha256=data["metaSha256"],
                         seconds=data["length"], looping=data["isLooping"],
                         maximumPositiveFloorCorrection=float(max(0, correction.max())),
                         maximumContinuousLoweringToFloor=float(max(0, -correction.min())),
                         bestBilateral100msWindows=sides))
    output.write_text(json.dumps(dict(state="source-selection-not-qualification", rows=rows,
                                    scope="Best persistent near-ground fixed vertex per foot over 100ms at individually fitted lateral speed. "
                                          "Horizontal fore/aft residual is retained. Continuous lowering is reported, not applied during flight. "
                                          "Actual-driver native timing, equipment, transitions and physics need separate proof."), indent=2) + "\n", encoding="utf-8")


def run(root: Path, output: Path):
    if output.exists():
        raise FileExistsError("Preserve the previous calibration.")
    converted = root / "LateralCalibrationInputs"
    converted.mkdir(exist_ok=False)
    for side in ("Left", "Right"):
        name = "StandingStrafeWalk" + side + "Review"
        data = json.loads((root / (name + ".json")).read_text(encoding="utf-8-sig"))
        sign = -1 if side == "Left" else 1

        def rotate(point):
            return [point[2], point[1], point[0] * sign]

        rows = dict(path=data["path"], floor=-.91, globalSkinWeights="FourBones",
                    ids=[dict(side=0 if any(".L" in p["bone"] for p in vertex) else 1, vertex=i)
                         for i, vertex in enumerate(data["vertices"])],
                    frames=[dict(seconds=s["time"], hips=[0, 0, 0], left=rotate(s["left"]),
                                 right=rotate(s["right"]), vertices=[rotate(p) for p in s["points"]])
                            for s in data["samples"]])
        (converted / (name + ".json")).write_text(json.dumps(rows), encoding="utf-8")
    sources = (("Left", "StandingStrafeWalkLeftReview", .55, (.09, .25, .59, .75), 9 / 120),
               ("Right", "StandingStrafeWalkRightReview", .55, (.025, .34, .525, .84), 9 / 120))
    report = calibrate(converted, False, sources, maximum_lift=.08, regularization=.15, minimum_native_speed=.2)
    report["limitations"] = ("Lateral coordinate rotation for sole-window selection only. Runtime uses uniform native source time "
                              "and measured lateral metres/cycle with the unequal native contact phases; "
                              "the proposed forward-style sourceTimeMap is deliberately unused. No diagonal/backward coverage.")
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--screen-sources", action="store_true")
    args = parser.parse_args()
    (screen_sources if args.screen_sources else run)(args.directory, args.output)
