# 3D Conversion Start-Readiness Inventory

This is the current readiness control sheet for beginning the production conversion of Booter & BigARM to a top-down, isometric-style 2.5D game made with 3D runtime assets. It does not replace [`3D_CONVERSION_AUDIT_AND_CHECKLIST.md`](./3D_CONVERSION_AUDIT_AND_CHECKLIST.md), which remains the canonical full conversion program.

This document answers two narrower questions:

1. What must be true before shared 3D conversion engineering begins at CP-07?
2. What additional work must be true before production 3D models, rigs, animation, and environment families begin at CP-13?

**Current readiness:** Amber — the existing project is a viable conversion base, but CP-07 should not begin until the immediate blockers and CP-06 acceptance items below are closed.

## Readiness Levels

### Level A — Ready to begin conversion engineering

Level A authorizes CP-07 shared runtime seams and later greybox parity work. It does not authorize production asset sourcing, package installation, Build Settings changes, cutover, release, or legacy deletion.

Level A is reached when:

- the protected spike defects found by the second-pass audit are repaired and revalidated;
- physical-gamepad and keyboard/mouse evidence is complete;
- the user accepts or revises the working camera, scale, elevation, input, and readability direction;
- the user explicitly chooses **Proceed** at CP-06;
- the CP-07 task brief defines exact ownership, architecture seams, proof, and stop conditions;
- the current 2D implementation remains intact as the comparison and recovery path.

### Level B — Ready to begin production 3D assets

Level B authorizes controlled work on representative production models, rigs, materials, animations, environment modules, and their Unity import pipeline. It does not authorize broad purchasing or commissioning by implication.

Level B is reached when:

- Level A is complete;
- the art family and game-camera readability target are accepted;
- target platform, minimum hardware, resolution, frame-rate goal, and representative budgets are recorded;
- scale, axes, pivots, materials, textures, rigs, animation, colliders, LODs, prefab structure, source formats, and license tracking have an approved standard;
- the sourcing plan is accepted and each purchase, commission, download, or external account action has its own authority;
- one representative asset family passes import, camera-distance, collision, animation, performance, and provenance checks before asset breadth expands.

### Not part of start readiness

CP-08 through CP-18 remain required to complete and cut over the conversion, but they are not prerequisites to beginning CP-07. Their order and exit proof remain in the master conversion plan.

## Live Inventory

Verified from the repository on 2026-08-12.

