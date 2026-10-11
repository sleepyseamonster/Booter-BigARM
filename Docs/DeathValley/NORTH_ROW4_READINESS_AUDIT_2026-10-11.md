# Fourth northern row readiness audit — October 11, 2026

The third northern row is complete and its saved checkpoint still matches the [continuation handoff](THIRD_NORTHERN_ROW_CONTINUATION_HANDOFF_2026-10-11.md). The author requested another six sections, then directed this chat to audit first. This document completes that audit; no new terrain acquisition, authoring or integration was performed.

## Current checkpoint

- Branch: `main`, tracking `origin/main`; audited HEAD `00d9f4077`. The checkout was 57 commits ahead at inspection, with no staged changes.
- Greater Wasteland: 7680 native terrains, 30 sections, 480 sectors, complete 96 × 80 footprint, bounds EPSG:26911 `[499920,4006200,524496,4026680]`.
- Scene SHA256: `19c3633ffd884f11b72cd9f6daae13526315ad252f93da49b5191d99ab87c566`.
- Final native manifest SHA256: `9b0c383e636a3a461fa9d607db5f2f84b64da4485edfa2241197145023c06304`.
- Package manifest, lock and Editor-version bytes match the handoff. Unity remains 6000.4.0f1 and Pipeline remains 0.8.0-exp.1.
- Preserve dirty `Greybox_BigARM.mat` and untracked `Art/Environment/Rocks/Fused` content and metadata. A separate rock Blender session is open; it is outside this audit's ownership.

The fresh doctor checked 15374 records with zero issues. A read-only rerun of the sealed completion script passed every existing assertion, using a fresh doctor result and omitting its sealed-receipt writer. This verified the six committed acceptance lineages, source manifests, 18 retained Blender source hashes, authoring proofs and view hashes, preservation receipts, test XML and map hashes, camp reconciliation, original scene document preservation, source exclusions and package pins. The original 46 retired sources remain absent.

A separate fresh check verified all 7680 accepted TerrainData hashes, paired metadata hashes and native snapshot hashes, and compared all 15184 normalized snapshot joins exactly. It retained only border arrays during comparison. The previous isolated saved production scene also matches root exactly.

These checks reverify current bytes against accepted evidence. They do not claim a new Blender reopen, visual inspection, Unity collider run, test execution, gameplay smoke test or Player performance result. The accepted native report remains bound to the current scene and manifest: 15184 joins and 192000 collider samples, maximum new-grid readback error 0.0274658203125 m, maximum sampled collider error 0.00006103515625 m.

Fresh machine-readable results and exact commands are retained in the paired [audit evidence](../Evidence/DeathValley/NORTH_ROW4_READINESS_AUDIT_2026-10-11.json). Detailed local outputs and the audit-only helpers remain under ignored `Logs/NorthRow4Readiness20261011/`.

## Next extent and source coverage

Continue west to east with six 4096 m square sections across `[499920,4026680,524496,4030776]`. Each adds 256 native tiles and sixteen sectors. At completion the world would contain 9216 tiles, 36 sections and 576 sectors on a full 96 × 96 grid.

| Step | West easting | Total tiles | Exact joins | Exterior slots | Collider samples |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 499920 | 7936 | 15680 | 384 | 198400 |
| 2 | 504016 | 8192 | 16192 | 384 | 204800 |
| 3 | 508112 | 8448 | 16704 | 384 | 211200 |
| 4 | 512208 | 8704 | 17216 | 384 | 217600 |
| 5 | 516304 | 8960 | 17728 | 384 | 224000 |
| 6 | 520400 | 9216 | 18240 | 384 | 230400 |

