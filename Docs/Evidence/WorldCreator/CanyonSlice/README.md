# Generated canyon face implementation receipt

Date: 2026-09-28. This receipt covers the first terrain-driven canyon face pass. It is a technical checkpoint, not visual or traversal acceptance.

## Fixture and preserved source

- Unity: `6000.4.0f1`, isolated project mirror at `/tmp/booter-canyon-zDplUi`. The live project had `Temp/UnityLockfile` and was not opened by batchmode.
- World seed: `24681357`; source fingerprint: `96ef0728256ce0e5c5524173e588aa7f`; versions: topology 13, coordinate 1, landform 2, material 1, decoration 2, resource 1, site 2.
- Proof-only mirror profile: `includeCanyonsInInitialPlayableArea: 1`. The production profile remains user-owned and unchanged by this task. Its first playable area still excludes deep canyons.
- Canonical canyon segment: `d1c85c86013ce2b97008b5c689c0d111`, midpoint `(416.74, 342.39)` in absolute horizontal coordinates. The query reports canyon shelf/wall candidates on both sides with the same segment parent, including drops near 41 m and 17 m.
- Sampled near chunks: `(23,17)` and `(22,20)`, each 18 m wide. This fixture uses the normal World Creator query, cliff section study, and chunk decoration code.

## Implemented path

`WorldCliffFacePlanner` links reciprocal absolute-grid candidates only when canyon parent, strata, direction, facing, and rim/toe elevations agree. The existing chunk decorator owns only spans whose first section lies in that chunk. A closed 12-point ring mesh gives the face two stepped strata ledges and a cap; a lower-detail closed mesh supplies collision. The parent and strata key set a stable layer phase. Sparse buttresses and toe fragments load the existing baked `CliffStone_A-E_LOD0-2` Workbench meshes, use LOD groups and simplified box collision, and remain children of the streamed chunk. Spawn, formation, site, approach, route, slope, and chunk-footprint screens limit placement. Static geology writes no save delta.

## Measured proof

- Focused normal-profile EditMode run: 5 of 5 passed (`/tmp/booter-canyon-face-tests2.xml`). It checks source LODs, face closure/endpoints and seam data, stable reload/rebase, border ownership, and parent/strata separation.
- Canyon proof-only EditMode run: 1 of 1 passed (`/tmp/booter-canyon-fixture-repeat.xml`). The two chunks produced 30 physical face spans and 10 Workbench stone colliders. Reversed chunk build order and a local-origin rebase reproduced the same generated object names and counts. The three sequential decoration calls took 424, 351, and 359 ms across both chunks in this editor fixture.
- An exact pre-change decorator comparison on the same mirror fixture produced 30 face spans and no Workbench stones, with 393, 341, and 341 ms across the same three calls (`/tmp/booter-canyon-fixture-baseline.xml`). That proof-only test intentionally fails its new-stones assertion against the old decorator; the timing is a diagnostic, not a Player performance verdict.
- Source Workbench prefab and baked mesh assets were not changed. The production scene, profile, query, representation, and pre-existing dirty files were not staged by this task.

## Limits and next gates

The current pass does not show the canyon through fixed production-camera views, prove rendered terrain-triangle contact, profile a full-content Development Player, or prove two streaming-ring transitions. Middle/far terrain comes from the canonical query, but the new ledge and Workbench detail exists only in near decoration; distance continuity and arrival pop remain unverified. No gameplay smoke test or hands-on traversal was run. The user must review the visual result and traverse it before this becomes an accepted canyon treatment. Do not enable the canyon in the production first area or change topology/save versions on the strength of this receipt.
