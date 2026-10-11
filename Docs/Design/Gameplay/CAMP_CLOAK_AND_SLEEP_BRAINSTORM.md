# Camp cloak and sleep prototype proposal

Brainstorming pass dated October 10, 2026. Booter carries a compact vertical shade, deploys it through the radial menu, and sleeps on its shaded side using his cloak as a blanket. Sleep restores physical Reserve while consuming nourishment, hydration, and oxygen. The world continues visibly at accelerated speed, including threats and the existing twilight sun.

The user has established this direction. Dimensions, rates, controls, visual details, and component names below are provisional prototype choices. This pass specifies the meshes and systems; it does not create meshes or change runtime behavior. It serves the fixed, always-loaded Greater Wasteland production lane. A second brainstorming pass on October 10 refines sleep around short, satisfying breaks with occasional placement decisions and manageable costs. Balance remains a hypothesis until player testing.

## Existing foundations

- [World basis](../../WORLD_BASIS.md) establishes perpetual twilight, low-angle sunlight, algae-based survival, vulnerability, and the Legger's physical presence. A portable camp grants no safe-zone status or automatic companion protection.
- [Survival standard](./SURVIVAL_SYSTEM_STANDARD.md) defines Reserve as long-term physical readiness, explicitly intended to recover through sleep. Use this existing meter first. Separate sleepiness or multiple tiredness meters can follow if their distinct purpose becomes clear.
- [Survival vitals](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Survival/TopDown3DSurvivalVitals.cs) advances hunger, thirst, and Reserve using scaled elapsed time. Oxygen exists but has no automatic drain. Snapshot version 2 preserves all five values; a snapshot API alone does not establish production world saving.
- [Authored survival settings](../../../Assets/_Project/Settings/Survival/Resources/TopDown3DSurvivalSettings.asset) uses 100-point capacities, nourishment loss of 0.025 points per simulation second, hydration loss of 0.04, and ordinary Reserve loss of 0.01. These are prototype balance values.
- [Radial state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DRadialState.cs) provides customizable pages and serialized command values. Camp can become another command through the existing dispatch path.
- [Twilight sun](../../../Assets/_Project/Scripts/Runtime/TopDown3D/PerpetualTwilightSun.cs) already advances with scaled elapsed time and exposes a direction to the sun. Its path uses elevation and azimuth oscillation rather than a literal figure eight. Retain it for this prototype; inspect the production scene's enabled state and authored duration during implementation.

## Camp appearance and mesh brief

Build a folding vertical fabric screen with two slender metal uprights, a folding spreader, small foot plates, and short bracing straps. The shade should read as a survivor's carried tool: restrained rusted hardware, dusty opaque fabric, broad folds, and reinforced corners. Keep its silhouette readable from the elevated gameplay camera. No fire, furniture, powered enclosure, or resource-producing apparatus is needed.

Proposed starting dimensions are 2.2 metres wide and 1.3 metres tall, with a sleeping footprint roughly 2.1 by 0.85 metres behind it. These must be fitted to Booter's actual imported scale. Keep the bottom edge near the ground so low sunlight cannot shine beneath it. At a hypothetical 10-degree solar elevation, a 1.3-metre screen casts roughly 7.4 metres of shadow on flat ground; terrain, light direction, and the current sun elevation determine actual coverage.

Start with a slightly bowed rectangular screen and a shallow angled return at each end. The returns help cover a modest sun-direction change without creating a full tent. Face the screen toward the horizontal projection of the live sun direction and put the sleeping spot behind it. Allow rotation before placement. Once deployed, it stays where Booter put it; changing shadows can later affect comfort without automatically rotating the camp.

| Mesh | Prototype construction | Starting triangle budget |
| --- | --- | --- |
| Deployed shade | Separate fabric, uprights, spreader, feet, and braces; broad silhouette folds | 1,500 to 3,000 total |
| Packed camp | Narrow folded screen and bundled poles, strapped together | 300 to 700 |
| Worn cloak | Shoulder yoke, draped back, open front, split lower hem | 1,000 to 2,000 |
| Sleeping cloak | Broad blanket drape following a reclining body, head exposed | 600 to 1,200 |
| Rolled cloak | Compact folded representation if needed for unequipped presentation | 200 to 400 |

