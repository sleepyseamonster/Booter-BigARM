# Saved Unity Golden Rock family

These three native v6 recipes translate the saved
`Assets/_Project/Art/Environment/Rocks/Source/ApprovedBoulderFamilyRecipe.asset`.
The Unity asset remains read-only. [Source receipt](./source-receipt.json.txt)
records its hash and the bounded adaptations.

| Preset | Source seed | Composition |
|---|---:|---|
| 01 Golden Rock | 2126351350 | Approved core and support masses |
| 02 Approved Variation A | -346006759 | Saved smaller variant, native seed bits 3948960537 |
| 03 Approved Variation B | 362337 | Saved smaller variant |

Each recipe preserves both masses' positions, quaternion rotations, physical
scales and shape seeds. Negative Unity seeds retain their unsigned 32-bit pattern.
Native geometry uses the source weathered-block field and surface relaxation
with the native shared triangulator. It does not refit the saved resting pose.

| Control | Saved baseline |
|---|---:|
| Fusion | 0.0657 m |
| Surface relaxation | 0.45 |
| Base voxel size at Detail 2 | 50 mm |
| Edge damage | 1.00 |
| Side grit | 0.22 |
| Underside shale | 0.20 |
| Side / top shale patches | 0 / 0 |
| Geology scale | 2.4 m |
| Surface variation | 0.346 |
| Cracks | 0.36 |
| Worn shine | 0.099 |
| Dust color, sRGB | 0.32, 0.30, 0.28 |

The saved recipe and current Unity defaults supersede older prose that lists
grit 0.417 and underside shale 0.905. Dust strength remains the native preserved
material default 0.06. Unity's per-seed charcoal tint/color-variation formula and
complete material shader are not reproduced; surface appearance uses native
shading and the original texture library.

Choose a preset, edit the two source masses or surface controls, Apply recipe,
then Save recipe. The workbench's working file is separate from this library.
Overall scale changes the whole physical result. Source yaw/pitch/roll are
adjustments to its captured quaternion pose. Seed changes primitive chips;
it does not run Unity's full random composition planner. This is an authoring
family, with live terrain placement/streaming integration still pending.

Reproduce into a new directory inside Engine, from the repository root:

```sh
python3 Engine/Tools/import_golden_rock.py Assets/_Project/Art/Environment/Rocks/Source/ApprovedBoulderFamilyRecipe.asset Engine/out/golden-reimport
```

See [transfer result and verification](../../Docs/GOLDEN_ROCK_TRANSFER_RESULT.md).
