# Camp foundation and Ironstone processor implementation plan

October 14, 2026. **Planning only; implementation has not begun. Audited and revised October 14.** Source: the author's request for an extensible camp/item/companion-management skeleton and an Ironstone refinery, followed by “Create a plan” and “Audit the plan.” This plan turns the [audit and brainstorm](CAMP_SYSTEM_SKELETON_AND_IRONSTONE_REFINERY_BRAINSTORM_2026-10-14.md) into a bounded first implementation and explicit later milestones. New economy values and material names remain provisional lab defaults. Section 15 records the audit findings and their disposition.

## 1. Objective and observable success

Build the first camp-management foundation in `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`: a recognized starting workshop, registered physical stores/stations, one management modal with Items/Stations/Companions views, and one manually operated Ironstone processor with a retained local output tray.

The player can inspect eligible stock without pooling inventories, see the Legger's actual status, call him through the existing call-over behavior, process one complete sorting batch, and retrieve products into legal nearby storage. Full trays, stale commands, lost access, disable or reset cannot consume input without delivering every output. Existing battery crafting, canister continuity, Arc refinery, rest, salvage and riding remain intact.

**First-slice completion stops at dry sorting and status/call-over management.** Cutting, powered metal production and autonomous companion work are separate milestones below. This distinction keeps the requested long-term system extensible without treating every brainstorm idea as immediate implementation scope.

## 2. Authority, lane and stop conditions

The user owns product/creative decisions. Gottspan owns source boundaries and integration; Babineaux owns background Unity authoring/validation; Lorekeeper checks local canon/proposal status; Gear Ball owns the scoped verified commit. These are responsibilities, not new delegated chats. No sub-agents are required by this plan.

Runtime belongs to `Assets/_Project/Scripts/Runtime/TopDown3D/`, namespace/assembly `BooterBigArm.TopDown3D` / `BooterBigArm.TopDown3D.Runtime`. Editor automation belongs to `Scripts/Editor/TopDown3D/`, assembly `BooterBigArm.Editor`; tests to `Assets/_Project/Tests/Editor/`, assembly `BooterBigArm.Editor.Tests`.

Stop source/scene integration while another writer owns the same files. Revalidate the latest riding, animation and optics handoffs rather than copying a stale candidate. Do not focus any app, use the author-owned Unity checkout for batchmode, change branch/packages/settings, publish source archives, push or run gameplay smoke tests. A later instruction to implement this plan authorizes the bounded first slice; subsequent milestones remain outside it unless explicitly included.

## 3. Sources and refreshed repository facts

Priority: user's instruction and root agreement; [world basis](../../WORLD_BASIS.md); [Legger standard](BIGARM_COMPANION_STANDARD.md); live code/assets; [camp brainstorm](CAMP_SYSTEM_SKELETON_AND_IRONSTONE_REFINERY_BRAINSTORM_2026-10-14.md); retained [workstation proof](../../Evidence/Gameplay/CampWorkstations20261014/README.md) and older planning documents.

- Planning began on `main`, tracking `origin/main`, at `735fe3f25`; the worktree is mixed with active riding/scene and rendering edits. Every such existing change remains outside this planning task. Scene and action-controller hashes must be refreshed again in Batch 0.
- Pinned Editor is `6000.4.0f1`; manifests specify Input System `1.19.0`, URP `17.4.0`, Test Framework `1.6.0` and uGUI `2.0.0`. Preserve them. No root `Temp/UnityLockfile` or Unity process was found in the planning preflight; this is a momentary fact, not permission to launch there later.
- [Waypoint settings](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Camping/TopDown3DWaypointCampSettings.cs) already define checkpoint/world identity `greater-wasteland.authored.v1` and site identity `camp.starting`. Reuse those. The existing authored device-placement identity separately uses production scene GUID `4de5dd018ee194314a00fd369e2d3eeb`.
- EconomySession currently binds three inventories and one refinery, seeds the linked chest once and provides a separate economy reset. InventoryTrial has one external reset blocker and one world-participant callback; blindly registering another callback would replace the old owner.
- InventoryState and its prepared transaction already support lists of item amounts and multiple participating stores, including equipment IDs/contents. CraftingRecipe/CraftingService are single-output and require distinct input/output authorities.
- The starting loadout is six equipment slots and four supply pockets; cargo has twelve mounts; the chest has 24 slots. Resources use accessible Legger/local storage. Old mining prose describing a Booter resource fallback is superseded by current routing.
- Batteries are quantity-one supplies represented by empty/charged item IDs, not numeric charge. Raw Ironstone mass is 1.25. The existing raw-ore battery recipe and ten-dust Arc charging cost remain unchanged.
- Camp Loop is marked session-only. Existing production checkpoint code does not save station buffers, refinery power/jobs or a camp roster aggregate. This plan introduces no production disk-save service.

