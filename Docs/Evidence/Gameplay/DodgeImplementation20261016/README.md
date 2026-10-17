# Camp Loop dodge implementation — October 16, 2026

Installed in the already open **CampLoopTesting** main camp slice and ready for author hands-on testing. Unity 6000.4.0f1; `main`. The feature uses the existing sole Rigidbody movement owner and sole animation output. No author scene save/reload, Play Mode, foreground activation, package change or external publication occurred.

## Subsequent transition audit — October 16

The [running/sprint recovery audit](../../../Design/Gameplay/CAMP_LOOP_DODGE_TRANSITION_AUDIT_2026-10-16.md) reproduced two handoff defects: held LT facing lock drops on the first ordinary recovery tick, and side/back rolls reset motor facing before the local pose fade. The source/geometry checks below remain valid, but they do not establish smooth temporal recovery. The subsequent [transition fix](../DodgeTransitionFix20261016/README.md) resolves those ownership defects and bounds moving-entry braking, with 222 passing focused checks and a guarded import into the open main camp. Accepted gaits remain unchanged; author hands-on feel acceptance is pending.

## Controls and behavior

- Gamepad **East**: one directional dodge roll per press. Left stick chooses direction; neutral uses current facing.
- **View/Back**: canister pickup / item cancel in the slice. **Backspace**: diagnostic animation cancel. **F9**: animation palette. Menu Back remains East.
- Existing binding IDs and stored preference files remain intact. Superseded slice defaults adapt in memory. Held buttons, modal return and replacement devices require neutral input before a new request.
- Complete roll takes 0.95 s, with an 80 ms entry blend, 160 ms moving-gait recovery and 200 ms cooldown after the roll stage. The last 20% blends velocity toward held movement. Nominal source travel is 2.4 m; entry speed, run-out and collision change actual displacement.
- LT facing/look-ahead state survives the lease. Airborne, crouched, mounted, dead and busy requests are rejected. Pause, death, disable, reset and disposal release or freeze the appropriate owners.
- The existing upright capsule stays authoritative. Sweeps stop walls, walkable slopes supply a tangent and ordinary gravity owns a ledge departure. No invulnerability, stamina cost or crawl clearance is introduced.

## Source and terrain work

The existing DodgeRight is a near-stationary torso lean. Existing backward dodge is a long multi-part take. Fresh complete Mixamo **Standing Dodge Forward** and **Stand To Roll** exports were audited; Stand To Roll supplies the unarmed entry, rotation and recovery. [Source receipts](sources.json) seal original bytes and export settings.

The selected 0.1–2.1 s source interval is retimed to 0.95 s. Root X/Z curves are zero; a monotone source-derived displacement curve feeds the Rigidbody. Root Y is baked and fitted to the actual equipped character. Preparation and fitting run only in the marked background candidate and refuse to replace existing outputs.

A roll uses its hands, shoulders and back as support. Its dedicated visual tangent samples a 0.7 × 1.3 m footprint and fits a separate 399-point body cache against native terrain. It does not apply standing foot planting to the folded pose. The existing 180 mm action body-correction bound and 2 mm mesh penetration tolerance were retained. Soft cloak projection is separately bounded at 0.6 m for the roll. The accepted gait, strafe, crouch and jump projection caches remain unchanged.

Earlier source-only fitting and the capsule-contact tangent failed equipped/native checks. Those failures remain in the private checkpoint. The final equipped-source fitting and footprint tangent passed without relaxing the body bound or mesh assertion.

## Verification

**185/185 unique focused EditMode checks passed**, plus a repeated six-case input check after whitespace cleanup. [Tests and case results](tests.json) identify the exact suites: dodge controller/motor/poses, input, motor ownership, airborne, jump timing/presentation, expanded gaits, directional movement, low motion, crouch, pose envelope, reset and native ground.

Dodge pose checks cover 7,704 offline poses across one/two/four bone weights: five native sites and eight headings for full rolls, plus entry/recovery from six speeds and four gait phases at two sites. Equipped mesh samples retain the existing −2 mm threshold; graph output stays singular and garment bounds hold. The existing native-ground regressions also passed, including 26,880 accepted gait/directional poses.

[Fresh input comparison](candidate-input-contract.json) proves 18 terrain/collider records, nine TerrainData/meta pairs and 31 coupled motor/driver/input sources match. Both Editors independently matched all nine live native world matrices with clean TerrainData. Candidate lab/lighting/hierarchy differs from the dirty author scene; tests build their own actor/support fixtures. Greater Wasteland promotion is outside this slice.

[Installed readbacks](readbacks.json) prove the required qualified profile resolves in the author Editor, East and View bindings resolve correctly, dependencies pass and final compilation is complete. [Preservation seals](preservation.json) retain all 172 protected inputs, unsaved scene state and root IDs. Accepted moving speeds remain 1.4/3/4/4.9/6.15 m/s, with the accepted cadence and 1.6× jump gravity unchanged. Active compiler failure state was false; the author console's current error count was zero. Existing warnings were not cleared.

These checks establish source, state, geometry and import contracts. Actual traversal feel, collision behavior under physics stepping, roll pacing and directional/LT transitions require the author's Play Mode test. No automated gameplay smoke test was run.

## Private assets, recovery and scope

The eight new private files (four asset/meta pairs), modified local PlayerBindings and final source hashes are sealed in [the manifest](qualified-manifest.json). Mixamo payloads and private bindings remain ignored and local. The unused forward-step export and superseded failed derivative were not installed. Existing metadata was preserved.

The immutable recovery store is `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-16/ImportCheckpoints/DodgeImplementation20261016/`. `before/` contains prior task source and PlayerBindings bytes; `qualified/` contains the installed private bundle. The guarded installer checks stable Edit Mode, exact scene/root identity, clean binding assets, source/protected hashes and GUID ownership. Failure restores only the previous binding and retains newly imported pairs unbound. A requested full rollback must revert this feature's code change and restore the prior PlayerBindings pair together; retain the new private pairs for recovery rather than deleting/regenerating metadata.

This transient actor feature creates no deterministic world identity, streamed chunks, generated objects or persisted deltas. It respects the existing fixed, always-loaded authored terrain and runtime collisions. The author-owned Editor remains open; only the task-owned hidden validation Editor is closed at handoff.
