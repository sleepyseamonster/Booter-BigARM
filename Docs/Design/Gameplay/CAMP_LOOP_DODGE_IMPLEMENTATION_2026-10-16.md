# Camp Loop dodge roll — October 16, 2026

Status: implemented, qualified and installed in the main CampLoopTesting slice. Ready for author hands-on testing; feel acceptance remains pending.

The author assigned Gamepad East to dodge and authorized source acquisition and implementation decisions. Target the existing main CampLoopTesting native-ground slice; retain fast run at 4.9 m/s, full sprint at 6.15 m/s and the 1.6× jump gravity tuning.

## Audit and selected behavior

East currently conflicts with canister pickup and diagnostic action cancel. In the slice's runtime input clone, East becomes dodge, View/Back becomes canister pickup, Backspace remains diagnostic cancel and F9 retains the animation palette. Menu Back/Cancel and the shipped production input asset remain unchanged. Preserve binding IDs, migrate only obsolete lab defaults in memory and retain existing preference files.

The existing DodgeRight is a torso lean with negligible root travel; using it for a travelling dodge would slide planted feet. The existing backward dodge is a long multi-part motion. Fresh Mixamo exports supply Standing Dodge Forward (1.0 s) and Stand To Roll (2.367 s), both complete Humanoid takes. The latter contains an unarmed entry, complete roll and standing recovery and is selected for the dodge. The forward step remains an audit source.

East rolls toward the left stick; neutral input rolls forward. Capture direction once, preserve the existing sole Rigidbody movement owner, sweep against obstacles, respect walkable slopes and gravity, and retain the upright gameplay capsule. This roll does not grant crawl clearance. No invulnerability or stamina rules are added to the current combat/survival design. Hold input must not repeat dodges or leak a request through menus, reset or device loss.

Use a complete grounded derivative with a bounded displacement curve, prompt entry blend, locomotion run-out and short recovery/cooldown. Preserve held LT facing/look-ahead through the movement lease. Reject airborne, mounted, crouched, dead and busy requests. Do not teleport or disable collision. Ordinary gravity owns a ledge departure; normal airborne presentation resumes after the dodge releases.

## Implementation and proof order

1. Seal the accepted settings, source/test backups, scene identity and unrelated dirty work.
2. Add an optional paired dodge calibration and controller using the existing action coordinator and one animation output. Add a dedicated movement policy so action acquisition/release does not erase momentum or held LT facing.
3. Resolve input conflicts in the runtime clone, update labels/preferences and cover held-edge/modal behavior with focused EditMode tests.
4. Prepare the source derivative and projection only in the marked background candidate. Measure complete source timing and equipped poses; retain original files and metadata.
5. Exercise movement ownership, collision/support queries, pause, disable/reset, cooldown, run-out and rejection paths without Play Mode, automated gameplay input or physics trajectory simulation. Verify relevant jump, movement and directional regressions.
6. Qualify equipped poses and blends at one/two/four bone qualities on the main camp's sealed native terrain inputs. Match relevant candidate inputs to the live author scene.
7. Install sealed private pairs through guarded stable-EditMode imports, preserve dirty scene/root IDs and metadata, close only the owned validation Editor, record proof/recovery and commit exact task-owned files.

## World and authoring boundaries

This is transient actor state on fixed, always-loaded authored terrain. Deterministic world identity, chunk streaming/unload, generated-object IDs and persisted deltas are not applicable because none is created or changed. Terrain/support/collision constraints remain authoritative. Greater Wasteland promotion, package changes, external publication and source asset redistribution are outside this slice.

No scene save/reload or foreground app activation is authorized. Hands-on gameplay smoke and feel testing remain with the author. Offline pose, command/state and import proof must be reported separately from actual traversal acceptance.

Private originals, measured source frames and the continuing immutable checkpoint are under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-16/ImportCheckpoints/DodgeImplementation20261016/`.

## Completed result

East now triggers the complete directional roll. The 0.95 s derivative uses a source-derived nominal 2.4 m displacement, momentum-preserving entry and moving-gait recovery. The equipped source and separate native-ground body fitting passed without relaxing the existing body-correction or mesh bounds. Canister pickup moved to View/Back; F9 opens the palette and Backspace cancels diagnostic actions. Existing saved preferences adapt in memory.

185 focused EditMode checks passed. The verified private asset pairs and required dodge binding are installed in the already open main camp slice, preserving accepted gaits, jump tuning, native terrain and the unsaved author scene. No gameplay smoke or Play Mode test was run. See the [implementation evidence and recovery receipt](../../Evidence/Gameplay/DodgeImplementation20261016/README.md) for exact controls, hashes, proof limits and author testing scope.
