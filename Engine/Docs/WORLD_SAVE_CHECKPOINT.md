# World Persistence Checkpoint — Paused

The user requested a good stopping point on 2026-09-10. This is a verified P21 library checkpoint, not completed application integration. Stop here until the user resumes work.

## Implemented

`WorldSave` stores the terrain/rock recipes, authored constraints, player snapshot, working-origin region and removed-rock deltas as one immutable generation. Each member is bounded by the existing document limit and checked against its recorded length/CRC. A manifest is written last, and only a final directory rename publishes the generation. Loading selects the newest complete valid generation; incomplete staging directories are ignored, and a damaged committed member can fall back to the previous complete generation without mixing player/world data.

An OS-backed writer lease serializes save publication and releases when its handle closes. The expected-generation argument rejects stale sessions. New saves normally retain the latest two valid generations; corrupt or interrupted generations are preserved for inspection. A profile is bounded to 64 generation entries rather than silently growing without limit. Configuration mismatches, unsupported document/content versions and implicit conversion from a legacy player-only profile are rejected. This is application-level partial-write recovery, not verified full power-loss durability or a schema-migration implementation.

`WorldDeltas` records removed rock cell members under their signed region address, with 256 changed regions and up to 256 rock slots per region. A live removal invalidates the generation ticket and queues fresh content with the tombstone applied. The previously valid representation remains while the replacement prepares. The rendering adapter prepares a new mesh and replaces the existing Jolt shape before adopting it, preserving the collider token. Failed preparation retains the old representation and exposes an error. Regeneration/restart filters both visible placements and aggregate rock collision.

The streaming adapter exposes nearest-rock removal and delta retrieval for the next application integration step. These are library entry points; no new remove/save UI or world-profile CLI has been connected yet. The apps still use their earlier save paths. The dedicated rock-generator package remains unchanged.

## Verification

[The focused native check](../Evidence/P21-checkpoint-native/result.json) passed for whole-generation roundtrip, player/delta consistency after an injected interrupted save, recovery from a corrupt committed member, overlapping writer rejection, stale-session rejection, configuration/future-content incompatibility, preservation when no valid generation exists, and refusal to reinterpret a legacy player-only profile. It also exercised actual Jolt rock removal with a stable body token and verified absence after region unload/reload and a saved restart.

Both app targets and the new test executable build. This checkpoint adds no graphical verification or gameplay smoke test. Windows lease behavior, abrupt-process/power-loss cases, origin rebasing and app-level save/restart remain unverified. The native test injects an exception between member writes; it does not claim an actual machine crash.

## Resume point

P21 remains `in_progress`. On explicit resumption, wire `loadWorldSave`/`saveWorld` and the delta-aware streaming constructor into both apps, preserving the existing single-rock workbench and player-only calibration profiles. Capture player/world state together at a main-thread boundary, keep the expected generation in the session, and expose a small workbench action for removing a nearby rock and saving the world. Finish configuration/version handling and one focused app-level restart check. P22 then handles the working origin and broader integration; do not start another subsystem before closing this save path.

The full engine goal remains incomplete. This checkpoint does not change its scope or mark it achieved.