| Surface | Ready now | Still missing or provisional | Readiness effect |
| --- | --- | --- | --- |
| Unity and render pipeline | Unity `6000.4.0f1`, URP `17.4.0`, legacy 2D renderer at index 0, conversion 3D renderer at index 1 | Final renderer-default decision remains a cutover matter | Ready for Level A coexistence |
| Scenes | Protected `PrototypeScene` and `SampleScene`; isolated `IsometricConversionLab` outside Build Settings | No production 3D gameplay scene or player-build proof | Lab is sufficient for Level A; production scene comes later |
| Camera | Fixed-yaw orthographic working default and 48-degree mild-perspective comparison exist | User lens/framing acceptance; final zoom limits; rotation decision | Blocks CP-06 acceptance |
| Player traversal | Temporary 3D Rigidbody motor, camera-basis math, collision, sprint, facing, and ramp proof exist | Physical gamepad proof; shared traversal interface; teleport/save consumers; production tuning | Gamepad blocks CP-06; shared seam is CP-07 work |
| BigARM | Temporary large greybox follower and recall position proof exist | Recall warning fix; accepted scale; production behavior split, navigation, collision footprint, persistence | Warning and scale acceptance block Level A; production behavior is later |
| Interaction | Temporary 3D harvest node, hold interaction, marker, inventory yield, depletion, respawn, and pickup exist | Shared interaction seams, tool rules, persistence, final targeting and prompt treatment | Enough for spike acceptance after validation |
| Occlusion | Deliberate blocker and temporary binary hide/reveal experiment exist | User acceptance and final fade, cutaway, silhouette, roof, or placement policy | Working policy needed before content architecture hardens |
| Lighting and post-processing | Directional light, ambient fill, fog, 3D Lit materials, and post-process resources exist | The committed Volume profile contains a null override and must be repaired before its color-adjustment claim is valid | Blocks clean spike closeout |
| Input | Existing Gameplay, System, and UI maps; structural gamepad and keyboard/mouse bindings | Physical gamepad playtest; future centralized action-map ownership; final supported-device matrix | Physical proof blocks CP-06; ownership seam belongs in CP-07 |
| Validation | Preservation validator, editor test assembly, eight focused tests, baseline hashes, repo-health tool | Kinematic warning is not caught; renderer-index relationship has a blind spot; no PlayMode suite yet | Immediate validator/test repair required |
| Save and state | Versioned JSON service, inventory, survival, item, player, BigARM, and world DTO concepts are reusable | XZ meaning, stable IDs, conversion policy, atomic-save hardening, 3D round-trip tests | Planned CP-07 through CP-09 work, not a Level A blocker |
| World generation | Seed, generation version, chunk identity, settings, catalogs, and current generator provide design input | Pure chunk plan, XZ coordinates, 3D realization, streaming, pooling, runtime deltas, navigation seams | Planned CP-10 through CP-12 work |
| Physics and layers | Unity 3D physics is available; temporary spike queries are serialized | No accepted 3D collision-layer matrix; current spike masks include all layers | Define from proven interactions during later architecture work |
| Navigation | Default Unity navigation settings exist | No selected package or proven streamed-world/BigARM solution | Defer selection until topology and BigARM footprint exist |
| Production 3D art | None required for greybox engineering | Zero FBX/OBJ/Blend/DAE/glTF/GLB models, zero animation clips/controllers, no production rigs or 3D prefabs | Blocks Level B, not Level A |
| Greybox art | Nine project-owned temporary materials and primitive scene geometry | Not production art and not an asset-style commitment | Ready for Level A only |
| Existing prefabs and source art | Eleven prefabs, 26 PNG files, and five PSD files remain available as legacy reference | Existing prefabs are sprite-prototype content, not production 3D assets | Preserve until cutover and separate retirement authority |
| Audio and VFX | Current design intent can be preserved | No conversion-specific production audio/VFX pipeline or representative content | Later production-slice work |
| Build and performance | Existing player-build automation builds enabled scenes | Lab intentionally excluded; no conversion player build, target hardware, frame budget, memory budget, or profiling | Level B/CP-16 decisions, not Level A |

## Level A Checklist — Before CP-07 Begins

### A. Protect the workspace and editor

- [ ] **A-01 — Resolve the Unity external-change dialog safely.** Compare the loaded and on-disk `IsometricConversionLab` state, preserve any unsaved work, then deliberately reload or retain it. Do not click through blindly.
- [ ] **A-02 — Repair rebuild data safety.** `RebuildFromMenu` must protect unrelated modified scenes with an explicit save/cancel gate before either `OpenSceneMode.Single` operation.
- [ ] **A-03 — Reopen and resave only the generated lab if repair work requires it.** Confirm no legacy scene or Build Settings change is produced.
- [ ] **A-04 — Re-run repo status and classify every dirty path.** The existing RuleGround and PSD changes remain user-owned and excluded.

### B. Correct the protected spike defects

- [ ] **A-05 — Persist the Volume override correctly.** Add `ColorAdjustments` as a real subasset, repair the existing null profile instead of returning it unchanged, and verify the profile contains no missing component reference.
- [ ] **A-06 — Remove the kinematic Rigidbody warning.** BigARM recall must not assign `linearVelocity` while the body is kinematic.
- [ ] **A-07 — Strengthen renderer validation.** Prove that the renderer index selected by the lab camera points to `IsometricRenderer.asset`, rather than independently assuming camera index 1 and allowing the conversion renderer at any non-default index.
- [ ] **A-08 — Make focused tests fail on unexpected conversion warnings or errors.** A green count must not hide unsupported physics operations.
- [ ] **A-09 — Decide whether all-layer masks remain acceptable for the spike.** If not, add only the minimum temporary layer policy required by observed interactions; do not invent the final collision matrix.

### C. Complete the evidence floor