## 4. Scope and prototype defaults

| In the first slice | Preserved/deferred |
| --- | --- |
| Starting-site binding and a bounded capability/store registry. | No new location authority, portable workshop spawning or multi-camp construction. |
| Management Items/Stations/Companions views; contextual station actions. | No remote item access, camp safety zone, recruitment or enemy-to-companion conversion. |
| Legger identity, actual position/proximity, route/load availability and call-to-Booter. | No wait/home/haul/work orders, second NPC motor or teleport recovery. |
| One processor and sorting definition; four material item definitions. | No cutting, metal output, power port, fuel, water, queue or timer yet. |
| Eight-slot output store, all-or-nothing processing and full-stack transfers. | No automatic output delivery, drops/discard, “process all,” multi-source consumption or same-store conversion. |
| Composable economy/full-trial reset, retained snapshots and recovery. | No application-restart persistence, offline work or production promotion. |

Proposed sorting input/output: **4 raw Ironstone → 2 coarse aggregate + 1 iron-rich concentrate + 1 mineral sand + 2 mineral dust**. Result masses respectively 0.75, 2.0, 1.0 and 0.25; all are Resource items, max stack 99, BigArmOnly carry preference. Total material mass remains 5.0. Mineral dust is distinct from `resource.airborne_dust` and is never valid Arc-canister/energy input.

Use proposed lab IDs `lab.ironstone.aggregate`, `lab.ironstone.concentrate`, `lab.ironstone.sand`, `lab.ironstone.mineral_dust`. One batch per explicit confirmation, instant atomic execution, no power cost for the manual sorting experiment. Tray mass limit is unbounded in this first slice, with finite eight-slot/stack limits; destination cargo mass checks still apply. No extra starter ore is granted: gather from the existing deposits, leaving the four-ore battery seed contract unchanged.

These reversible test defaults are sufficient to prepare implementation. They do not establish final lore, chemistry or balance. Adding different base metals, changing the battery recipe, charging partial batteries or enabling labor/recruitment requires another specified slice.

## 5. World identity and lifecycle contract

| Concern | Required behavior |
| --- | --- |
| Site identity | Bind to the existing waypoint WorldId/SiteId; retain its authored anchor. A workshop profile lists capabilities, not a duplicate coordinate or discovery owner. Portable camp GUIDs remain rest-instance identities. |
| Station/store identity | Registry is scoped to one loaded Scene instance, then keyed by world/site plus stable member ID. Production and lab may share waypoint IDs without sharing authorities. Preserve existing station/cache IDs, gear instance IDs and companion ID. Give the new processor/tray explicit authored IDs; reject duplicate keys and the same state registered under multiple aliases. |
| Generated identity | No generated placement is added. Future player-created sites would allocate/persist identity once, but no generator is introduced here. |
| Terrain streaming | Not applicable: the nine native lab terrain tiles are fixed and resident. Do not activate retired terrain generation/streaming. |
| View disable/reload | Detach views/subscriptions and invalidate commands; retain inventory, processor revision and epochs. Use a runtime registration generation and session token to reject commands prepared before disable, owner replacement or domain reconstruction, even if IDs/revisions repeat. A visual rebuild resolves existing records and never grants stock or creates another store. |
| Serialized runtime deltas | Retain the processor envelope/version, tray InventorySnapshot, operation revision and economy epoch. Existing slot snapshot versions and canister payload formats remain intact. Invalid retained data is preserved and blocks mutations, never replaced with an empty tray. |
| Authored constraints | Survey ground, capsule/body clearance, station approaches and Legger loading space after other scene integrations. Preserve all native terrain/collider data, nodes/rocks, actors, return/sleep routes, salvage, AP01–AP12, NPC/riding and rendering records. |
| Persistence boundary | Domain/scene-view reconstruction proof only. A later production aggregate must save all stores/sockets/jobs/roster orders together, with versions and prevalidated restore; the old generated-world save service stays inactive. |
| Performance | Serialized bindings and cached ID lookup; event-driven stock refresh. While the modal is visible, bounded status refresh (at most every 0.4 unscaled seconds) may sample actor position, follower route state and access because those lack a complete event contract. Command-time checks remain immediate. No per-frame scene scans, quantity polling across every NPC or background factory simulation. |

