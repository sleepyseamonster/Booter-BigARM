# Sustained crouch controller checkpoint — 2026-10-14

This batch implements the seven-role crouch controller and its integration seams. Real source contact, speed, transition/blend and radial fit qualification remain pending. The private profile still has contactsQualified=false; the player input therefore reports the pending qualification and cannot start a physical crouch. The original 71 motion bindings remain at import readiness.

## Delivered behavior

- The existing input router sends crouch to an owned controller. Standing, entry, sustained crouch and exit use the existing motor and Playables graph, with one Animator output.
- Entry and exit run through the shared action coordinator. Capsule height follows the measured profile while preserving its foot plane; target height correction stays in the visual pose owner.
- Sustained crouch uses an indefinite fixed-step clock, a five-input idle/directional mixer, fixed entry facing and the existing collision/movement writer. Directional blending normalizes weights without allocating an array per frame. Contact crossings sample their actual semantic phase and pause without consuming markers.
- A standing clearance query gates exit. Cancellation during entry finishes the transition before attempting exit. Ceiling obstruction retains the smaller capsule and owned pose.
- Reset first invalidates action clocks. A blocked actor return preserves the safe crouch pose and suppresses further posture commands. Successful actor restoration then releases the crouch branch before input restoration.
- Legacy gathering, canister deployment/pickup and camp movement require standing while the laboratory posture owner is present. Palette previews ask the player to stand first. Inventory access remains independent of sustained posture.

## Authored fixture and world contracts

AP03's roof underside is now 1.575m, leaving 5.5cm above the provisional 1.52m crouch capsule while blocking the 1.82m standing shape. Only five transform fields changed in the prefab; its GUID, object file IDs and metadata are preserved. The factory and bounded candidate repair method reproduce the dimensions.

This is an authored laboratory on nine fixed, always-loaded native terrains. Crouch changes no deterministic world identity, chunk or terrain loading, placement rules, generated object IDs or authored terrain constraints. The existing session and AP station IDs own reset. Posture/clock state is session-local; the laboratory save guard excludes it from persistent runtime deltas. Legger's position and state are outside this controller and reset scope.

## Proof and limits

The final isolated Edit Mode run passes 57 tests, with zero failures or skips. It covers clock crossing/pause/direction limits; controller ownership, fixed-facing movement, safe cancellation, roof obstruction and blocked/successful reset; existing motor/presentation/player integration; canonical private crouch payload/derivative gates; and station ownership/geometry/reset contracts. The controller fixtures use explicitly synthetic contact maps on cloned private profiles to isolate the runtime contract. Those maps do not qualify the real clips.

The final run removes manually initialized fixture graphs during Edit Mode teardown. See tests.xml and receipt.json for the exact candidate source hashes and scene/fixture identities. No Play Mode, gameplay smoke test, build, package change, production scene/terrain edit or external publication occurred.

The author editor's background main-thread query still times out, so its dirty state and current source refresh cannot be confirmed. The saved author CampLoopTesting scene retains SHA-256 91df493660fcf6e4aa1ce735d9a8927189813da670ea2d0f6fdac829440b8ae4. All twelve stations remain installed only in the validated candidate; the author-scene installer continues to require a clean scene and its sealed baseline.

Next, qualify real directional plant windows, speeds, capsule radial fit and standing/entry/idle/exit blends before enabling this family. Slide, airborne/landing, prop interactions, cover, ladder, rest/mount, combat/recovery and motion-gap closure remain part of the active implementation goal.