Use one fabric material and one hardware material where possible. Give cloth enough thickness or a suitable two-sided material to remain visible from both sides. Use simplified collision on supports and placement volumes, with no dynamic fabric collision. Preserve a clear sleeping body silhouette and escape route behind the screen.

Blender sources belong in a new local study under `SourceArt/Blender/Studies/CampAndCloak/`. Follow the [local-only source policy](../../Operations/LOCAL_ONLY_SOURCE_POLICY.md): preserve `.blend` sources and backups locally. Proposed Unity exports belong under `Assets/_Project/Art/Environment/Camp/` and `Assets/_Project/Art/Characters/Booter/Cloak/`; prefabs belong in their established project-owned prefab folders. Native sources and Unity exports have separate identities and lifecycle requirements.

## Cloak behavior

Treat the cloak as one carried possession with worn and blanket presentations. Sleeping switches its presentation onto Booter's resting body; waking restores the worn cloak. The switch must never duplicate an item or leave both meshes visible. The first prototype can provide a starter cloak without introducing a full equipment or crafting economy.

Fit the worn mesh to the current player rig. Begin with a simple weighted cloak or a shoulder-attached blockout, then inspect walking, turning, crouching, climbing, and traversal for clipping. Keep the hem above the feet, expose the hands, and leave room for carried equipment. Use a posed blanket mesh for sleeping rather than attempting to solve dynamic cloth immediately. Small shader motion can later suggest wind; cloth simulation, garment damage, dyeing, and thermal statistics are separate experiments.

The cloak provides the blanket visual and a possible comfort modifier. It does not create food, water, oxygen, healing, armour, or hazard immunity. Whether sleep without a cloak is allowed remains open; the first prototype assumes the starter cloak is present.

## Player flow

1. Select **Set Up Camp** from the radial menu. Close the radial and enter a placement preview without pausing simulation.
2. Show the screen and sleeping footprint together. Default to a sun-facing orientation, allow adjustment, and report concrete placement failures.
3. Confirm on reachable ground with sufficient support, acceptable slope, clear body space, and clear standing space. Revalidate at confirmation so moving objects cannot invalidate an old preview. Cancel creates nothing and spends nothing.
4. Play a brief setup action at normal speed. Validate proximity and interruption before consuming or reserving the carried kit, then create one deployed camp. Prevent repeated confirm input from creating duplicates.
5. Physically approach the camp and choose **Rest**, **Sleep**, or **Pack Up**. Keep interaction short and show Reserve and resource costs without adding an inventory screen.
6. Booter sits for rest or lies down under the cloak for sleep. The camera keeps him and the surrounding approach space visible. Wake is always available through a live binding prompt.
7. Waking restores normal world speed before the standing animation. The camp stays deployed until packed; fleeing does not teleport it back into inventory.

One deployed camp per starter kit is enough. If the kit is already deployed, the radial should report that fact and allow a local camp interaction when in reach. It must not pack, move, or retrieve a distant camp remotely. The Legger continues his current physical behavior; autonomous guarding is a later explicit mechanic.

## Rest and sleep tuning

The first pass's 75-second nap and roughly four-minute full recovery would spend too much real time watching a repeated survival action. Replace that candidate with a default sleep action lasting roughly 25 to 50 real seconds when Reserve is meaningfully depleted. Setup and packing should each take about two seconds at normal speed, with a short reclining and standing transition. These are feel targets, not reasons to skip simulation or hide hazards.

Use simulation seconds consistently until the project defines a shared calendar. Do not label these intervals as world hours yet. The following are deliberately provisional net rates for 100-point meters.

| Mode | Reserve change per simulation second | Nourishment loss | Hydration loss | Oxygen loss | World speed |
| --- | --- | --- | --- | --- | --- |
| Ordinary activity today | -0.01, with existing exertion cost | 0.025 | 0.04 | 0 today | 1x |
| Deliberate camp rest proposal | +0.300, up to 40 Reserve | 0.020 | 0.032 | 0.008 | 1x |
| Camp sleep proposal | +0.400 | 0.015 | 0.028 | 0.010 | 4x |