## 6. Alternatives and selected architecture

| Approach | Fit and tradeoff |
| --- | --- |
| Extend the current session/UI switches with a processor field and fourth owner index. | Small immediate change and easy coexistence; repeats hard-coded dispatch/reset growth and does not establish the requested camp skeleton. Viable for an isolated station study, rejected as the main foundation. |
| Add a bounded site/member registry and station adapters, reusing existing inventories/transactions. | More explicit binding/lifecycle work, but each physical owner stays canonical and future stations do not need another global manager. Lowest useful generalization for this task; recommended. |
| Introduce a complete persistent job/worker/factory graph now. | Supports future queues and labor directly, but requires reservations, clocks, save migration and actor executors before the first sorting interaction. Viable as a later large slice, excessive here. |

Choose the second approach. Adapters for the existing bench/refinery/cache must preserve their services, access checks, stable IDs and reset semantics. The registry discovers capabilities and stores; it owns no copies of quantities, equipment or NPC positions. EconomySession remains the lab bootstrap/grant owner and delegates discovery/reset composition to the registered owners. CampController remains the portable-rest owner.

Kit and Legger cargo are actor-owned stores projected into this site's session, not stationary camp members. Their registration follows actual actor identity and lifetime; the camp never keeps a second cargo snapshot after the Legger leaves. Resolve waypoint identity from the existing controller/definition after its installer is ready, without spawning another starting camp or recreating discovery. A disabled or out-of-range individual capability is unavailable locally; it does not unregister unrelated stores or disable a valid legacy workbench. A shared catalog/ownership corruption still blocks affected mutations.

Use a new **multi-output process definition** over the existing item-amount records and prepared transaction API, rather than changing the serialized single-output battery recipe immediately. A stateless recipe-quote adapter may present both definitions consistently; it does not become another mutable item/recipe authority. Do not duplicate the catalog or transaction engine.

Append a `StationResources` owner policy without renumbering current enum values, and handle it explicitly as resource-only: reject equipment/supply/canister payloads, enforce slot/stack limits and validate snapshots. Do not represent the tray as fake Legger cargo. The tray component wraps one InventoryState and owns its retained envelope; the processor owns operation readiness/revision only.

Audit every inventory-owner switch/fallback before adding the enum value: the current policy's final branch treats any non-Booter/non-cache owner as BigArm. The new branch must precede it, and UI routing must use registered descriptors rather than enum arithmetic. The output tray is read-only as a transfer destination: only a committed process or an explicit reset/validated restore writes it. Neither the generic Items view nor the old chest view may deposit stock into it. Recipe ingredients come from the selected cargo/chest, not from a new input buffer.

## 7. Proposed source and asset boundary

Final filenames can be adjusted once to match project conventions; responsibilities and ownership remain fixed.

- New runtime Camping files: `TopDown3DCampSite.cs` (waypoint-bound membership/lifecycle), `TopDown3DCampRegistry.cs` (bounded ID map), `TopDown3DCampStoreBinding.cs` (actual owner/access descriptor), `ITopDown3DCampStation.cs` / `TopDown3DCampStationAdapters.cs` (validated station dispatch), `TopDown3DProcessRecipe.cs`, `TopDown3DProcessingService.cs`, `TopDown3DIronstoneProcessor.cs`, `TopDown3DStationResourceStore.cs`, and `TopDown3DCampResetParticipant.cs` (prepared reset contracts). Use plain records where no MonoBehaviour is needed.
- Extend existing `Camping/TopDown3DCampLoopEconomySession.cs` and `Inventory/TopDown3DCampLoopInventoryTrial.cs` for registered stores/participants. Extend `Inventory/TopDown3DInventoryPolicy.cs`; reuse State, TransferService and Transactions rather than rewrite them.
- Add a narrowly scoped exact-stack transfer entry point in `Inventory/TopDown3DInventoryTransferService.Commands.cs`, preserving existing maximum-fit defaults. It accepts the selected slot/revision, destination revision and access predicate; it prepares both drafts and rejects unless the entire selected stack fits. Do not use the existing partial-fit `TransferSlot` unchanged for processor retrieval or the exact item transfer described by this new view.
- Extend `UI/TopDown3DCampWorkstationUiController.cs` / Canvas and Menu/Inventory `.CampWorkstations.cs` handoffs into one camp modal/session owner; add `UI/TopDown3DCampOverviewCanvas.cs` only for the overview presentation, with no second EventSystem/input owner. Change `Interaction/TopDown3DPlayerActionController.CampWorkstations.cs` through the registry; preserve the base/Riding dispatch and animation station receiver.
- Add a bounded read-only companion projection in `Camping/TopDown3DCampCompanionStatus.cs`; call the existing follower command after capability/life/mount checks. Do not change NPC policy/navigation or companion snapshots in this slice.
- Data: `Settings/Items/CampLoopIronstone/` for four product assets and sorting recipe; extend the existing `Settings/Items/CampLoopLoadout/Resources/CampLoopLoadoutProfile.asset` definition list. Preserve source salvage catalog bytes/GUID.
- Content: `Prefabs/TopDown3D/Camping/IronstoneProcessor.prefab`, reusable existing station materials where suitable, and `UI/Items/CampIronstone/` for simple owned product icons. Every new asset retains its paired metadata; no Blender/source archive changes.
- Editor: `Scripts/Editor/TopDown3D/CampIronstoneAuthoring.cs` with separate asset creation, isolated scene installation and read-only validation entry points. Scene authoring is an additive, source-hash-guarded delta against current `CampLoopTesting.unity`.
- New focused tests: `CampSiteRegistryTests`, `CampProcessingTests`, `CampStationStoreTests`, `CampAggregateResetTests`, `CampManagementUiTests`, `CampCompanionStatusTests`, `CampIronstoneSceneTests`. Evidence/document update belongs to the feature's closeout, not unrelated mining-document cleanup.