- [ ] **A-10 — Capture and link the missing preserved 2D visual baseline.** The current baseline records hashes and structure but not a 2D screenshot.
- [ ] **A-11 — Re-run the preservation validator after all repairs.** It must pass with the legacy renderer still default, both protected hierarchy objects inactive, and exactly the two legacy scenes enabled in order.
- [ ] **A-12 — Re-run the focused EditMode suite.** Require all tests to pass with no unexpected conversion warnings or exceptions.
- [ ] **A-13 — Re-check serialized references and `.meta` pairing.** Require no missing scripts, null required references, duplicate GUIDs, or missing task-owned metadata.
- [ ] **A-14 — Repeat the keyboard/mouse interaction pass.** Cover movement, sprint, diagonal speed, facing, collision, ramp traversal, harvesting, pickup, recall, both lenses, and occlusion.
- [ ] **A-15 — Complete a physical-gamepad pass.** Cover the same movement, interaction, sprint, and recall behaviors using a real device.
- [ ] **A-16 — Capture any corrected evidence.** Replace or supplement screenshots and notes if a repair changes visible behavior.
- [ ] **A-17 — Reconcile the evidence report, project status, automation doc, master checklist, and decision log.** Historical audit wording must be labeled as historical instead of contradicting current implementation.

### D. Record user acceptance

- [ ] **A-18 — Choose the working lens.** Recommended default: fixed orthographic; retain mild perspective only if it solves a named visual problem.
- [ ] **A-19 — Accept or revise fixed yaw, pitch, framing, and zoom limits.** Recommended first slice: no player camera rotation.
- [ ] **A-20 — Accept or revise camera-relative movement and movement-facing interaction.** Right-stick/mouse combat aiming remains a later evaluation unless explicitly brought forward.
- [ ] **A-21 — Accept or revise modest ramps as the first elevation scope.** Jumping, climbing, caves, and stacked floors remain deferred.
- [ ] **A-22 — Accept or revise the temporary occlusion direction.** Binary hiding is only evidence; record the intended production family without prematurely implementing it.
- [ ] **A-23 — Accept or revise BigARM's relative scale and camera occupancy.** This is a readability decision, not approval of the temporary cube design.
- [ ] **A-24 — Accept or revise interaction-marker and prompt readability at gameplay distance.** Screen-space UI may remain 2D.
- [ ] **A-25 — Confirm the Level A input scope.** Recommended: gamepad primary and keyboard/mouse first-class; Touch, Joystick, XR, Attack, and Jump remain outside the conversion slice.
- [ ] **A-26 — Record the CP-06 decision.** `Proceed` authorizes CP-07 only; `Revise` keeps work in the protected spike; `Stop` preserves the 2D path.

### E. Prepare the CP-07 work contract

- [ ] **A-27 — Name exact owned files and preserved paths.** Include the user-owned dirty-art exclusion.
- [ ] **A-28 — Define the first shared traversal contract.** Position, velocity, teleport, grounded/movement state, and sprint state must be dimension-neutral.
- [ ] **A-29 — Select a Unity-serializable reference seam.** Use a concrete facade, abstract `MonoBehaviour`, or explicit component resolution that survives scene reload; do not serialize raw interfaces.
- [ ] **A-30 — Define XZ/Y coordinate semantics.** Horizontal world coordinates are X/Z and elevation is Y; do not silently reinterpret legacy chunk X/Y data.
- [ ] **A-31 — Inventory concrete `PlayerMotor2D` consumers.** Classify survival, save/load, BigARM commands, camera/debug, and other consumers by migration order.
- [ ] **A-32 — Define temporary 2D/3D coexistence.** State whether implementations remain in one assembly or move behind explicit legacy/shared boundaries.
- [ ] **A-33 — Define input ownership for the seam.** Avoid multiple components independently disabling actions or maps they do not exclusively own.
- [ ] **A-34 — Define proof before implementation.** Require Inspector serialization, legacy-scene opening, conversion-scene opening, focused tests, validator checks, and exact diff review.
- [ ] **A-35 — Define the stop condition.** CP-07 stops after shared seams work in both paths; it does not continue into loop parity, packages, production assets, Build Settings, cutover, or cleanup.

## Level B Checklist — Before Production 3D Assets Begin

These items may be planned alongside engineering, but broad production asset work must not begin until their dependencies are known.

### F. Creative and platform brief

- [ ] **B-01 — Name the art family and provide representative references.** Record what is being matched and what is explicitly rejected.
- [ ] **B-02 — Approve Booter and BigARM silhouette, scale, proportions, and top-facing detail goals.** Judge at gameplay-camera distance.
- [ ] **B-03 — Approve the environment language.** Canyon floors, walls, ramps, landmarks, ruins, resources, and traversal boundaries must read from the accepted camera.
- [ ] **B-04 — Name target platform(s), minimum hardware, target resolution, and frame-rate goal.** Do not derive budgets from the development Mac alone.
- [ ] **B-05 — Choose the sourcing mix.** Internal creation, commission, purchase, marketplace, AI-assisted source work, or a documented combination.
- [ ] **B-06 — Establish purchase, account, license, and provenance authority.** Every external action remains separately approved.