These are game balance hypotheses, not physiological claims. Sleep lowers food and water use relative to active travel, while breathing still spends oxygen. Reserve recovery replaces its ordinary depletion while resting or sleeping; adding recovery on top of depletion would produce different net rates. Oxygen loss is initially limited to explicit camp rest/sleep modes; general breathing, equipment supply, and environmental oxygen remain their own future design slice.

Sleeping from 20 to the default target of 80 Reserve takes 150 simulation seconds or 37.5 real seconds at 4x. It spends 2.25 nourishment, 4.2 hydration, and 1.5 oxygen. From empty to 80 takes 50 real seconds; an optional empty-to-full recovery takes 62.5 seconds and spends 3.75 nourishment, 7 hydration, and 2.5 oxygen. These modest costs are a starting point: sleep should support an expedition rather than require another supply run after every camp.

Deliberate camp rest would introduce partial recovery beyond the current sleep-only Reserve standard. Keep that change explicitly scoped to a camp interaction; standing still and waiting continue to drain Reserve. Rest is an awake emergency breather: a 20-second action adds at most 6 Reserve, capped at 40, spending 0.4 nourishment, 0.64 hydration, and 0.16 oxygen. At or above that ceiling, explain that sleep is needed rather than allowing a costly action with no recovery. The ceiling makes sleep the useful option for sustained recovery while rest can help a vulnerable player continue to a better location. Remove Rest from the first implementation if it adds menu clutter without a distinct use.

Use one primary action, **Sleep until ready**, with an 80-percent Reserve target and manual wake. An optional **Sleep fully** choice can live behind a secondary input if testing shows a reason for it. Avoid hour selectors and separate nap durations in the first prototype. Show estimated real duration, expected Reserve, and rounded supply costs together before confirmation. At or above 80, explain that Booter is already ready and expose full recovery only if useful. Reaching the target, cancellation, a critical resource threshold, or danger ends acceleration. Do not refill all survival meters, heal automatically, or skip the resource cost on interruption.

## What makes sleeping enjoyable

The enjoyable decision happens before lying down: notice a useful lee side of a rock, position the screen against the low sun, and make a temporary little refuge. Good preparation should produce a visibly calmer resting place and a slightly quicker recovery. The reward is being ready for the next stretch of travel, with the camp giving Booter a physical presence in the landscape.

Keep placement forgiving. Auto-orient the screen, choose a nearby supported footprint, and allow one adjustment rather than asking the player to align several separate objects. Show **Good shade** or **Exposed** beside the preview; validity and comfort are separate. An acceptable exposed location still allows useful sleep. A brief warning about visible threats or insufficient supplies belongs at confirmation, without a second confirmation chain.

For a later comfort experiment, good shade can raise Reserve recovery by about 15 percent while keeping resource drain tied to elapsed simulation time. It changes the 20-to-80 example from 37.5 to about 32.6 real seconds. There is no shelter checklist, perfect-score requirement, or harsher baseline imposed to make the bonus necessary. Use one comfort result rather than separate warmth, softness, wind, blanket, and shade meters. The starter cloak supplies the expected blanket; it is not another recurring maintenance task.

Avoid shadow micromanagement. Evaluate meaningful shade with stable sampling and a tolerance interval; a briefly shifting shadow should not flicker the UI or repeatedly wake Booter. Comfort can ease toward its new recovery rate, preserving accrued progress. Shade never proves the absence of enemies, electrical danger, or other hazards.

During sleep, keep the body and cloak visible, pull the camera back slightly if needed, and let shadows, the Legger, wind, and dust show that the world is moving. A compact indication such as **Sleeping 4x · Reserve 54 → 80 · Wake** is enough. Audio should preserve threat cues rather than speeding every sound into noise or muffling danger. No dream screen, repeated button press, timing minigame, or compulsory inventory organization is needed.

Make wake-up satisfying and usable: acceleration ends, Booter sits up, gathers the cloak, and stands. Grant control as soon as the standing collision space is valid. Ordinary wake can be calm; a danger wake must be brief enough to respond and must not trap the player in a decorative animation. Packing is one local action that returns the same kit; it needs no consumable fuel or repair chore in this prototype.

## Balance without a repeated chore

