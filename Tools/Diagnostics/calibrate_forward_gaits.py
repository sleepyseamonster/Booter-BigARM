"""Bounded native gait calibration proposals using fixed sole identities."""
import argparse
import json
from pathlib import Path
import numpy as np


SOURCES = (
    ("Walk", "MCU_am_Loco_Walk_Fwd_NoRM", 1.4, (.1875, .421875, .703125, .921875), .18),
    ("Run", "MCU_am_Loco_Jog_Fwd_NoRM", 2.7, (.18605, .44186, .69767, .93023), .10),
    ("Sprint", "MCU_am_Loco_Run_Fwd_NoRM", 3.8, (.18919, .43243, .67568, .91892), 8 / 120),
)


def calibrate(root, align_stance=False, sources=None):
    reports = []
    selected_sources = SOURCES if sources is None else sources
    for role, name, nominal, seeds, window_seconds in selected_sources:
        data = json.loads((root / (name + ".json")).read_text(encoding="utf-8-sig"))
        if data.get("globalSkinWeights") != "FourBones":
            raise ValueError("Use the corrected four-bone raw tracks.")
        t = np.array([f["seconds"] for f in data["frames"]])
        xyz = np.array([f["vertices"] for f in data["frames"]])
        period = float(t[-1])
        original_t = t.copy()
        hips = np.array([f["hips"] for f in data["frames"]])
        feet = [np.array([f["left"] for f in data["frames"]]), np.array([f["right"] for f in data["frames"]])]
        if align_stance:
            t = np.r_[t[:-1] - period, t[:-1], t + period]
            xyz = np.concatenate((xyz[:-1], xyz[:-1], xyz))
            hips = np.concatenate((hips[:-1], hips[:-1], hips))
            feet = [np.concatenate((f[:-1], f[:-1], f)) for f in feet]
        side_ids = np.array([i["side"] for i in data["ids"]])
        floor = data["floor"]
        minimum = xyz[:, :, 1].min(axis=1)
        dt = np.median(np.diff(t))
        count = int(round(window_seconds / dt))
        if align_stance and role == "Run":
            count = 8  # Persistent mid-stance patch; the complete rolling stance is longer.
        patches = []
        for plant, seed in enumerate(seeds):
            side = 1 if plant % 2 == 0 else 0
            ids = np.flatnonzero(side_ids == side)
            best = None
            for first in range(len(t) - count):
                stop = first + count
                center = (t[first] + t[stop]) / (2 * period)
                if not 0 <= center < 1 or abs(center - seed) > .075:
                    continue
                under_hip = float(np.interp(center * period, t, feet[side][:, 2] - hips[:, 2]))
                if align_stance and abs(under_hip) > .06:
                    continue
                part = xyz[first:stop + 1]
                correction = floor - minimum[first:stop + 1]
                if correction.min() < -.03 or correction.max() > .07:
                    continue
                low = part[:, ids, 1].min(axis=1)
                # The selected foot must be the lower foot throughout its window.
                if np.max(low - minimum[first:stop+1]) > .002:
                    continue
                times = t[first:stop + 1] - t[first]
                ct = times - times.mean()
                for vertex in ids:
                    height = part[:, vertex, 1] - low
                    if height.max() > .008:
                        continue
                    p = part[:, vertex][:, [0, 2]]
                    speed = -float(np.dot(ct, p[:, 1] - p[:, 1].mean()) / np.dot(ct, ct))
                    if not .5 < speed < 7:
                        continue
                    world = p + np.column_stack((np.zeros(len(times)), times * speed))
                    lateral_excursion = float(np.abs(p[:, 0] - p[0, 0]).max())
                    if lateral_excursion > .02:
                        continue
                    excursion = float(np.linalg.norm(world - world[0], axis=1).max())
                    travel = float(np.linalg.norm(np.diff(world, axis=0), axis=1).sum())
                    score = excursion + travel * .15 + abs(center - seed) * .03 + float(height.mean())
                    if align_stance:
                        score += abs(under_hip) * .15
                    if best is None or score < best[0]:
                        best = (score, dict(side=side, vertex=int(vertex), identity=data["ids"][vertex],
                            first=first, stop=stop, start=float(t[first]), end=float(t[stop]),
                            centerPhase=float(center), nativeFitSpeed=speed, fittedExcursion=excursion,
                            underHipOffset=under_hip,
                            fittedCumulativeTravel=travel, maximumPatchGroundGap=float(height.max())))
            if best is None:
                raise ValueError(f"No bounded bilateral patch: {role}, plant {plant}")
            patches.append(best[1])
        # Contacts are measured support-window midpoints. No equal-quarter source
        # assumption: only the shared semantic clock has quarter-cycle contacts.
        contacts = np.array([p["centerPhase"] for p in patches])
        speed = float(np.median([p["nativeFitSpeed"] for p in patches]))
        distance = speed * period
        knots = [(0., float(contacts[0])), (1., float(contacts[0] + 1))]
        for index, p in enumerate(patches):
            first, stop, vertex = p["first"], p["stop"], p["vertex"]
            center_z = float(np.interp(p["centerPhase"] * period, t, xyz[:, vertex, 2]))
            u = index * .25 + (center_z - xyz[first:stop+1, vertex, 2]) / distance
            if np.any(np.diff(u) <= 0):
                raise ValueError("A planted window must retain rearward sole motion.")
            for semantic, native in zip(u, t[first:stop+1] / period):
                if semantic < 0:
                    semantic += 1; native += 1
                knots.append((float(semantic), float(native)))
            p["warpedExcursion"] = float(np.abs(xyz[first:stop+1, vertex, 0] - xyz[first, vertex, 0]).max())
            p["warpedCumulativeTravel"] = float(np.abs(np.diff(xyz[first:stop+1, vertex, 0])).sum())
        knots.sort()
        if np.any(np.diff(np.array(knots)[:, 0]) <= 0) or np.any(np.diff(np.array(knots)[:, 1]) <= 0):
            raise ValueError("The native source map must be strictly increasing.")
        # Regularize exact stance locking toward the native contact-centre map.
        # A rolling sole does not need mathematically zero drift at the cost of
        # abrupt source-rate changes. Keep half the measured correction first.
        baseline_u = np.arange(5) * .25
        baseline_native = np.r_[contacts, contacts[0] + 1]
        knots = [(u, float(.5 * native + .5 * np.interp(u, baseline_u, baseline_native))) for u, native in knots]
        clean = [knots[0]]
        for knot in knots[1:]:
            if knot[0] - clean[-1][0] < 1e-6 or knot[1] - clean[-1][1] < 1e-6:
                if knot[0] == 1:
                    clean[-1] = knot
                continue
            clean.append(knot)
        knots = clean
        slopes = np.diff(np.array(knots)[:, 1]) / np.diff(np.array(knots)[:, 0])
        native_rates = slopes * nominal / speed
        for p in patches:
            first, stop, vertex = p["first"], p["stop"], p["vertex"]
            native = t[first:stop+1] / period
            native = np.where(native < contacts[0], native + 1, native)
            semantic = np.interp(native, np.array(knots)[:, 1], np.array(knots)[:, 0])
            # Unwrap a right-foot window crossing semantic zero.
            semantic = np.unwrap(semantic * 2 * np.pi) / (2 * np.pi)
            world = xyz[first:stop+1, vertex][:, [0, 2]] + np.column_stack((np.zeros(len(semantic)), distance * semantic))
            p["regularizedExcursion"] = float(np.linalg.norm(world - world[0], axis=1).max())
            p["regularizedCumulativeTravel"] = float(np.linalg.norm(np.diff(world, axis=0), axis=1).sum())
        original_minimum = np.interp(original_t, t, minimum)
        signed = np.maximum(0, floor - original_minimum)
        for p in patches:
            first, stop = p["first"], p["stop"]
            # 25ms feather outside each measured stance window. Positive-only
            # projection remains elsewhere, so native flight is not pulled down.
            for i, at in enumerate(original_t):
                if align_stance:
                    at += round(((t[first] + t[stop]) * .5 - at) / period) * period
                fade = min((at - t[first] + .025) / .025, (t[stop] + .025 - at) / .025, 1)
                if fade > 0 and floor < original_minimum[i]:
                    signed[i] = min(signed[i], (floor - original_minimum[i]) * fade)
        for p in patches:
            first, stop, vertex = p["first"], p["stop"], p["vertex"]
            times = t[first:stop+1] - t[first]
            planar = xyz[first:stop+1, vertex][:, [0, 2]]
            world = planar + np.column_stack((np.zeros(len(times)), times * speed))
            displacement = np.linalg.norm(world - world[0], axis=1)
            travel = np.linalg.norm(np.diff(world, axis=0), axis=1)
            p["familySpeedExcursion"] = float(displacement.max())
            p["familySpeedCumulativeTravel"] = float(travel.sum())
            p["medianDriftSpeed"] = float(np.median(travel / np.diff(times)))
            p["peakDriftSpeed"] = float(np.max(travel / np.diff(times)))
        reports.append(dict(role=role, source=data["path"], nominalSpeed=nominal, duration=period,
            regularization=.5,
            nativeContactSpeed=speed, metresPerCycle=distance, nativeContacts=contacts.tolist(),
            sourceTimeMap=knots, pureMinimumEffectiveRate=float(native_rates.min()),
            pureMaximumEffectiveRate=float(native_rates.max()),
            nominalNativePlaybackRate=nominal / speed, patches=patches,
            correction=[[float(at / period), float(value)] for at, value in zip(original_t, signed)],
            maximumLift=float(signed.max()), maximumLowering=float(-signed.min())))
    return dict(state="bounded-native-family-calibration-proposal", nativeSourcesUnchanged=True, stanceStageAligned=align_stance,
        budgets=dict(stancePatchExcursion=.02, windowGroundGap=.008, lowering=.03, lift=.07,
                     blendSegmentRateMinimum=.65, blendSegmentRateMaximum=1.35,
                     significantContributorWeight=.1, referenceSpeeds=[s[2] for s in selected_sources]),
        limitations="Contact-window proposals and native-only drift; semantic mapping/blends/equipment still require qualification.",
        gaits=reports)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--align-stance", action="store_true")
    parser.add_argument("--sprint-arm-drive", action="store_true",
                        help="Calibrate the authored SprintArmDriveReview source at the requested 5m/s review pace.")
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError("Preserve existing proposals.")
    sources = (("Sprint", "SprintArmDriveReview", 5.0, (.18919, .43243, .67568, .91892), 8 / 120),) if args.sprint_arm_drive else None
    report = calibrate(args.directory, args.align_stance, sources)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps([{k: g[k] for k in ("role", "nativeContactSpeed", "metresPerCycle", "nativeContacts", "maximumLift", "maximumLowering")} |
                      dict(maximumExcursion=max(p["familySpeedExcursion"] for p in g["patches"]))
                      for g in report["gaits"]], indent=2))


if __name__ == "__main__":
    main()