### G. 3D asset technical standard

- [ ] **B-07 — Establish Unity scale and measurement rules.** Include Booter height, BigARM footprint, door/path widths, step height, ramp limits, and world-module dimensions.
- [ ] **B-08 — Establish axes, forward direction, pivots, origins, and prefab-root conventions.**
- [ ] **B-09 — Establish source and interchange formats.** Prefer a controlled interchange such as FBX unless another format is proven; do not depend on direct `.blend` import accidentally.
- [ ] **B-10 — Establish geometry budgets by asset class and screen coverage.**
- [ ] **B-11 — Establish texture resolution, channel packing, color space, compression, and material-family rules.**
- [ ] **B-12 — Establish URP shader, transparency, shadow, instancing, and SRP-batcher expectations.**
- [ ] **B-13 — Establish collider policy.** Primitive, compound, simplified mesh, trigger, and special-case rules must be explicit.
- [ ] **B-14 — Establish rig, humanoid/generic, root-motion, bone naming, animation import, and attachment-point rules.**
- [ ] **B-15 — Establish LOD, culling, pooling, and repeated-prop expectations.**
- [ ] **B-16 — Establish prefab nesting, variant, source-art, optimized-runtime, naming, and folder rules.**
- [ ] **B-17 — Establish license/provenance records and an import acceptance checklist.**

### H. Representative production proof

- [ ] **B-18 — Choose one representative asset family instead of buying or building broad content.** Recommended family: Booter, one resource node, one ground/canyon module, and one prop using the same scale/material rules.
- [ ] **B-19 — Import it through the approved pipeline.** Require deterministic import settings and project-owned prefabs/materials.
- [ ] **B-20 — Test it from both accepted projection modes at gameplay distance.** Judge silhouette, value grouping, top-facing information, ground contact, and occlusion.
- [ ] **B-21 — Test collision, navigation footprint, animation, attachments, and interaction markers.**
- [ ] **B-22 — Profile the representative family on the target floor.** Record frame time, draw calls, memory, shadows, material count, and any streaming/collider cost relevant to the asset.
- [ ] **B-23 — Record provenance and license evidence.** Reject assets whose usage rights or source cannot be documented.
- [ ] **B-24 — Accept, revise, or reject the family before expanding production breadth.**

## Who Provides What

### User decisions or materials

The user does **not** need to provide production 3D assets before Level A greybox engineering begins. Before Level B, the user must provide or approve:

- visual references and the intended art family;
- final creative approval for Booter, BigARM, world, creatures, and major landmarks;
- target-platform and quality priorities;
- the sourcing approach and any spending, marketplace, commissioning, or account actions;
- licenses or provenance for user-supplied assets;
- acceptance of the representative production family before breadth expands.

### Gottspan/Codex ownership after authorization

Gottspan can own:

- conversion sequencing, task briefs, file ownership, and integration order;
- shared runtime architecture, adapters, greybox implementation, and focused tests;
- Unity import rules, prefab/material setup, validation tooling, and documentation;
- evidence collection, risk tracking, and gate reconciliation;
- controlled integration of user-approved assets.

This ownership does not include unapproved purchases, external accounts, destructive cleanup, release, or final creative acceptance.

## Explicitly Deferred Until Later Gates

The following are not start-readiness tasks and must not be pulled forward by momentum:

- complete survival-loop parity and save migration;
- deterministic 3D world generation and streaming;
- the BigARM navigation package decision and production AI split;
- broad character, environment, creature, VFX, audio, or animation production;
- adding the conversion scene to Build Settings;
- changing the default renderer;
- shipping-platform builds and final optimization;
- converting canonical world text from 2D language;
- deleting, moving, or retiring legacy 2D scenes, code, prefabs, sprites, tilemaps, packages, or documentation.

## Go/No-Go Rule

CP-07 may begin only when all Level A items A-01 through A-35 that apply to the accepted spike are checked, the user has recorded **Proceed** at A-26, and the resulting CP-07 task brief is narrow enough to stop before finite-loop parity.

Production 3D asset work may begin only when Level B has an accepted brief and representative-family scope. Full conversion completion still requires the later checkpoints in the master plan.