## 8. Command, snapshot and reset contracts

### Processing

An operation captures the Scene/session token, registration generation, actual source/tray authority references, site/processor/tray IDs, processor revision, recipe ID/version and immutable validated content fingerprint, source and tray revisions, and economy epoch. The fingerprint covers input/output IDs and quantities and relevant catalog type/mass/stack values, so editing an asset without incrementing its version invalidates an old quote. Input source is one selected physical cargo/chest. Tray is the sole output destination, distinct from source. The confirm path:

1. Check registered ownership, actor eligibility/input mode, station access and source access. Reject duplicate/unavailable/recovery owners.
2. Build one source and one tray draft; remove four ore and try adding the complete result list. Validate positive bounded quantities, definition types and explicit mass balance. Use checked quantity arithmetic even though first UI batch count is one.
3. Prepare processor revision/operation state as a world participant. Processor, inventory revision and epoch exhaustion reject without mutation. Acquire through the existing transaction boundary, recheck access/revisions/registration/fingerprint, install all state, then publish. All fallible lookup, validation, arithmetic and allocation occurs before installation. Prepared Install delegates only assign prevalidated fields and cannot throw, call observers, invoke Unity or serialize; the existing transaction installs inventories before world mutations and cannot roll back a throwing Install. Publish observers run after every owner is installed, under the existing locks.
4. Repeated confirmation with the same captured revisions fails. A newly prepared explicit command may process another batch. No stock transfer or byproduct emission happens in UI callbacks/presentation.
5. Take a selected full stack from tray to legal cargo/chest using the exact-stack entry point and both physical access checks. If only part fits, reject and leave the entire stack; show the limiting slot/mass reason. No partial batch processing or implicit disposal. No initial Take All feature. Existing legacy transfer screens retain their current maximum-fit semantics.

Recipe validation rejects duplicate/unknown IDs, empty or excessive result lists (maximum eight), invalid quantities, non-resource inputs/outputs, input/output ID overlap, mineral-dust/Arc-Dust substitution and non-finite/negative masses. Compare accumulated material mass using a documented tolerance of 0.0001 mass units for this bounded recipe; validate each individual mass before summing. Already committed products remain ordinary stock when a recipe asset changes; never re-evaluate old yields on load.

### Startup and retained state

Wait for the existing marked lab services and catalog resolution. Resolve explicit site/member/store bindings; validate one instance of each required owner, shared catalog compatibility and retained state before enabling UI. Initialize a fresh tray once and empty, without any ore grant. A retained valid tray replaces neither inventory authority nor subscriptions. A malformed envelope or incompatible snapshot preserves its bytes/state and disables processing/transfers for recovery. Repeated open/enable/rebinding never reseeds or duplicates UI/state.

