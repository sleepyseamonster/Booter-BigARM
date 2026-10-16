"""Fit two-dimensional stance velocity; private pose tapes are never published."""
import argparse
import json
from pathlib import Path
import numpy as np


def screen(directory: Path, output: Path):
    if output.exists():
        raise FileExistsError(output)
    rows = []
    for path in sorted(directory.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        if not isinstance(data, dict) or "samples" not in data:
            continue
        times = np.array([s["time"] for s in data["samples"]])
        points = np.array([s["points"] for s in data["samples"]])
        count = round(.1 / float(np.median(np.diff(times))))
        sides = []
        for side in (".L", ".R"):
            ids = np.array([i for i, v in enumerate(data["vertices"]) if any(side in p["bone"] for p in v)], dtype=int)
            candidates = []
            for first in range(len(times) - count):
                stop = first + count
                part = points[first:stop + 1]
                low = part[:, ids, 1].min(axis=1)
                if max(low - part[:, :, 1].min(axis=1)) > .004:
                    continue
                t = times[first:stop + 1] - times[first]
                centered = t - t.mean()
                for vertex in ids:
                    if max(part[:, vertex, 1] - low) > .008:
                        continue
                    velocity = -centered @ part[:, vertex][:, [0, 2]] / (centered @ centered)
                    speed = float(np.linalg.norm(velocity))
                    if not .2 < speed < 8:
                        continue
                    world = part[:, vertex][:, [0, 2]] + np.outer(t, velocity)
                    excursion = float(np.linalg.norm(world - world[0], axis=1).max())
                    travel = float(np.linalg.norm(np.diff(world, axis=0), axis=1).sum())
                    candidates.append((excursion + .15 * travel, dict(side=side, vertex=int(vertex),
                        velocity=velocity.tolist(), speed=speed,
                        heading=float(np.degrees(np.arctan2(velocity[0], velocity[1]))),
                        excursion=excursion, cumulativeTravel=travel,
                        start=float(times[first]), end=float(times[stop]))))
            candidates.sort(key=lambda c: c[0])
            sides.append(candidates[0][1] if candidates else dict(side=side, unavailable=True))
        rows.append(dict(path=data["path"], sha256=data["sha256"], seconds=data["length"],
                         bestBilateral100msWindows=sides))
    output.write_bytes((json.dumps(dict(state="source-screen-not-runtime-qualification", rows=rows), indent=2) + "\n").encode())


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    screen(args.directory, args.output)
