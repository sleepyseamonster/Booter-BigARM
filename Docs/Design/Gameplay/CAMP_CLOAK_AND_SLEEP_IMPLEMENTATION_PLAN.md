# Camp cloak and sleep implementation plan

Planning checkpoint dated October 10, 2026. Build a small, physical camp loop for Greater Wasteland: Booter deploys a vertical shade through the radial menu, rests or sleeps beneath his cloak, regains Reserve while spending supplies, and wakes into the same continuously simulated world. The recommended first release of the prototype is session-only and uses the existing player, input, inventory, animation, survival, and twilight authorities.

This plan turns the [brainstorming proposal](./CAMP_CLOAK_AND_SLEEP_BRAINSTORM.md) into dependency-ordered work. It does not implement the feature or approve every proposed tuning value. The current request authorizes planning; execution begins after an implementation instruction. A deployed-camp save integration is a separate milestone.

## Objective and observable success

The player can carry one reusable camp kit, call up a placement preview, orient and confirm the screen on supported terrain, approach it physically, and choose Rest, Sleep until ready, or Pack up. Booter remains visible while asleep under the cloak. Sleep accelerates the world, restores only Reserve, and drains food, water, and oxygen once per elapsed simulation interval. Manual wake, low supplies, damage, or lifecycle interruption promptly releases acceleration and preserves earned recovery.

The controller flow must work without a mouse, preserve the existing radial preferences, and prevent a single press from both opening and committing the next action. Packing returns the same kit locally. The camp stays behind if Booter flees. Current movement, climbing, gathering, canister placement, inventory, companion behavior, native terrain, and ordinary survival depletion continue to work.

## Authority and planning boundary

| Owner | Responsibility |
| --- | --- |
| User | Product direction, art acceptance, gameplay feel, and hands-on smoke testing |
| Gottspan | File ownership, sequencing, canonical documentation, integration, and final scope audit |
| Blender | Local native authoring, mesh and pose studies, export, geometry inspection, and art handoff |
| Babineaux | Unity import, serialized wiring, background-safe validation, and evidence boundaries |
| Gear Ball | Verified task-only local commits; publication remains separately authorized |

These are responsibility lanes, not instructions to start separate agents. Work on `main` tracking `origin/main`. Preserve other writers' changes. Do not focus Unity or other windows. No package upgrades, broad project settings, branch/worktree changes, external publication, terrain generation, terrain streaming, or archived-system reactivation are needed.

Stop a batch when its ownership is ambiguous, a required check fails, a serialized reference cannot be preserved, the installed tools cannot produce the required proof, or the next action exceeds the approved implementation scope. Resolve routine reversible technical choices inside the plan; surface consequential departures such as a new survival economy, rig replacement, saved-world migration, or purchased animation asset.

## Sources and current repository facts

Source priority is the user's direction, [root agreement](../../../AGENTS.md), [world basis](../../WORLD_BASIS.md), canonical standards, the brainstorming proposal, and live code/assets for implementation facts. Use the [local workspace guide](../../LOCAL_WORKSPACE.md), [input standard](../../Engineering/INPUT_ARCHITECTURE_STANDARD.md), [survival standard](./SURVIVAL_SYSTEM_STANDARD.md), [radial design](./RADIAL_MENU_SYSTEM_DESIGN.md), [Blender handoff SOP](../../Agents/Blender/sops/MODEL_TO_UNITY.md), and [Unity automation reference](../../Engineering/UNITY_AUTOMATION.md).

The October 10 file/process audit established:

- Unity is pinned to `6000.4.0f1`; URP is `17.4.0`, Input System `1.19.0`, Test Framework `1.6.0`, and Pipeline `0.8.0-exp.1`. No new package is required.
- Production runtime assembly is `BooterBigArm.TopDown3D.Runtime`, namespace `BooterBigArm.TopDown3D`. Editor assembly is `BooterBigArm.Editor`; focused tests use `BooterBigArm.Editor.Tests`, namespace `BooterBigArm.Tests`.
- `GreaterWasteland.unity` has the existing player inventory and animation driver. Its sun cycle is enabled with a 1,200-second duration. A 150-simulation-second sleep advances one eighth of that cycle.
- `TopDown3DSurvivalVitals` owns all five meters, runs ordinary depletion in Update, and exposes `Changed` and `SetValue`. Snapshot version 2 preserves the values. Oxygen has no ordinary automatic drain. There is no dedicated damage event in this vitals class.
- `TopDown3DInteractionController` discovers cargo access, Ironstone, and harvesters explicitly. `ITopDown3DInteractable` includes gathering-oriented reward and consumption fields. `TopDown3DPlayerActionController.TryBeginCurrentTarget` branches for cargo and harvesters before gathering. A camp requires explicit discovery and dispatch integration.
- `TopDown3DPlayerAnimationDriver` owns a Playables-based visual graph. Extend that authority for camp actions; do not install a competing Animator controller. `TopDown3DPlayerMotor` already offers owner-based action constraints.
- `TopDown3DInventoryUiSceneInstaller` wires inventory, radial, and system UI around one EventSystem. Installation must remain idempotent and must not reset the existing inventory.
- An Editor process owns this checkout and `Temp/UnityLockfile` exists. A separate terrain validation job was also observed. Do not launch batchmode against the production checkout or interfere with either process.
- Pre-existing dirty content includes `Greybox_BigARM.mat`, `FixedTerrainDressingStudy.unity`, and untracked fused-rock assets. These are outside this plan's ownership. Recheck the full manifest before execution; this snapshot is not a lock on other work.

No Unity import, runtime test, mesh creation, performance capture, or gameplay acceptance was performed by this planning task.

## First milestone scope and proposed defaults

Implement deployment, local camp actions, cloak presentation, deliberate awake Rest, Sleep until ready, manual wake, low-resource wake, health-loss interruption, packing, a footprint/exit preview, wake reason, and a fading recovery summary. Include actual threat-event integration only if a suitable production authority exists at execution time. Provide the interruption seam without inventing enemies or weather.

