# Batch 9 persistence and saved-place evidence

Date: 2026-08-24

## Save authority and compatibility

- The live game-state schema is now version 3 and carries the complete `WorldPersistenceManifest`: world seed, independent version domains, and coordinate-model identity/version.
- Booter, BigARM, and marked places persist precision-safe absolute positions paired with canonical coordinate addresses. Unity-local `Vector3` data is no longer the geographic authority.
- Schema v1/v2 game saves are recognized and rejected before runtime mutation because they lack enough absolute and manifest identity to migrate safely.
- A topology, version-domain, world-seed, coordinate-model, or canonical-address mismatch rejects the load instead of silently resolving a marked place to different geography.
- Explicit presence flags protect optional BigARM, cargo, and resource payloads from Unity JSON null-instantiation ambiguity.
- All incoming inventory, cargo, resource, saved-place, and generalized-delta payloads are validated before the local-origin frame or live state changes.

## Saved places and mutable world state

- A saved place carries a stable marker ID, player-supplied name, full manifest, precision-safe absolute position/address, bounded feature references, and an optional opaque thematic-coordinate payload.
- The thematic payload is only a replaceable future authority seam. Batch 9 defines no globe bounds, projection, wrapping, notation, named regions, or lore.
- Version 2 of `SavedPlaceRecordCodec` is deterministic and bounded. It can explicitly migrate the earlier version 1 record shape without inventing thematic coordinates; the live game save still rejects that record if its missing absolute identity cannot be proven against its address.
- Stable generalized deltas are bounded, sorted, revisioned, namespaced by version domain, and refer to feature IDs rather than GameObjects or build order.
- The existing resource snapshot is incorporated transactionally, including zero-use/depleted nodes and generation-version rejection.
- Saved places and generalized deltas are count-bounded; individual names, roles, payloads, and thematic data are length-bounded. No chunk meshes, vertices, triangles, or whole generated geometry enter the save.

## Verification receipts

- Complete World Creator namespace: 79/79 passed (`/tmp/booter-batch9-worldcreator-final.xml`).
- Focused live game-state persistence: 2/2 passed (`/tmp/booter-batch9-game-persistence.xml`).
  - cross-chunk Booter position save/reload
  - depleted-resource reconstruction
  - named marked-place reconstruction with stable feature reference and opaque thematic seam
  - generalized-delta round trip
  - geometry-free serialized payload
  - wrong-schema rejection before runtime mutation
- Saved-place/manifest suite: 6/6 passed (`/tmp/booter-batch9-persistence.xml`).
  - deterministic version 2 codec round trip
  - explicit version 1 record migration
  - topology mismatch rejection
  - coordinate-model substitution rejection
  - far absolute address and saved-place reconstruction across local-origin frames
  - corruption/trailing-data rejection
- Legacy prototype save-compatibility regression: 1/1 passed (`/tmp/booter-batch9-legacy-save.xml`).
- Production authority validator: passed (`/tmp/booter-batch9-validator.log`).
- Exact Development StandaloneOSX build: succeeded (`/tmp/BooterBigArm-Batch9.app`, `/tmp/booter-batch9-development-build.log`).

## Remaining gates

- No save silently resolves to changed topology; incompatible data fails closed. Deliberate future migration rules require an explicitly authored migration once the affected coordinate/topology change is known.
- Batch 10 still owns the full proof transects, duplicate-authority removal audit, user visual acceptance, and the controlled target-Windows profile. This Batch 9 build is compilation/runtime packaging proof, not final visual or target-hardware acceptance.
