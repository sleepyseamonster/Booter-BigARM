"""Synthetic chunk-identity study; no Unity, terrain files, or production saves.

Placement uses keyed hash candidates and order-independent neighbour priority.
This is not Random123 or Bridson's algorithm, and is not a production generator.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import random
import time
from functools import lru_cache
from pathlib import Path

IDENTITY_SIZE = 256
CANDIDATE_SPACING = 32
MINIMUM_SPACING = 12
SEED = 1234
VERSION = 1
RECIPE = "synthetic-clutter-study"
LOW, HIGH = -512, 512


def key(gx: int, gz: int, channel: str) -> bytes:
    payload = f"synthetic-world|{VERSION}|{SEED}|{RECIPE}|{gx}|{gz}|{channel}"
    return hashlib.sha256(payload.encode("ascii")).digest()


@lru_cache(maxsize=None)
def candidate(gx: int, gz: int):
    if not (LOW // CANDIDATE_SPACING <= gx < HIGH // CANDIDATE_SPACING
            and LOW // CANDIDATE_SPACING <= gz < HIGH // CANDIDATE_SPACING):
        return None
    xj = int.from_bytes(key(gx, gz, "x")[:8], "big") / 2**64
    zj = int.from_bytes(key(gx, gz, "z")[:8], "big") / 2**64
    x = (gx + .1 + .8 * xj) * CANDIDATE_SPACING
    z = (gz + .1 + .8 * zj) * CANDIDATE_SPACING
    # Synthetic protected-site mask, deliberately crossing identity-cell borders.
    if x * x + z * z < 96**2:
        return None
    return (x, z, key(gx, gz, "priority"), key(gx, gz, "id")[:16].hex())


def accepted(gx: int, gz: int):
    current = candidate(gx, gz)
    if current is None:
        return None
    x, z, priority, stable_id = current
    # Jitter stays inside its candidate cell. This halo covers possible conflicts.
    halo = math.ceil(MINIMUM_SPACING / CANDIDATE_SPACING) + 1
    for nx in range(gx - halo, gx + halo + 1):
        for nz in range(gz - halo, gz + halo + 1):
            if (nx, nz) == (gx, gz):
                continue
            other = candidate(nx, nz)
            if other is None:
                continue
            ox, oz, op, oid = other
            if ((x - ox)**2 + (z - oz)**2 < MINIMUM_SPACING**2
                    and (op, oid) < (priority, stable_id)):
                return None
    return {"id": stable_id, "x": x, "z": z,
            "owner": [gx // (IDENTITY_SIZE // CANDIDATE_SPACING),
                      gz // (IDENTITY_SIZE // CANDIDATE_SPACING)]}


def generate_region(cx: int, cz: int, region_size: int):
    count = region_size // CANDIDATE_SPACING
    output = []
    for gx in range(cx * count, (cx + 1) * count):
        for gz in range(cz * count, (cz + 1) * count):
            point = accepted(gx, gz)
            if point is not None:
                output.append(point)
    return output


def generate_cell(cx: int, cz: int):
    return generate_region(cx, cz, IDENTITY_SIZE)


def digest(records):
    payload = json.dumps(sorted(records, key=lambda p: p["id"]),
                         sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(payload.encode("ascii")).hexdigest()


def study():
    cells = [(x, z) for x in range(LOW // IDENTITY_SIZE, HIGH // IDENTITY_SIZE)
             for z in range(LOW // IDENTITY_SIZE, HIGH // IDENTITY_SIZE)]
    shuffled = cells.copy()
    random.Random(19).shuffle(shuffled)  # Changes traversal only, never placement.
    orders = [cells, list(reversed(cells)), shuffled]
    results, elapsed = [], []
    for order in orders:
        candidate.cache_clear()
        start = time.perf_counter()
        records = [p for cell in order for p in generate_cell(*cell)]
        elapsed.append((time.perf_counter() - start) * 1000)
        results.append(records)
    hashes = [digest(r) for r in results]
    assert len(set(hashes)) == 1, "Generation-order dependence"
    records = results[0]
    assert len({p["id"] for p in records}) == len(records), "Duplicate IDs"
    for p in records:
        assert p["owner"] == [math.floor(p["x"] / IDENTITY_SIZE),
                              math.floor(p["z"] / IDENTITY_SIZE)]
        assert p["x"]**2 + p["z"]**2 >= 96**2

    minimum = float("inf")
    cross_border_minimum = float("inf")
    for i, a in enumerate(records):
        for b in records[i + 1:]:
            separation = math.hypot(a["x"] - b["x"], a["z"] - b["z"])
            minimum = min(minimum, separation)
            if a["owner"] != b["owner"]:
                cross_border_minimum = min(cross_border_minimum, separation)
    assert minimum >= MINIMUM_SPACING, "Spacing failure across or inside cells"

    batch_counts, partition_hashes = {}, {}
    for size in (64, 128, 256):
        candidate.cache_clear()
        regions = [(x, z) for x in range(LOW // size, HIGH // size)
                   for z in range(LOW // size, HIGH // size)]
        fresh = [p for cell in reversed(regions)
                 for p in generate_region(*cell, size)]
        partition_hashes[str(size)] = digest(fresh)
        assert partition_hashes[str(size)] == hashes[0], "Work partition changed generation"
        batches = {}
        for p in fresh:
            group = (math.floor(p["x"] / size), math.floor(p["z"] / size))
            batches.setdefault(group, []).append(p)
        flattened = [p for points in batches.values() for p in points]
        assert digest(flattened) == hashes[0], "Render repartition changed identity"
        batch_counts[str(size)] = len(batches)

    # Sparse mutation survives discarding and reconstructing presentation data.
    changed_id = min(p["id"] for p in records)
    sparse_delta = json.loads(json.dumps({changed_id: {"depleted": True}}))
    candidate.cache_clear()
    recreated = [p for cell in shuffled for p in generate_cell(*cell)]
    remaining = [p for p in recreated if not sparse_delta.get(p["id"], {}).get("depleted", False)]
    assert len(remaining) == len(records) - 1
    assert changed_id not in {p["id"] for p in remaining}

    return {
        "mode": "synthetic_cpu_reference_study",
        "scope": "1,024 metre square synthetic X/Z domain; no terrain samples, Unity objects, rendering, physics or production saves",
        "identityCellMetres": IDENTITY_SIZE,
        "identityCellCount": len(cells),
        "rawCandidateCount": ((HIGH - LOW) // CANDIDATE_SPACING)**2,
        "acceptedRecords": len(records),
        "minimumSpacingMetres": minimum,
        "minimumCrossBorderSpacingMetres": cross_border_minimum,
        "requiredSpacingMetres": MINIMUM_SPACING,
        "orderDigests": hashes,
        "generationTimesMs": elapsed,
        "occupiedRenderBatchesByMetres": batch_counts,
        "freshWorkPartitionDigests": partition_hashes,
        "checks": {"orderIndependent": True, "uniqueOwnershipAndIds": True,
                   "crossBorderSpacing": True, "protectedSiteExclusion": True,
                   "renderRepartitionPreservesIds": True,
                   "sparseDepletionSurvivesRegeneration": True},
        "limits": "Does not establish Unity performance, GPU/CPU bit parity, production density, terrain grounding, maximal Poisson packing, or save migration correctness."
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    result = study()
    destination = Path(args.output).resolve()
    allowed = (Path.cwd() / "Logs").resolve()
    if allowed not in destination.parents or destination.suffix.lower() != ".json":
        raise ValueError("Write this reference study only to a .json file under the current checkout's Logs folder")
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
