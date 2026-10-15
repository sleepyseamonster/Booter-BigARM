# Modular camera and environment presentation

Execution contract: the October 14, 2026 camera/environment plan in this chat, including its seven audit corrections. User implementation authorization followed that audit. Gottspan integrates, Babineaux owns background Unity proof, Gear Ball commits only task-owned verified changes, and the author accepts appearance and gameplay feel.

## Boundary

The first migration targets `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. Existing production and template scenes keep their serialized sun/profile contracts. Shared runtime and renderer changes still require focused legacy regressions. This does not promote Camp into production, alter terrain, enable global dust, change controls or optics tuning, add persistence, change packages, or authorize publication.

Production terrain remains fixed and always loaded. Presentation introduces no generated-object IDs, chunk residency, regenerated content, or saved runtime deltas. The twilight clock keeps the previous scaled-time cycle and initial authored phase; save integration remains unchanged. Existing deterministic dust rules and terrain section/sector identities remain intact.

## Ownership

| Owner | State |
| --- | --- |
| `TopDown3DTwilightClock` | Phase, duration, run/pause, authored sun path |
| `TopDown3DSunLight` and its profile | Directional light, direct color/intensity, shadows |
| `TopDown3DAmbientLighting` and its profile | Ambient fill and optional reflection |
| `TopDown3DSkyPresentation` and its profile | Camera-local cloned sky, exposure and apparent sun diameter |
| `TopDown3DEnvironmentRig` | Explicit references, unique active-scene authority, acquisition/restoration of owned properties |
| `TopDown3DSceneRenderingOverride` | Independent camera optics and focus binding |
| `TopDown3DCameraOpticsProfile` | Explicit world-blur selection and custom lens effect settings |
| `TopDown3DCampOpticsProfile` | Native DOF, focus, bloom, dirt, exposure and tonemapping data; migration creates a private copy |
| `TopDown3DCameraOpticsFeature` | Modular camera stack setup and early Gaussian scheduling |
| Existing camera rig/range and dust components | Their existing movement, draw-distance and atmosphere responsibilities |
| `CampLookEditorPreview` | Editor-only preview registration and borrowed Scene-view restoration |

`PerpetualTwilightSun` retains its GUID/type and public API for gameplay callers. An explicit environment reference delegates migrated scenes to the modules; the old serialized fields are inactive compatibility data there. Unconverted scenes continue through their existing implementation. There is one advancing clock per migrated environment. The coordinator has no second phase or visual tuning store.

Only the active scene's unique environment may write global lighting. Duplicate rigs or sun facades reject acquisition. Module disable restores its owned values, rather than authoring a new lighting preset. Disabling the clock freezes phase. Replacing modules releases their prior resources first. Teardown compares current values with the last owned write and preserves newer external writes. Inactive-scene global restoration is deferred until that scene becomes active again. Sky resources stay camera-local and never replace shared source material state.

## Optics compatibility

| World blur | DOF | Flare | Streak |
| --- | --- | --- | --- |
| Off | Off | Available when independently enabled | Available when independently enabled |
| Tilt Shift | Off | Available when independently enabled | Available when independently enabled |
| Depth of Field / early Gaussian | Gaussian before lens artifacts | Available if early Gaussian is supported | Suppressed |
| Depth of Field / native Gaussian or Bokeh | Native post-process DOF | Suppressed | Suppressed |

The optics master suppresses all modular optical effects. Unsupported early Gaussian uses the existing native-DOF fallback and suppresses flare. Invalid, disabled, or ambiguous bindings never fall back to shared artistic effects. Utility cameras retain their existing behavior.

Modular tilt shift owns both legacy sampling parameters and reference-resolution parameters. Changing modes preserves dormant tuning. Shared renderer settings remain the authority for unconverted cameras only. Apparent sun diameter comes from the sky profile for modular flare calculations; the retained native field is legacy compatibility data.

## Version 1 migration

`CampPresentationMigration.DryRun` checks the compiled open Camp scene, unique legacy camera/sun/profile, and destination state. `InstallCurrentScene` creates module profiles and scene objects with Undo, preserving world poses, existing object names, source assets, and GUIDs. It does not save the scene. A complete repeat is a no-op; partial or invalid installation is rejected for inspection.

The migration maps source direct-light colors and intensities to SunLighting, ambient colors/reflection to AmbientLighting, sky material/exposure/diameter to Sky, native optics to a private NativeOptics copy, and effect selection/tilt/flare parameters to CameraOptics. Legacy renderer tilt parameters are copied separately from dormant reference-mode values. The original shared profile assets are retained unchanged by migration.

Migration preserves effective gameplay blur: selected native DOF takes precedence over a simultaneously checked legacy tilt flag. Preserving the suppressed tilt parameters does not activate them.

The grouping roots are Systems, Actors, Camera, Environment, World, UI and Development. Existing terrain subtrees remain intact. Camera and sun move out of Camp Look; Camp Look becomes an editor-preview overview under Development. Existing root-dependent production/template builders retain their old scene contracts; they are not run to rebuild the migrated Camp scene.

`RegisterRendererFeature` adds one optics entry point before lens effects, with a serialized renderer feature-map entry. For unconverted cameras this feature is a no-op, and Sun Bloom retains its old setup path. For modular cameras Sun Bloom no longer sets up optics. Bloom can therefore be disabled independently.

## Verification and recovery

Capture unsaved author state as a separate scene copy without clearing its dirty flag. Validate compilation, focused clock/ownership/optics/legacy tests and matched phase captures in a marked isolated graphics-enabled candidate. Do not run test fixtures or scene-opening validators against the author Editor. Preserve background-only operation.

Before installation, refresh the author snapshot and source hashes. Apply only migration-owned scene and renderer changes. Never replace the author scene with an isolated candidate scene. Retain recovery snapshots and any partial migration assets; do not delete source work to retry a gate.

Focused tests establish contracts and lifecycle behavior. Static captures establish only the captured views/phases, not gameplay acceptance, target-hardware performance or a Player build. Hands-on smoke testing remains author-owned. Optimization is a subsequent measured pass, not part of the structural cutover.

Stop a dependent batch on ownership conflict, missing migration source, failed reference/compile/equivalence gates, partial installation, or out-of-scope production/publication work. Completion requires a verified Camp migration, independent controls and restoration, unchanged unconverted-scene contracts, task-owned commit, and an explicit visual acceptance handoff. No push is authorized.

## Implementation evidence

The [October 14 implementation record](../Evidence/Rendering/PresentationModules20261014/README.md) retains 119 focused checks, three exact matched-phase captures, saved-scene reload proof and the bounded scene delta. Live Editor dialog handling and Git closeout are reported separately from isolated proof.