For a fresh extended session, preflight the waypoint binding, new definitions/recipe, empty tray/processor retention and reset-participant set **before** the existing economy seed grant. Do not let initialization wait on its own Ready flag, or let InventoryTrial capture a baseline and expose Reset before that preflight completes. Commit the seed's existing initialization flag/ID once; afterwards adopt the original inventory baseline plus the known empty tray baseline, and enable the new UI. For a retained session, load/validate existing baseline data rather than recapturing current used stock. A later capability failure preserves committed old grants and valid legacy services; retry/re-enable never grants again.

Version the extended trial-baseline envelope separately from slot snapshots. An existing valid legacy baseline may be extended once with the authored processor's known empty initial tray baseline when its new retention is demonstrably fresh; keep the existing kit/cargo/cache/world snapshots byte-equivalent in meaning. Never substitute today's stock or tray contents for that initial baseline. If a retained processor exists but its baseline record is missing, preserve both records and block reset for recovery rather than guessing. Test legacy baseline upgrade, repeated upgrade and missing/invalid participant records.

Specify new retained envelope version 1 with explicit initialized/present flag, world/site/member IDs, capacity, inventory snapshot and processor operation revision. EconomySession remains the only epoch authority; the new envelope may record its epoch for consistency validation but never installs a competing epoch. Capture processor/tray state together only outside a commit; a serialization callback during locking leaves the prior valid envelope intact. InventorySnapshot stores slots, not InventoryState.Revision: reconstructed inventories use the existing revision lifecycle and a fresh session token rather than promising to restore that revision. Reject initialized-but-missing, wrong-ID/capacity, unknown-version, negative revision and inconsistent-epoch retention without overwriting it. No production snapshot version changes are needed.

### Composable reset

Replace the lab's single station callback usage with a bounded participant composition owned by EconomySession. Inspect and cut over every existing owned caller in the same batch; preserve each existing blocker and prepared mutation inside that composition, with no parallel legacy reset branch. Each participant supplies a stable reset ID, validation/block reason and prepared mutation, with no installation side effects during preparation.

The composition returns a list of inventory drafts and a list of scalar/world participants; do not wrap nested Commit calls inside one world mutation. Deduplicate authorities and participants before locking; each store and epoch owner appears once. Reset confirmation captures reset kind, epoch, registration generation/session token and participating store/world revisions. If any changes before the second confirmation, cancel that quote and require a newly displayed confirmation. Check activity/life/input and all blockers again at commit. A remote cargo reset remains an explicitly described trial reset exception, never ordinary crafting/transfer access.

**Camp economy reset:** preserve its existing scope and confirmation: restore the original empty starter canister ID and four chest ore; clear trial batteries/devices/refinery; additionally clear the processor tray and remove the four new lab product IDs from registered kit/cargo/chest/tray. Preserve other IDs, cargo raw ore, salvage ledger/depletion, actors, vitals and riding state. The existing reset replaces chest raw ore regardless of provenance; the confirmation must continue to disclose replacement rather than imply preservation of extra mined chest ore. Advance the shared epoch and processor revision in the same commit.

**Full inventory trial reset:** preserve the broader existing baseline behavior; include the processor tray's initial empty state and scalar revision/epoch preparation. Keep the existing guard requiring retrieval of Arc refinery socket items before full-trial restore. Registered tray contents may be cleared by this deliberately confirmed broader reset; they are not silently ignored. All participants validate before any installation, so one rejection leaves every store/world owner unchanged.

Animation reset never clears or replenishes the tray. UI close, disable and scene-view reconstruction are not reset commands. Startup baseline adoption must occur only after all economics validate; never capture malformed/used retained state as a new fresh baseline.

## 9. UI and companion status contract

One camp modal owns Inventory-mode input and the existing EventSystem; the overview and station details are views under that owner. Add a “Camp Overview” navigation action to each existing registered camp chest/station view and the new processor view. Opening any of those physical interactions establishes the modal's access anchor; returning to the overview preserves that anchor. This needs no new input binding or rest-menu ownership change. System menu suspension preserves focus/source context and resumes only while site/access/epoch remain valid. Inventory/radial/camp/riding actions cannot acquire competing input ownership. Held submit/click/cancel guards apply on open, resume and after commit/reset confirmation. World time is unchanged and camp is not a safe zone.

- **Items:** four registered stores, one selected store at a time, explicit accessible/read-only status, actual item IDs/counts/mass and legal transfer destinations. Show source/store names, not numeric owner indices. No pooled total can be consumed. The maximum remains the existing 24-slot grid in this slice; larger stores are deferred.
- **Stations:** registered bench, Arc refinery and Ironstone processor with ready/blocked reasons. Selecting a distant station may show read-only status, but does not open an executable remote action. Recipe detail names the actual linked source and all results.
- **Companions:** only the existing Legger, with ID, physical proximity/route state, load and cargo accessibility. “Call Legger to Booter” closes the modal into Gameplay before invoking the existing command; it grants no stock, home task or teleport. Reject while actor absent/disabled, busy under an incompatible owner or mounted. The Lager enemy does not appear as an ally.