The strip crosses N4030000. Half-open source ownership therefore requires eight products: `x49` through `x52`, each with both `y403` and `y404`. A fresh read-only [USGS National Map product query](https://tnmaccess.nationalmap.gov/api/v1/products?bbox=-117.00089241096936%2C36.384940579770344%2C-116.72674516513595%2C36.42217963328417&datasets=Digital+Elevation+Model+%28DEM%29+1+meter&outputFormat=JSON&max=100) returned exactly eight matching `CA_FEMAR9Southeast_D24` products, with no missing or ambiguous domain. Only catalog JSON was fetched. Raster dimensions, spacing, datum, NoData, overlap and elevation range still require bounded measured acquisition and source audit. Catalog availability alone does not establish that the strip fits the unchanged −100 m to 1700 m encoding.

The broad colour palette still ends at N4024200. Reuse its immutable reference and explicitly record any northern edge extension; do not invent geometry or detailed imagery. Audit the farther extension visually before sealing authoring.

## Operational readiness and gaps

The installed tools are sufficient: pinned Unity, existing Blender 5.2.2, and the Python environment. Numpy, Pillow, rasterio and pyproj imports passed with the repository's pinned versions; `pip check` passed. No plugin, package upgrade or purchased resource is needed.

The root Unity process is alive (observed PID 13084), but the targeted CLI status still returns `STATUS_NO_INSTANCES`. Open scene identity, unsaved state, current main-thread console truth and loaded-object fingerprints could not be freshly read. Do not infer the process launch's `-openfile` argument is the currently loaded scene. Preserve this Editor and the user's foreground application. Hidden isolated work can prepare and validate a candidate, but root transfer must wait for fresh production-unloaded, asset-dirty, compilation/Play Mode and loaded-state guards. Do not copy historical root guard results forward.

The final third-row section's `AcceptedModel01` directory is absent. Native references and its accepted manifest remain available. Derive that missing bank from actual normalized native snapshots, respecting their explicit orientation and hashes, before preparing the first new section. South anchors are the six accepted third-row sections; later west anchors come from the newly accepted fourth row.

The existing implementation is bounded to row three. Python contracts/capture/preparation/authoring/export/metadata and coverage adapters, C# source/builder/validator dispatch, canonical setup terrain detection, and map count/occupancy validation all need a distinct `north_row4` family. Current coverage allows counts only through 7680; map special handling stops there. Preserve sealed row-three bounds, owners and receipts. Never add a seventh row-three step or bypass predecessor checks.

D: had 24765120512 bytes free (24.77 GB, about 23.06 GiB) at initial inspection; C: had about 36.07 GB free. The previous validation project measured 23361742315 bytes in its sealed capacity receipt. Duplicating that whole project would leave only about 1.40 GB before any new sources, below the 3 GiB reserve. Thus the old warm-copy workflow is not presently feasible on D:.

Fresh component measurement found 14519550919 bytes of Assets and 8782281468 bytes of Library in the existing isolation. Even that current combined footprint plus the 3 GiB reserve exceeds free D: space before new row sources or runtime assets. A lean copy with a fresh Library reduces initial copying but does not establish sustained capacity. Prefer provisioning additional capacity; alternatively budget a fresh physical validation owner on C:, which had 38014275584 bytes free at the later measurement. Account for root and isolated native copies, growing Library, acquisition windows, prepared banks, Blender sources, Git objects and reserve before selecting C:. Preserve old validation projects, C: rollback, GIS and Blender sources. Do not overwrite the old validation owner or delete retained sources to create room. Prior row storage is a planning analogue, not a fixed fourth-row size guarantee.

## Recommended next work

1. Write the fourth-row execution contract and measured storage budget. Fix world origin, height encoding, six bounds, step counts, source domains, immutable predecessor and failure gates. Resolve the physical isolation location before raster acquisition or import.
2. Add a small repository-owned orchestration runner with explicit family, step, date, output version and isolation path. Extract established guard behavior without weakening it: immutable predecessor capture, freshness refusal, source hashing, background launches, terminal exit propagation, and exact transfer/staging manifests. This replaces repeated edits to ignored hardcoded helpers and makes resumption easier. Keep geometry in a separate fourth-row contract; avoid a broad rewrite of sealed families.
3. Implement the fourth-row adapters and focused positive/negative contracts, including both northing ownership domains, first-step 496 added joins, later 512 joins, sparse occupancy, snapshot orientation, precision budget and changed-predecessor rejection. Validate Editor compilation and focused tests in the selected isolation.
4. Acquire and audit only the approved strip into fresh local-only owners. Check elevation limits before authoring. Capture the committed 7680-tile predecessor and derive the missing accepted model bank.
5. Complete one section at a time through the existing pilot, section, combined review, reopened Blender proof, sealed export, candidate check, independent saved native/collider validation, canonical setup, metadata/coverage/map inspection and local commit gates. Capture the next predecessor only after acceptance and commit.
6. Before any root transfer, obtain fresh safe bridge guards; if unavailable, retain the validated candidate and report the specific transfer limitation. Never restart, save, switch or focus the user's Editor as a shortcut.

Identity remains `epsg26911/e{east}/n{north}/size256` with the existing Unity origin. Terrain remains fixed and always loaded; terrain generation and streaming/unload/reload are not applicable because this work extends native authored assets. Existing gameplay identity and persisted deltas must remain untouched; this slice introduces no procedural-object or save-format change. Authored constraints are the measured sources, accepted quantized borders, precision budget and immutable predecessor assets.

The audit's conclusion is **checkpoint intact; ready to plan and build fourth-row tooling, with storage and live transfer readiness as gates before execution**. New tools are bounded workflow improvements; the existing installed software is adequate.
