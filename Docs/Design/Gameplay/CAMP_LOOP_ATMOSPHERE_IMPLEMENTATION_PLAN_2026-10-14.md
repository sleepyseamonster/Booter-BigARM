# Camp Loop atmosphere implementation plan

October 14, 2026. Revision 2 incorporates the chat plan audit and current implementation authorization. Source: the user's request to implement the audited plan. This is the execution contract for this goal; the [lighting audit](CAMP_LOOP_LIGHTING_CAMERA_AUDIT_2026-10-14.md) supplies the initial evidence.

## Outcome and ownership

Build a bounded camp presentation study: representative Death Valley basin geometry beneath red ochre and orange dust, with a separately controlled deep yellow sun. Preserve the existing sun cycle, gameplay camera input/lens/framing, native terrain and gameplay state. Implement structural camera-clearance corrections separately. Gottspan integrates; Babineaux owns background Unity proof; Gear Ball commits only verified task-owned changes. The author accepts appearance and feel.

No production promotion, package upgrades, terrain expansion, generated terrain, gameplay smoke tests, save changes or external publication. Static captures and focused EditMode tests run in a fresh isolated candidate. Existing dirty animation/inventory/workstation/NPC files are excluded from that candidate unless needed and explicitly reconciled.

## Current boundary

The saved production scene has 9216 native tiles across X −22528 to 2048 and Z −2048 to 22528. Booter's camp anchor is approximately X −1848, Z −1498, only 550 m from the southern footprint boundary. Thus a complete photographic 360 degree landscape cannot claim verified geographical continuity. Reconstructed surrounding ridges can supply references and feasibility proof; generated background continuation remains artistic and must pass a bounded movement comparison.

Unity 6000.4.0f1 and URP 17.4.0 remain pinned. Shared panorama/material/renderer/default-volume assets retain their GUIDs and existing production contracts. New camp sky assets use sibling paths. One camp scene binding selects presentation data; it does not become another sun clock or fog owner. PerpetualTwilightSun remains the lighting/sky lifecycle authority. Camera-scoped feature parameters must not mutate the shared renderer assets. RenderSettings follows active-scene semantics; additive/preview ownership and teardown require explicit proof.

## Batches and gates

0. Freeze a committed candidate and protected-source hashes. Exclude unrelated dirty files; retain required private animation dependencies locally. Recheck locks and active processes before every Unity launch. Do not use a competing Editor on the checkout.
1. Reconstruct coarse diagnostic context from existing native TerrainData without changing those assets. Capture stable camp/station views and cardinal ridge silhouettes. Compare a fixed backdrop over the bounded camp envelope, including scouting offsets and twilight extremes. Record missing-direction geography. Reject fixed detailed ground if sliding or edges are unacceptable; retain real terrain and use diffuse sky/horizon treatment instead.
2. Generate three sunless atmosphere variants using the same terrain reference and composition: clearer, moderate ochre haze and deeper twilight. Validate image projection/seam/poles before asset acceptance. Show concrete comparisons for author selection; the author selected Clearer twilight on October 14. Clear.asset anchors the installed camp study; overall scene appearance remains a hands-on acceptance step.
3. Add a sibling spherical sky shader/material and a camp-scoped profile/binding. World-aligned landscape stays fixed; analytic yellow disc/aureole follows the existing sun. Give below-horizon background a deliberate smooth treatment. Keep the existing Resource panorama and its tests intact. Treat generated PNG as display artwork; do not claim measured HDRI. HDR lighting data is conditional on a demonstrated reflection requirement.
4. Integrate optical controls without global asset writes. Resolve scene binding once through a bounded registry; reject duplicate active bindings. Preserve baseline rendering for scenes without the binding. Snapshot and restore only owned lighting/sky state on enable/disable/destruction and preview capture. Test repeated enable/disable, scene transitions and two-scene isolation. Static captures explicitly apply the same presentation path as runtime.
5. Evaluate direct/ambient lighting, shadows and material readability first; add optical effects one at a time. Keep global dust disabled. Check built-in post-processing separately from custom renderer passes. Record resolution, quality, profile, sun phase and render settings for each capture. GPU frame time is not inferred from render/readback duration.
6. Correct camera clearance during snaps, initial-overlap cases, saturated casts and closer-than-minimum blockers. Preserve orbit, pitch, FOV, look-ahead and damping. Use bounded along-boom recovery; no invented off-axis framing. Expose unresolved fully enclosed cases honestly. Parent integration runs camera fixtures and existing foundation regressions.
7. Run pinned import/compilation, focused profile/sky/optics/distance/camera tests, shader diagnostics and matched static captures. Preserve unrelated scene bytes and terrain references during integration. Commit only verified task changes. Provide author visual/feel acceptance handoff and measured performance limitations.

## Proof and completion

Each success claim needs matching evidence: source hashes for preservation; Unity diagnostics for compilation; EditMode fixtures for ownership/lifecycle/camera rules; static render captures for spherical mapping, yellow sun and ridge alignment; named target-hardware profiling for GPU budgets. Existing PerpetualTwilightSunTests, SceneRenderingOverrideTests, TopDown3DViewDistanceRenderingTests, TopDown3DTiltShiftTests, TopDown3DRoundLensFlareTests, TopDown3DSunBloomTests and TopDown3DFoundationTests remain relevant. New focused tests cover the study's sibling assets and failure boundaries without relaxing production tests.

The terrain remains fixed and always loaded. Presentation changes introduce no generated-object identities, streaming, unload/reload or saved-world deltas. Existing deterministic dust identities remain untouched. Sun direction/timing used by camp shade remains authoritative and unchanged. New materials/settings carry stable Unity GUIDs.

Stop a dependent batch for failed structural/compile proof, concurrent changes to an owned file, missing-source geography, unresolved creative selection, or a required broad project-setting/production-promotion decision. Continue independent authorized work. Goal completion requires a verified integrated study and concrete evidence/handoff; rendered appearance, player feel and hardware budgets cannot be reported as accepted merely because automated checks pass.

## Implemented checkpoint

Clearer twilight is installed in CampLoopTesting. The study uses spherical sunless artwork, an independent yellow sun, restored ambient/reflection ownership and camera-local optical settings. Real render-only native-height context replaces the missing surrounding camp geometry; nine playable terrain tiles are preserved. Camera clearance was committed separately. The [implementation receipt and visual handoff](../../Evidence/Rendering/CampAtmosphere20261014/README.md) records 43 focused checks, two view-distance render checks, the repeated 12-check asset/lifecycle gate, protected-source hashes and 24 static captures. GPU samples were unavailable; appearance, camera feel and Player frame-time acceptance remain author-owned.