Use the three-panel vertical screen candidate: approximately 2.2 metres wide and 1.3 metres tall, inward-folding wings, weighted feet, opaque cloth, and a packed bundle. Fit it to the actual rig. Source meshes and backups remain local under `SourceArt/Blender/Studies/CampAndCloak/`; follow the [local-only source policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Exported Unity art and metadata remain publishable.

Proposed initial tuning, all editable and subject to user testing:

| Choice | Prototype default |
| --- | --- |
| Primary sleep action | Restore Reserve to 80 percent; no hour picker or full-meter buff |
| Sleep | 4x simulation, +0.4 Reserve per simulation second; food -0.015, water -0.028, oxygen -0.010 |
| Awake Rest | 20 simulation seconds at 1x, +0.3 Reserve per second capped at 40; food -0.020, water -0.032, oxygen -0.008 |
| Low-supply floor | 10 percent, plus the estimated stand-up supply cost; offer shorter achievable recovery |
| Setup and pack | Approximately two seconds each at 1x; brief reclining and standing transitions |
| Camp availability | One starter kit carried by Booter; no remote cargo consumption or camp retrieval |
| Radial assignment | Add an assignable Camp command; fresh profiles may use the empty east sector, existing custom layouts are preserved |

At these rates, sleep from 20 to 80 takes 37.5 real seconds and spends 2.25 food, 4.2 water, and 1.5 oxygen, excluding setup and pose transitions. No separate tiredness meter, healing, movement penalty, or automatic replenishment of supplies is introduced. Awake Rest intentionally extends the existing sleep-only recovery standard; update that standard at implementation cutover, while idle standing continues to deplete Reserve.

Defer the shade recovery bonus, return marker, free camera observation during sleep, literal figure-eight astronomy, cloak-only camping, storm rules, guarding, camp damage, crafting upgrades, establishment sleep, and saved-world integration. The placement camera may orbit using a dedicated camp intent; sleeping uses the existing follow camera with only a modest framing adjustment. This keeps the first milestone bounded.

## World identity and persistence

Terrain identity and generation version remain unchanged. A deployed camp is a player-authored runtime delta, with an instance ID assigned once at successful deployment. The session camp registry owns that ID and transform; Unity instance IDs, position hashes, inventory slot numbers, and dressing seeds are not identities.

Greater Wasteland terrain is fixed and always loaded, so terrain chunk generation, border regeneration, and terrain unload/reload are not applicable. Dressing visibility may hide a visual but cannot delete camp state. Use scene-scoped world ownership rather than parenting a deployed camp beneath Booter. Re-entering a fresh session does not restore camps in milestone one, and the player-facing prototype notes must say so.

Respect terrain support, slope, approach, body, standing, escape, and protected authored areas. Repeat the same support/clearance checks at confirm and sleep entry. Placement validity is deterministic for the same transform and sampled inputs; no new seed-based placement generator is needed. Use bounded physics probes only while placing or interacting, and one small camp record per kit.

Preserve vitals snapshot versions 1 and 2; transient sleep mode, acceleration, input state, and cloak presentation do not enter that snapshot. Do not attach the generated-world save service. A later save milestone needs an actual production world/save identity, versioned camp records, atomic kit/camp ownership, packed-camp removal, vitals and shared clock continuity, and reload/restart tests. It restores the player awake at normal speed. That milestone cannot be claimed complete with session-only tests.

## Candidate approaches and recommendation

| Approach | Authority and affected surfaces | Benefits | Risks and proof |
| --- | --- | --- | --- |
| Extend canonical systems with a bounded camp coordinator | Existing input, inventory, interaction, animation, and vitals plus a scene-owned camp registry | Small cohesive feature; preserves familiar controls and data owners | Requires careful modal and action integration; prove existing interaction dispatch, input, animation, and snapshot regressions |
| Generalize portable-object placement first | Extract canister placement and camp placement into a shared service, retaining separate inventories and object rules | Reuses preview, support, transactions, and cancellation for later deployables | Refactors an already working canister path; larger migration and regression scope; requires canister equivalence and transaction tests before camping |
| Author a dedicated camp harness and prefab first | Reference scene exercises the real runtime camp components before production installation | Contained art/input iteration and serialized wiring; easy to inspect poses and pivots | Reference-only success cannot prove production installation; still requires canonical inventory, UI, and world wiring at cutover |

Recommend the first approach, using the third approach's isolated reference harness for early asset inspection. Reuse small existing helpers where their contracts match, but do not refactor canister placement solely to make camping possible. There is one camp implementation in both scenes, one vitals owner, one player animation graph, one EventSystem, and one inventory authority. This limits serialization churn while making rules independently testable.

| Runtime authority | Proposed contract |
| --- | --- |
| Camp state/coordinator | Own deployment and action states, session ID, active camp ID, interruption reason, and transition ordering |
| Camp registry | Own committed deployed records and view lifetime within this scene; enforce one deployed instance per starter kit |
| Existing inventory | Store `tool.popup_camp`; deployment removes one only at commit, packing adds one only after capacity and reach checks |
| Existing vitals | Own values and the sole elapsed-time advancement; select ordinary/rest/sleep rates via an explicit activity mode |
| World speed controller | Own a releasable acceleration request, remember pre-request speed, refuse incompatible ownership, and restore on every exit |
| Existing input router | Own camp map activation, prompt device, gesture consumption, cursor, and transitions back to gameplay |
| Existing motor and animation driver | Own collision, movement constraints, facing/short approach, the visual graph, and poses; camp requests actions rather than disabling these systems |
| Camp view/UI | Display resolved state, preview geometry, anchors, costs, and wake results; no independent inventory or time mutation |

Deployment commit prepares a validated inactive camp view, rechecks kit ownership and support, removes the kit, registers the record, and enables the view as one guarded transaction. Failure compensates to the original state before returning control. Packing checks inventory capacity first, commits kit return and record removal together, then completes visuals. While deployed, the kit is world-owned, not also an inventory item. Do not reserve an inventory slot by adding a second inventory implementation; packing with no capacity reports a failure and leaves the camp intact.

## Planned source and asset boundary

All new paths below are proposals; preserve paired `.meta` files and existing GUIDs. Add no new assembly boundary.

| Boundary | Planned files |
| --- | --- |
| Camp rules and runtime | New `Assets/_Project/Scripts/Runtime/TopDown3D/Camping/TopDown3DCampState.cs`, `TopDown3DCampController.cs`, `TopDown3DCampPlacement.cs`, `TopDown3DCampView.cs`, `TopDown3DCampSettings.cs`, `TopDown3DCampSceneInstaller.cs`; keep registry and transaction records with state until separation is justified |
| World speed | New `Assets/_Project/Scripts/Runtime/TopDown3D/Simulation/TopDown3DWorldSpeedController.cs`; token lifecycle and supported speed checks |
| Camp UI | New `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DCampCanvas.cs`; extend `TopDown3DInventoryUiSceneInstaller.cs` for a single explicit install handoff |
| Input and radial | Existing `TopDown3DInputRouter.cs`, `UI/TopDown3DRadialState.cs`, `UI/TopDown3DRadialMenuController.cs`, `UI/TopDown3DMenuController.cs`, and `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions` |
| Physical interaction | Existing `Interaction/TopDown3DInteractionController.cs`, `TopDown3DPlayerActionController.cs`, and `ITopDown3DInteractable.cs`; add explicit camp discovery and camp dispatch before the gather path, retaining the interface for now |
| Player presentation | Existing `TopDown3DPlayerAnimationDriver.cs`, `TopDown3DPlayerMotor.cs`, and `TopDown3DCameraRig.cs`; extend their action contracts only where required |
| Survival | Existing `Survival/TopDown3DSurvivalVitals.cs`, `TopDown3DSurvivalSettings.cs`, and `Assets/_Project/Settings/Survival/Resources/TopDown3DSurvivalSettings.asset` |
| Items and configuration | New `Assets/_Project/Settings/Items/Item_PopupCamp.asset`, add its definition to existing `TopDown3DItemCatalog.asset`, new `Assets/_Project/Settings/Camping/Resources/TopDown3DCampSettings.asset` |
| Art and prefabs | New camp exports in `Assets/_Project/Art/Environment/Camp/`, cloak exports and clips in `Assets/_Project/Art/Characters/Booter/Cloak/`, materials in `Assets/_Project/Materials/TopDown3D/Camp/`, camp prefab in `Assets/_Project/Prefabs/TopDown3D/Environment/Camp/` |
| Scene consumers | New `Assets/_Project/Scenes/Reference/CampSleepStudy.unity`, disabled in build settings; controlled starter-kit/clip-reference wiring in `Assets/_Project/Scenes/Production/GreaterWasteland.unity` only after reference gates pass |
| Proof and docs | New `Assets/_Project/Tests/Editor/TopDown3DCampTests.cs`, `TopDown3DCampInputTests.cs`, `TopDown3DSleepTests.cs`, `TopDown3DCampIntegrationTests.cs`; new `Assets/_Project/Scripts/Editor/Validation/TopDown3DCampProductionValidator.cs`; update survival standard and feature evidence at cutover |

Reference and production scene builders may serialize player inventory and clips. Batch 0 must trace those builders and update only the exact consumer that would otherwise erase camp wiring; do not run a wholesale production-scene rebuild. Use stable explicit references or authored resource configuration, failing clearly on missing required assets. Installation may discover the existing player once through the current scene installer, but no per-frame scene scan or duplicate bootstrap path is needed.

## Implementation batches and gates

### Batch 0 Revalidate and seal the work boundary

Gottspan and Babineaux inspect current status, branch, Unity process/lock state, asset references, player rig, catalog consumers, production builders, current damage/threat authorities, and all time-scale writers. Verify the installed Blender executable before authoring. Snapshot task-relevant GUIDs and production terrain hashes. Record the exact task manifest and validation-project path.

Gate: dependencies and background-safe proof path are identified, no file ownership overlaps, and first-milestone defaults are covered by implementation authority. If the approved scope changes, revise this plan before editing coupled assets.

### Batch 1 Build and inspect the mesh and pose study

Blender authors packed/deployed shade and worn/blanket cloak, with hinge, hand, ground, approach, sleep, and stand anchors. Use the brainstorming budgets and two-material target. Create brief setup, pack, reclining, sleep-loop, rest, and wake clips for the existing rig; keep root motion disabled and locomotion clips intact. Babineaux imports exports with controlled scale, axes, normals, materials, and collision, then wires the reference study.

Gate: saved native source reopens, exports contain only intended meshes, mesh/material checks pass, pivots and actual Unity dimensions agree, both sides of the shade render, and cloak poses do not hide the player's identity or obstruct the feet. Record background render evidence; final art/feel acceptance remains with the user. Stop on a missing rig/clip dependency rather than installing a different character or asset package.

### Batch 2 Implement camp rules and kit transactions

Add settings, camp state, instance IDs, registry/view ownership, support/clearance probes, and the kit item/catalog entry. Implement cancellation and atomic deploy/pack rules before UI. Extend physical target discovery and branch camp interaction before gathering. Camp interaction opens an action request; it never awards a gathering reward or consumes the camp through `TryConsume`.

Gate: unsupported/slope/overlap placements reject; confirm revalidation rejects newly occupied space; duplicate confirms create one camp; cancellation consumes nothing; failed creation restores ownership; packing with a full inventory preserves the deployed camp; terrain and canister records remain unchanged.

### Batch 3 Wire radial placement and local gamepad actions

Append the Camp command without renumbering existing enum values. Add camp placement/sleep action intents and router modes, explicit radial dispatch, action panel, prompts, footprint/escape preview, and one installation handoff. Keep older preferences valid and existing custom sectors unchanged. Gate all other modal opens and canister/gather actions while camp owns input.

Use the brainstorming control table: left stick/WASD moves a nearby ghost, D-pad left/right or Z/C rotates, D-pad up/R reorients to the sun, South/Enter confirms, East/Escape cancels, and West/E opens local camp actions. Sleep confirmation needs a fresh Submit. Start hands off to the system menu; Escape has contextual cancel/wake priority. Apply existing device/rebinding authority and neutral-return guards.

Gate: default and explicit radial styles work; one gesture cannot deploy or sleep twice; cancel beats confirm; movement, attacks, canister pickup, camera, and cursor cannot leak between maps; no second EventSystem appears. Device loss/focus loss cancels uncommitted work and preserves committed objects.

### Batch 4 Implement poses and survival recovery at normal speed

Extend motor-owned action constraints and the existing Playables driver. Add a short physically validated approach, reclining, rest/sleep pose, cloak switching, and waking. Keep the player collider and damage reception active. If standing space becomes blocked, try only nearby validated escape positions under motor control; never teleport or trap the player until an animation finishes.

Extend vitals with an explicit activity mode and a pure rate calculation used by its existing advancement path. Reserve recovery replaces ordinary Reserve depletion during Rest/Sleep; the values are still written by the vitals owner once. Add a synchronous health-decrease event in `SetValue`, while snapshot restore/reset does not count as an attack. Existing damage authority, if present, must use this path or provide an explicit event adapter.

Gate: ordinary rates and v1/v2 snapshot results are unchanged; idle standing never restores Reserve; Rest caps correctly; equal simulation duration gives equal recovery/costs; interruptions keep partial progress; worn and blanket meshes are mutually exclusive; body collision and existing gather/traversal animation authority remain valid.

### Batch 5 Enable continuous accelerated sleep and interruption

Add a scene-owned speed controller and release token. Activate acceleration only after the legal sleep pose begins. Start at 4x with the existing fixed step preserved; inventory and menus use responsive real-time input. Clamp sleep advancement to target/resource/interruption boundaries, integrate only elapsed sleep time, then end acceleration before waking. Damage interruption releases the request synchronously before another simulation attack step.

Audit every active gameplay timer, AI/navigation path, animation, particle, and weather authority for scaled versus wall-clock semantics. Keep wall-clock diagnostic/UI timers real-time; fix only gameplay timers that prevent this feature's continuous-simulation contract. Restore the pre-request speed on normal wake, low supply, damage, death, destroyed components, scene unload, Start, disconnect, or focus loss. Refuse conflicting acceleration rather than overwriting it.

Gate: no double depletion; the sun progresses with the same elapsed time; resource floor and target stop exactly; every exit restores speed and motor/input ownership; no forced ambush or invulnerability; 4x does not skip collision/attack opportunities or create unacceptable frame/physics backlog. If performance fails, reduce supported acceleration and retune recovery within the approved feel target rather than increasing the physics step silently.

### Batch 6 Integrate Greater Wasteland and close the prototype

Use explicit installer wiring, authored settings, catalog definition, and a single starter kit in the production player's initial inventory. Do not reconfigure/reset a live inventory or re-grant the kit each time an installer runs. Add the required clips/config references without changing terrain or unrelated hierarchy. Update exact builder consumers found in Batch 0 to preserve those values on deliberate future builds.

Add a non-saving camp validator. Verify scene-owned camp registry, closed UI, no active speed request on startup, one EventSystem, item definition and initial count, legal anchors, required visuals/clips, and no generated-world service dependency. Add wake reasons and the actual recovery/cost summary. Update the survival standard and evidence with the session-only limit.

Gate: focused checks and existing affected regressions pass, the serialized candidate contains only intended edits, protected terrain hashes/GUIDs remain accounted for, and evidence separates import/structural proof from art, feel, performance, and player acceptance. Commit verified task batches only; no push. Do not claim the prototype complete if core asset or simulation proof is missing.

### Later milestone Saved camp continuity

After separate scope approval, bind camps to the real production saved-world identity and versioned world-delta owner. Prove deploy/save/reload, pack/save/reload, scene re-entry, object visual unload/reload, repeated restore without duplicates, vitals/clock continuity, and interrupted sleep resuming awake at ordinary speed. Do not publish a temporary camp-only save file as production world persistence.

## Verification matrix and execution path

| Claim | Required proof | Limit |
| --- | --- | --- |
| Scope and reference safety | Exact diff, `.meta` pairs/GUID checks, relative links, catalog/input reference checks, terrain hash comparison | Does not prove rendering or gameplay |
| Mesh and animation handoff | Blender reopen/geometry report, Unity import, fixed background render views, clip/rig binding checks | User judges art and repeated-action feel |
| Camp and inventory transactions | New `TopDown3DCampTests` plus affected existing inventory and harvester tests | Session behavior only |
| Modal/controller correctness | New `TopDown3DCampInputTests` plus `TopDown3DRadialTests`, `TopDown3DMenuTests`, `TopDown3DMenuUiIntegrationTests`, and affected inventory UI checks | Physical controller feel remains user-owned |
| Recovery and interrupts | New `TopDown3DSleepTests` plus `TopDown3DSurvivalTests`: rate/clamp boundaries, partial elapsed time, synchronous health loss, target/resource exits, snapshot compatibility | Pure elapsed-time tests do not prove encounter pacing |
| Production wiring and cleanup | New `TopDown3DCampIntegrationTests`, camp production validator, existing radial/menu production validators where affected | Read validator implementation before invoking; no saving scenes |
| Live speed behavior | Structural/focused lifecycle tests and a named background-safe representative 1x/4x capture through an authorized deterministic harness | User owns hands-on sleep/threat smoke testing; do not invent combat or run unrequested PlayMode smoke tests |
| Performance | Report frame and fixed-step/backlog measurements for the camp, nearby companion, and existing terrain at supported speeds | A validator pass is not performance proof |
| Persistence | Explicitly excluded from first milestone | No claim of deployed-camp restart continuity |

Use the installed pinned Editor, discovered CLI schemas, and the [documented workflow](../../Engineering/UNITY_AUTOMATION.md). First inspect `unity editors running --json` and target the exact project explicitly. Reuse supported background-safe commands in the existing Editor, or an isolated candidate copy; never start competing batchmode against this locked checkout. Keep logs under ignored `Logs/`.

For an unowned isolated candidate, the focused test command shape is `Unity.exe -projectPath <candidate> -batchmode -nographics -runTests -testPlatform EditMode -testFilter <qualified fixture> -testResults <xml> -logFile <log>`. Run each changed behavior fixture and the affected regressions above, checking XML execution counts and failures. Existing validator entry point is `BooterBigArm.Editor.TopDown3DRadialProductionValidator.ValidateFromCli`; planned new entry point is `BooterBigArm.Editor.TopDown3DCampProductionValidator.ValidateFromCli`. The existing validator installs objects in memory and delegates other validators, so re-audit those callees before execution; it is not a feature builder.

No broad build or unrelated suite is required for this slice. If Player-only loading, stripping, or animation behavior becomes a material unresolved concern, obtain scoped build proof before declaring that behavior verified. Keep hands-on gamepad, camera, cloak clipping, visible sleep, wake reaction, and repeated-use balance as named user acceptance checks.

## Risk and recovery boundaries

- **Duplicate possession or lost kit:** guarded commit, compensation, repeated-input tests, and packing capacity check. No destructive inventory reset as recovery.
- **Movement or tool input leakage:** one modal owner, fresh-press/neutral return, contextual Escape/Start arbitration, existing action-constraint ownership tests.
- **Acceleration survives interruption:** idempotent token release and teardown coverage; do not restore global speed by assuming every prior state was 1x.
- **Lost attacks or doubled resource costs:** synchronous damage interruption, boundary-clamped integration, one survival advance owner, fixed-step cost measurements.
- **Cloth clipping or competing pose graph:** current rig and Playables integration, explicit visual switching, collision kept separate, fixed view inspection.
- **Production scene overwritten by builders or concurrent work:** trace serialized consumers, isolate candidate proof, narrow scene edits, and preserve all unrelated dirty assets.
- **Prototype mistaken for persistent world support:** session-only declaration in evidence and user handoff; defer save files until a production world owner exists.

If a batch fails, leave its candidate isolated or repair its task-owned changes, then rerun the affected checks. Do not reset, clean, delete metadata, stop someone else's process, change package pins, or run a scene rebuilder to manufacture a passing state.

## Completion and next action

The plan is ready for an implementation instruction using the proposed first-milestone defaults. The next execution step is Batch 0, then the mesh/pose study and pure camp rules. No further brainstorming is a prerequisite for routine work; material creative departures remain the user's decision.

The first milestone is complete only when deployment, local Rest/Sleep/Pack, cloak presentation, recovery/costs, continuous supported acceleration, interruption/cleanup, production wiring, focused regressions, and relevant asset/performance proof pass. Report remaining user art/feel acceptance explicitly. Saved-world continuity, publication, and deferred features are outside that completion claim.