Keep the existing `target == null` exact-canister deployment picker outside camp-overview navigation; it has no physical camp anchor. New station/overview entry and station execution use the settled standing-action eligibility guard, with a live enabled Booter and no incompatible riding/rest/waypoint/action owner. Transfers, reset and companion call recheck life/input and their existing activity/access guards; preserve the full inventory trial's current activity contract rather than silently making every inventory action standing-only. Close the modal on health reduction, death, application-focus loss or input-device disconnect; these interruptions do not roll back committed stock or invent a camp safety zone. Status uses actual follower route/ride state and cargo, not the companion snapshot's static `Task = Follow` label. “Call Legger” closes the modal completely, releases only its own input/modal ownership, then rechecks eligibility in Gameplay before invoking `RequestCallOver`; report a request rather than claiming route/arrival success. It does not automatically reopen the modal.

Entering station detail requires physical station access and adopts that station as the modal's access anchor; mutation also requires current source/destination access. Losing the active anchor's physical access, invalidation, actor/input disable or destroyed target closes the appropriate view, releases only UI ownership and leaves stock intact. The waypoint identifies site membership; its coordinate is not a second simultaneous range gate for a chest/station elsewhere in that authored camp. Overview remains accessible only through its current physical interaction anchor.

## 10. Dependency-ordered batches and gates

| Batch | Intent/source boundary | Acceptance and proof | Stop before next batch if… |
| --- | --- | --- | --- |
| 0 | Revalidate main/ownership, current scene/source hashes, pinned Editor, lock/process state and latest riding/optics integration. Snapshot protected files and nine terrain assets/GUIDs. Create a separately hydrated background candidate, including dynamic Resources assets. | Read-only manifest lists task-owned changes, protected assets and active source/scene writers. Current callers/validators are inspected. | Writer handoff is missing; source/pins differ unexplained; candidate lacks dependencies. |
| 1 | Add waypoint-bound site/registry/store descriptors and station adapters; register existing owners. Leave their state/services unchanged. | Registry tests reject duplicate IDs/state aliases, wrong scene/site, malformed bindings and disabled/recovery owners; legacy rest/crafting/refinery behavior remains available through adapters. | Registry would become a second state/location owner or regress existing dispatch. |
| 2 | Add resource-only tray policy/component and versioned retained envelope. Create four product definitions and multi-output recipe; use the same catalog/profile. | Store/snapshot tests preserve stacks/revisions, reject wrong types and corrupted retention, and prove no reseed on re-enable. Asset checks prove IDs, mass and GUID/catalog compatibility. | Invalid retention is replaced with empty state; metadata/catalog authority drifts. |
| 3 | Implement pure processing preparation plus atomic processor/store commit and exact-stack retrieval. | One batch produces exactly all four products; missing input/full tray/stack overflow/access loss/stale recipe or replaced owner/revisions/reentry leave both stores and processor unchanged. Partially fitting retrieval rejects in full. Mass is 5.0 in/out. | Any side product or processor scalar is installed outside the transaction; retrieval silently moves a partial stack. |
| 4 | Extend EconomySession and InventoryTrial to coordinated reset/baseline/ownership participants before enabling processor UI. | Reset tests cover products in every store, filled Arc sockets, malformed participant, retained baseline, repeated reset, unchanged unrelated stock/salvage/vitals/actors and stale epoch rejection. | Any reset path omits the tray or replaces an existing socket guard. |
| 5 | Wire the overview/contextual UI and read-only Legger projection/call command through the existing modal/menu/input path. | UI tests cover keyboard/gamepad navigation, held actions, nested menu resume, source/destination range loss, destroyed target, mount conflicts, disable/re-enable and one EventSystem/modal. Reviewed normal/large-text captures have no clipping. | Stock/UI or motor authorities multiply; unavailable actions execute. |
| 6 | Author owned processor/profile bindings on current native camp terrain in the isolated candidate. Export only validated additive changes under a source-hash guard. | Scene validator proves one processor/tray/site, native support/body clearance/loading access, original content/IDs and no production/build/save changes. Repeated installer validates without changing saved hash. | Source scene changed, clearance fails or any unrelated record would be rewritten. |
| 7 | Reconcile exact installed candidate, regressions, manifests, docs/evidence and scoped commit. | Every success criterion maps to passing focused proof; protected hashes match; task files and source delta match the tested candidate. Self-audit, clear scoped index and local commit. | Failure, unowned staging or unverifiable source/candidate mismatch remains. |