Sleep is a player-chosen expedition break, not a scheduled bedtime. Start with the current ordinary and exertion depletion rates. A provisional pacing goal is a meaningful camp opportunity every 40 to 90 minutes of mixed active play, with longer trips between sleeps during lighter activity. Tune against actual depletion and route length; do not increase fatigue simply to make the new feature appear more often. The existing rates consume 36 Reserve per simulation hour ordinarily and 90 during continuous exertion, so activity mix matters.

An 80-percent target should be enough to set off confidently. Repeated short sleeps offer no special bonus, free supplies, or buff that encourages topping up after every encounter. Recovery and costs accrue continuously, and setup/standing time is still world time. Stop recovery at its cap, retain partial recovery after interruption, and never charge a fixed full-night fee for a brief attempt. Full recovery earns only its additional Reserve in this prototype; it does not create a second rested-buff timer to maintain.

Reserve depletion should first communicate planning pressure through Booter's presentation and the meter. Preserve the current ability to move and climb when empty. Any later tiredness penalty must leave a dependable route to camp and avoid surprise collapse, involuntary sleep, or disabling the controls needed to escape. Sleep remains valuable because it replenishes a visible reserve, without making a depleted player unable to recover.

Before sleep, predict how far the available supplies can safely carry the action. If the chosen target would cross a critical threshold, offer a shorter achievable sleep with the resulting Reserve shown; reject only when there is no safe recovery interval. A low-supply player should be able to make a useful partial recovery without being stranded by an all-or-nothing target. Recheck live values during sleep and preserve the existing automatic wake safeguards.

Risk should follow the actual location and simulation. Camping in an exposed patrol route is dangerous; scouting a quiet spot should usually pay off. Do not spawn an ambush merely because Sleep was selected, guarantee an interruption per camp, or reset recovery when attacked. If enemies frequently approach every valid spot, fix encounter pacing or placement guidance rather than granting sleep immunity. An interrupted player can choose to flee and leave the reusable camp behind.

The first player comparison should answer whether placing and packing feels like a small ritual, whether 25 to 50 seconds stays pleasant on repeated use, whether supply losses are noticeable without forcing immediate resupply, and whether danger allows a fair wake-and-react opportunity. Review camps per active hour, total time spent in camp actions, early manual wakes, reasons for interruption, and repeated attempts needed to reach the target. These are evaluation criteria, not current proof or authorization for automated gameplay smoke testing.

If sleep feels slow, increase recovery at the supported acceleration before pushing physics to extreme speeds. If it feels compulsory too often, adjust ordinary or exertion depletion after inspecting activity patterns. If it feels free, tune supply rates modestly before adding fees, consumable bedding, or penalties. If players chase 100 percent after every encounter, keep 80 as the prominent target and avoid full-meter bonuses. Change one balance lever at a time so the cause of improvement is clear.

## Accelerated simulation and interruption

The central behavior is visible continuous simulation. Booter remains in the world, can receive damage, and remains detectable and reachable. Sleep disables intentional locomotion and tool use while preserving collision, damage reception, and the surrounding simulation. Pose the visual rig separately from the collision authority so reclining does not disable the player collider or force his body through the terrain.

Prototype a single owner for world acceleration with a releasable sleep request. Begin at 4x; only try higher speeds after relevant simulation and performance checks. Existing scaled-time systems should receive accelerated elapsed time once. Avoid both accelerating time and independently multiplying survival rates, which would charge the player twice.

Keep ordinary fixed simulation steps initially to preserve collision resolution, accepting that acceleration requires more steps per real second. Measure the cost and clamp supported acceleration rather than silently enlarging the physics step or discarding dangerous events. Audit movement, companion navigation, AI, attack cooldowns, animation, particles, weather, and timers as each becomes relevant. Systems using wall-clock time need explicit classification; menus and wake input should remain responsive in real time. A global speed change alone is not proof that every system accelerates correctly.

Wake on a player command, received damage, a nearby credible threat signal, camp invalidation, death, or a critical food/water/oxygen threshold. A proposed threshold is 10 percent, with a further check that the expected stand-up cost cannot exhaust the remaining resource. Threat detection must connect to actual encounter authorities when present; do not invent an enemy system to finish camping. Always provide a damage-event interruption seam.

On danger, release acceleration immediately before standing up and before further sleep recovery. Damage already received remains applied. Detection should offer a reaction opportunity without granting immunity or suppressing the attack. Check interruptions during simulation steps so several accelerated attacks cannot occur before an end-of-frame wake check. Integrate recovery only up to the interruption time.

