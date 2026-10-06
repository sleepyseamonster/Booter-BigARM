# Safe Unity repository publication — 2026-10-06

Scope: commit and push the intentional saved work, preserve local editor and recovery artifacts, and keep the Badwater implementation goal paused. This receipt records publication proof; it does not accept gameplay feel or visual quality.

## History and preservation

- Original local `main`: `046fceb46fc1d19692f11c0fa1ed43f48dbd90af`, with 159 unpublished commits above remote `1d7d146c9c3f062221364a9783a5f316c53187b5`.
- Preparation checkout: `codex/safe-publication-20261006`, isolated from the open Unity project.
- All 159 commits retain their author, author date, message, and sequence. The two large Blender source paths use Git LFS in the preparation history. An independent comparison verified all 159 commit trees: only `.gitattributes` and LFS pointers differ from their original trees.
- All 25 converted historical Blender blobs match their original SHA-256 and byte count, totalling 1,624,066,490 bytes. The current badlands source is also preserved through LFS. No remote history rewrite or force push is required: the published history retains the remote base as an ancestor.
- Original history and all 61 dirty/untracked file snapshots are backed up locally under `/Users/worldbuilder/Desktop/BooterBigARM-Publication-20261006-101854/`. The Git bundle was verified. Forty intentional paths were included; 21 local settings, unreferenced Circle image, Unity recovery files, Python caches, and Blender save backups were preserved outside the publication manifest.

## Publication repairs

- The near-detail fixture retained tier-height and slope checks, added a stricter 0.175 m bound reflecting the documented 0.10 m Little Noise and 0.075 m Fine Noise, and samples an unconstrained point that exercises the combined detail.
- The material test compares the complete material sample and packed channels against the canonical service using each representation tier's surface normal. Middle and far corner samples still require exact equality. No runtime source was changed by these test repairs.
- Regenerating the two authored formation assets in an isolated mirror changed only `sourceRevision`. Byte comparison of all 602 generated rock files confirmed unchanged geometry, treatment, references, and GUIDs. Only the two provenance fields were retained.
- Three existing empty Unity folders now have `.gitkeep` placeholders, preserving their existing folder `.meta` GUIDs in a fresh clone. The repository health check ignores these Unity-ignored placeholders. Recovery files, Python caches, and Blender backup copies are ignored by Git without deleting local files.
- Three empty-value lines in the newly included `CanyonTerrainFace.mat.meta` were stripped of trailing whitespace; field values and GUID were preserved.

## Evidence

- Focused terrain, material, world-query, cliff, and streaming tests: **43/43 passed** after the test-contract repairs. The earlier run was 41/43; no failing test was removed or skipped.
- Authored source composition, catalog, chunk ownership, cosmetic identity, ground clutter, and cliff span tests: **8/8 passed**. The broader authored/natural-object test attempt was interrupted without a result; no broad-suite claim is made.
- Current `BrokenWorldBadlandsStudy.blend` opened in Blender 5.2.2 LTS background mode: 882 objects, 869 meshes, 3,032,478 vertices, 5,220,044 polygons, and no missing file-image references. This proves file readability and texture references, not visual acceptance.
- Badwater source audit and isolated Development Player build remain recorded in [the Badwater baseline](../Badwater/gameplay-terrain-baseline-2026-10-06.md). The Player has not been launched or profiled.

## Final publication validation

- `TopDown3DPrototypeValidator.ValidateFromCli`: exit 0 against the isolated mirror with the final source and the two provenance-only asset updates.
- Repository health: all required files, GUID/metadata pairing, ignored generated paths, version, and whitespace checks passed. A dirty-candidate warning disappeared after the logical commits.
- Git LFS object integrity passed. No ordinary Git object reachable from the publication sequence exceeds 100 MB. The remote base remains an ancestor, so publication uses a normal exact-SHA push.
- [Terrain test XML](./2026-10-06/terrain-tests.xml), [asset test XML](./2026-10-06/asset-tests.xml), [commit mapping](./2026-10-06/commit-map.csv), and [history verification](./2026-10-06/history-verification.json) preserve the evidence.
- Validated World Creator snapshot: `6d66c32`; saved Blender snapshot: `ada5530`; repository clone/ignore repairs: `ad75f1c`. These follow the 159 preserved historical commits. Publication receipt itself is a separate documentation commit.