## 11. Verification and proof boundary

No Unity or tests are run by this planning task. During implementation, use the installed pinned executable in a **separate background candidate**, `-WindowStyle Hidden`; never focus a window. Inspect each validator before calling it: existing builders may mutate scenes. New scene validator accepts a Scene without installing/saving; only its isolated CLI wrapper opens the lab.

| Claim | Required evidence |
| --- | --- |
| Immutable definitions and complete recipe | Focused asset/pure EditMode tests for IDs/types, duplicate/missing outputs, quantities/overflow and mass accounting. |
| Exactly-once processing and ownership | Draft/commit tests for all failure phases, stale sources/tray, guarded observer reentry and a second explicit legitimate batch. |
| Retention and recovery | Envelope/InventorySnapshot JSON round trips, stable logical owner identity and one current state/subscription set on reconstruction, new runtime command token after domain reconstruction, malformed data retained/blocked, no fresh grant. |
| Both reset paths are complete | Focused aggregate tests against actual registered stores/scalars and the existing full-trial baseline. One bad participant must prevent all changes. |
| Audited boundary cases | Fresh/retained startup in both execution orders; no grant before preflight; baseline/tray migration failure; same IDs with replaced owner/domain token; recipe edit without version bump; partly fitting exact retrieval; no tray deposits; stale reset confirmation; epoch/revision limit; observer sees all owners installed; damage/death/device/focus interruption; null-target deployment picker remains functional; lab/production Scene isolation. |
| UI and physical access | Focused EditMode input/menu/selection tests, real scene access/clearance checks, and static UI captures at 1920×1080 normal / 1280×720 large text. These do not establish gameplay feel. |
| Existing loop preservation | Affected `CampEconomySessionTests`, `CampLoopLoadoutTests`, `CampCanisterLifecycleTests`, `CampRefineryTests`, `CampCraftingTests`, `CampLoopSalvageTests`, inventory/menu/radial and scene fixtures; include settled riding integration checks where dispatcher/input eligibility changes. Select the current exact fixture list after Batch 0. |
| Production protection | Hash/GUID and structural comparison for GreaterWasteland, source catalog/cache prefab, package/build/editor settings, nine lab TerrainData assets and preserved scene documents. |

Command templates, with candidate/log paths resolved from the actual validation workspace:

```text
Unity.exe -batchmode -nographics -projectPath <isolated-candidate> -runTests -testPlatform EditMode -testFilter <new-and-affected-fixtures> -testResults <results.xml> -logFile <test.log>
Unity.exe -batchmode -nographics -quit -projectPath <isolated-candidate> -executeMethod BooterBigArm.Editor.CampIronstoneAuthoring.ValidateFromCli -logFile <validation.log>
```

The proposed validator does not yet exist. Tests normally exit without `-quit`; executeMethod uses it. Graphics captures omit `-nographics`. Fresh import/compile proof and exact tested-source hashes are required; prior 177-test results cannot prove this new slice. No gameplay smoke, Player performance, application-restart save or production build claim is made. Hands-on acceptance is user-owned.

## 12. Failure containment and recovery

- Missing/duplicate site, member or catalog bindings: disable the new capability with a reason; preserve old stores and retained records. Do not reconstruct a fresh camp or fall back to remote/global stock.
- Capacity/access/revision failure: reject the entire operation; no inputs, byproducts or power changes. Presentation failure after commit cannot turn a committed transaction into a reported rollback.
- Lifecycle/reset failure: preserve all participant snapshots and no partial grants; serialized presence/version must distinguish fresh empty from invalid retained state. Unknown versions require recovery, not reinterpretation.
- Bad scene/export: retain the source and candidate, reject source-hash mismatch and fix only owned additions; never restore the whole source scene from an older copy.
- Concurrent source integration: obtain the settled baseline and rebase task-owned changes sequentially. Preserve existing dirty work, all metadata, riding dispatch and animation receiver.
- Proposed materials without final uses: keep this an explicitly bounded processing study. Do not grow the catalog or silently change live battery/salvage recipes to force progression.

## 13. Subsequent milestones, separate from first-slice completion