Every exit path must release the request: normal wake, cancellation, target reached, death, destruction, scene change, and failed setup. Compose with any later pause owner rather than unconditionally forcing `Time.timeScale = 1`. Resume from an interrupted session at ordinary speed; acceleration is transient state, never a saved preference. Background application suspension does not imply offline sleep progress.

The existing twilight sun should follow the same elapsed simulation time. The revised 20-to-80 sleep advances 150 simulation seconds, one eighth of its default 1,200-second cycle. The first visual check must compare this interval with the production scene's actual cycle. Tune recovery before making a separate sun clock; literal figure-eight astronomy is outside this prototype.

## World and persistence contract

| Concern | Proposed contract |
| --- | --- |
| Deterministic world identity | Camp placement is a player-authored runtime delta belonging to a specific world/save identity. It must not change terrain identity, terrain hashes, or procedural dressing seeds. |
| Chunk streaming and unload/reload | Greater Wasteland terrain is fixed and always loaded, so terrain streaming is not applicable. Dressing-cell visibility may hide distant visuals but must not delete camp state. Future object unload/reload reconstructs the same camp once. |
| Stable generated-object identity | Camps are placed rather than procedurally generated. Assign a durable instance ID once at successful deployment and reuse it across save/reload; never derive it from scene instance IDs or rounded position. |
| Authored constraints | Respect terrain support, slope, collision, landmarks, protected placement areas, access routes, and the unchanged native terrain. A shade provides a shadow, not a universal shelter bonus or secure zone. |
| Persisted runtime deltas | A later versioned record stores camp ID, world ID, transform, kit ownership/state, cloak state, player vitals, and shared clock state when available. Save packing/removal explicitly so reload cannot resurrect a packed camp. Never save acceleration as active. |

Start with a session-only prototype and disclose that deployed camps do not survive reload until the separate saved-world integration slice is implemented. Do not install the archived generated-world save service. Keep placement, kit accounting, and state changes atomic so cancellation, repeated input, or interruption cannot duplicate possessions or reset vitals.

## Small implementation slices

1. **Mesh blockout:** author deployed and packed screen meshes plus worn and blanket cloak meshes. Inspect silhouette from front, side, top, and gameplay camera; verify normals, scale, pivots, materials, and Unity import. Keep the study separate from the production scene until handoff.
2. **Camp placement:** add an append-only radial command value and command adapter, placement preview, one kit, terrain/contact checks, local interaction, and pack-up. Preserve customized radial pages and existing numeric command meanings; migrate preferences explicitly if defaults change.
3. **Rest and sleep state:** add camp action states, sitting/reclining/standing poses, cloak presentation switching, and mode-aware survival advancement through the existing vitals authority. Keep all tuning editable and the save snapshot contract compatible.
4. **Live acceleration:** add the shared acceleration owner, responsive wake input, continuous sun movement, danger/resource interrupts, and cleanup. Runtime code remains in `BooterBigArm.TopDown3D.Runtime`; editor helpers remain isolated editor code.
5. **Saved-world integration:** only after the session loop works, add versioned camp and clock records with duplicate prevention and reload proof.

Relevant automated checks for implementation are placement rejection and confirm revalidation; one-kit accounting; existing radial preference compatibility; worn/blanket exclusivity; equal survival outcomes for equal simulation duration at different supported speeds; interruption ordering and partial recovery; low-resource wake; acceleration release on every exit; and future stable-ID save round trips. Unity import and focused structural checks accompany asset handoff. Hands-on gameplay smoke testing remains user-owned.

## Open creative choices

The first visual candidate is a bowed metal-framed screen with small returns and a dusty warm-grey cloak. The strongest comparisons are a folding rigid frame versus a tensioned fabric panel, and a long split cloak versus a shorter shoulder wrap. Keep these as candidates until the author sees them on Booter at gameplay scale.

After the basic loop, the useful design questions are whether the small shade bonus makes placement satisfying, whether storms interrupt sleep, whether sleeping without the cloak is possible, whether awake Rest earns its extra action, and whether Reserve alone communicates tiredness adequately. These can be explored without adding camp upgrades, automatic guarding, cloth simulation, or a full survival redesign to the first prototype.