1. **Cutting and material uses:** add worked stone blank (proposed mass 1.5) and recipe `2 raw ore → 1 blank + 1 aggregate + 1 mineral dust`, material mass 2.5. Specify a few consuming crafting recipes and recipe selection UI before implementation; no new ore grade/quality payload by default.
2. **Powered metal stage:** add iron feedstock (0.75) and slag (0.5), an explicit battery port and recipe `1 concentrate → 2 iron feedstock + 1 slag`, with one charged battery becoming empty in that same port atomically. Final energy cost and material form require the author's direction for this milestone. Add power ownership/reset/snapshots before UI; do not retrofit fractional charge or substitute mineral dust for Arc Dust.
3. **One companion work action:** choose hauling or station operation, then define actor intent/executor, target IDs, reservations, physical arrival, recall/mount/danger cancellation and blocked-route recovery. Retain one motor owner and real cargo transfers.
4. **Production camp aggregate:** version all sites/stores/sockets/jobs/roster deltas, migrate authored checkpoints and prove failure/restart recovery before promoting any new mechanic to Greater Wasteland. No independent generated-world save owner.

## 14. Completion and next authorized move

The first-slice plan is ready for review and a later explicit implementation instruction, using the stated reversible lab defaults. It is complete only when the identified camp, four physical stores, three stations, one sorting batch, management/Legger views, retained tray and both reset paths meet the proof matrix with old systems protected. Finish with evidence and a task-owned local commit; do not continue automatically into the subsequent milestones.

Creating and auditing this plan provides source inspection, dependency ordering and paper/document validation only. It does not prove new Unity compilation, imports, tests, scene appearance, performance or persistence. This turn delivers the audited plan; feature implementation starts only upon a further request.

## 15. Plan audit — October 14, 2026

Audited against live InventoryState/Transaction/PreparedWorldMutation, InventoryPolicy, TransferService.Commands, EconomySession, InventoryTrial, WaypointCampController/Settings, CampWorkstationUiController, PlayerActionController.CampWorkstations and BigArmState/Follower at `2aa8d1cb1`. Riding, animation and optics edits remain concurrent and unowned by this task. This is a source/design audit, with no Unity launch or executed gameplay tests.

| Finding | Severity and evidence | Revision/disposition |
| --- | --- | --- |
| “Full stack” retrieval assumed the old helper was exact. | High: `TransferSlot` calculates `GetMaxAddable` and can move only part. | Add an explicit exact-stack prepared entry point, preserve legacy defaults, and test partial-capacity rejection. |
| Startup/baseline ordering was descriptive rather than gated. | High: EconomySession grants before trial adoption; InventoryTrial can initialize independently on lab readiness. | Preflight new owners before fresh grants; coordinate baseline/reset readiness and retain old baselines without recapturing used stock. |
| Atomic reset composition did not state how inventories participate. | High: InventoryTrial owns three drafts and one external world callback. Nesting a new store commit inside that callback would split the transaction. | Return deduplicated inventory and world participant lists; one commit, all blockers retained, stale confirmation rejected. |
| ID/version/revision checks did not cover owner replacement or asset editing. | High: InventorySnapshot omits live Revision; mutable recipe assets need not bump their version. | Runtime session token/generation, reference matching and immutable content fingerprint; reject stale quotes. |
| Atomicity overclaimed protection against Install exceptions. | High: Commit installs inventory drafts before world Install delegates, without rollback. | No-throw, prevalidated field-only Install contract; failure paths occur before installation and observer failures remain post-commit. |
| New owner policy and tray destination dispatch were underspecified. | Medium: unknown owner currently falls through to BigArm; generic transfer views can otherwise deposit into any store. | Explicit resource-only enum branch and caller audit; output-only tray destination and preserved null-target deployment picker. |
| Retention did not distinguish fresh, invalid or reconstructed state precisely. | Medium: inventory revisions are runtime-only; baseline and economy epoch are independent existing owners. | Explicit version/presence/identity envelope, consistent capture outside locks, one epoch authority, fresh runtime command token. |
| Camp identity, actor membership and companion freshness needed bounds. | Medium: both scenes can use `camp.starting`; cargo moves; companion Task is static while follower state changes. | Scene-local registry, actor-owned projections, localized availability and bounded visible status refresh; life/ride/interrupt checks. |

**Audit outcome:** the architecture remains appropriate for the bounded lab slice after these revisions. Product yields and later metal/power costs remain proposals. The implementation must first revalidate the settled concurrent source/scene baseline, then satisfy the expanded proof matrix; this audit is not authorization or proof of implementation.
