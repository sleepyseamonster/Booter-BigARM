# Terrain Cliff, Valley, and Debris Plan

Status: the connected bedrock-face and sparse Rock Workbench runtime path are implemented;
one terraced canyon topology, fallen talus, and floor-material response remain in an
isolated Unity mirror. The mirror has deterministic, collision, adjacent-vertex,
and full-content Development Player evidence. The reference-image composition, complete
near/middle/far silhouette, continuous hands-on traversal, and topology/save compatibility
are still open. Production terrain heights, first-area canyon policy, and save authority
have not changed.

Connected-bedrock checkpoint, 2026-09-28: the disconnected three-to-seven-stone clusters
shown in the user's screenshot have been removed from near-chunk cliff decoration. Sustained
steep candidates now link only when their facing, tangent and rim/toe elevations agree; a
reciprocal neighbor rule prevents branch-like joins. Each absolute owner emits a closed,
faceted bedrock span whose buried back, crown, stepped front and buried toe share exact
endpoint samples with adjacent spans. Runtime LOD0 and LOD1 meshes are chunk-owned; the
low-detail closed mesh supplies static collision. The same light-gray rock material and
surface settings apply. This is the first structural face proof, with no source-stone
buttresses or talus yet. The canonical heightfield is unchanged, so sparse true-vertical
landforms, upper/lower terrain cuts and matching distance representation remain the next
separate topology step. Focused EditMode checks establish mesh endpoints, outward normal,
LOD reduction and deterministic rebuild/rebase; appearance and contact remain unaccepted
until an in-game view and traversal review.

## Next implementation slice: one generated layered canyon

**Implementation status, 2026-09-28:** the absolute parent-face planner, stepped closed
bedrock mesh, and sparse Rock Workbench buttress/toe pass are implemented. The generated
canyon fixture and focused tests are recorded in
[the canyon implementation receipt](Evidence/WorldCreator/CanyonSlice/README.md).
Fixed-camera visual review, full-footprint terrain contact, streaming-ring proof, and any
topology/save cutover remain open gates. A full-content Development Player route has been
profiled, with arrival hitches still requiring a budget and attribution. This section makes the
remaining slice executable. It extends the
connected-bedrock checkpoint and the generation contract below. Earlier rejected rock
clusters, flat stickers, and generic broad-rock walls are evidence, not fallback paths.

The [canyon morphology investigation](Evidence/WorldCreator/CanyonSlice/README.md)
found that the current 55 m wide, 80 m deep generated cut has no safe talus apron. Its
`CanyonShelfProfile` values are generated but not consumed by the excavation query. A
mirror-only 195 m wide cut using those shelf values creates a more useful terrain-driven
terrace candidate, but the 20-chunk proof is too repetitive and unprofiled. Refine the
parent landform and its material/distance treatment in the mirror before a topology
version or saved-place cutover. Keep the user's dirty query/profile/scene edits intact.
The refined mirror pass reduced the Workbench placements to 66, proved center contact
within 0.14 m and raycastable face collision, and showed the terraces flatten in an
18 m-sampled far view. The [receipt](Evidence/WorldCreator/CanyonSlice/README.md)
preserves the proof code, patches, images, and XML; a real representation and streaming
comparison remains required before cutover.
The actual representation compiler agrees at adjacent near and middle boundaries and at
sampled cross-tier vertices: 25 near edge vertices, 17 middle edge vertices, five near/middle
and two near/far shared points passed a mirror-only geometry/material check. The broad cut
persists in actual middle/far renders, but the middle tier is visibly angular and the far
tier smooths away most shelves. Coincident samples do not establish a continuous silhouette.
An isolated 33×33 far-tier trial reduced sampled cross-section height error and restored
several visible shelves, but used four times the far vertices and tripled warmed tile-build
time. Two full-content Player routes drained pending work eventually but varied enough to
preclude a causal frame-rate claim. Every far tile in the fixture contained canyon semantics,
so whole-tile detail selection offers no reduction. Retain the current 17×17 profile while
developing feature-local shelf detail and a controlled rendered Player comparison; see the
[far-tier receipt](Evidence/WorldCreator/CanyonSlice/README.md).
A mirror-only curvature-local far mesh now demonstrates that direction: 96 selected cells
use 766 vertices on the measured tile and reduce mean cross-section height error from
0.792 m to 0.405 m, with matching far-tile borders and rebase-stable vertex/color output.
The isolated mirror now integrates that mesh after a 17×17 far fallback, using one
background upgrade at a time and per-cell coverage ranges. A focused lifecycle test
proved deterministic unload/reload and coverage restoration; the final focused
performance suite passed 10 of 10. Three rendered full-content Player routes reached
49/49 upgraded far tiles, but first-arrival spikes varied sharply and route centers
did not match the earlier baseline. The [streaming receipt](Evidence/WorldCreator/CanyonSlice/README.md)
preserves the mirror code, tests, and raw telemetry. Keep the runtime integration
mirror-only until a same-center performance comparison, visible swap review, and
production-camera visual acceptance support a cutover.
Rendered Player captures at the first arrival and after all 49 far upgrades show that
the actual 25 m game camera sees a saturated red floor and very dark walls, not the
reference's open mesa view. The settled player position also differed from the route
driver's requested coordinate, so the next controlled benchmark must log and hold the
actual absolute player position. See the [game-camera receipt](Evidence/WorldCreator/CanyonSlice/README.md)
before judging material or camera changes.
A corrected temporary Player driver now waits for the initial-terrain motor restore before
locking the benchmark player; its five settled waypoints hold their exact requested
horizontal positions and the intended `23/25/27/25/23` chunk centers. Those views confirm
the red floor and dark walls. At the farther waypoint, a foreground Workbench stone
nearly fills the camera after the obstruction solver compresses the camera distance.
Treat camera legibility, material response, and formation placement as visible gates,
not as inferred successes from the geometry or frame telemetry.
A same-binary rendered comparison with the corrected route held `23/25/27/25/23`
centers in both modes. The 17×17 control stayed at 0/49 adaptive far tiles; the
candidate reached 49/49 by stop one, and neither retained near/decoration queues at
later stops. First-arrival p99 varied from 483 ms in the control to 50 ms in the
candidate, so one paired run does not establish a causal performance gain. Keep the
adaptive scheduler and all visual/topology changes in the mirror pending repeatable
frame attribution and the camera/material corrections above.
The mirror's next material candidate lowers canyon-floor sand deposition and raises
gravel/shale exposure. Seven focused material tests pass, and the fixed-camera and
rendered Player images lose the vivid red mottling, though the floor is now too smooth
and dark. The obstructing far-waypoint object was a Workbench buttress whose box
overlapped canonically walkable canyon floor. A terrain-affordance footprint screen
keeps 10 of 26 large buttresses in the 20-chunk fixture while retaining all 178
bedrock spans and 40 talus stones. Normal, reverse, and rebased fixtures agree; a
focused nine-chunk test and rendered Player show the exact waypoint and a restored
25 m camera view. The older single-chunk rock-presence assertion also fails in a
screen-disabled control under this canyon-enabled mirror, so it needs reconciliation
before production cutover. The [material and clearance receipt](Evidence/WorldCreator/CanyonSlice/README.md)
contains exact code, images, XML, and Player telemetry. Continue with more varied
floor texture and rock silhouettes, a production-camera vista that shows the parent
landform, and repeatable frame attribution before deciding versions or touching the
dirty production World Creator assets.
The current 25 m rim-camera diagnostic still shows a long dark fitted-face strip
against smooth orange slopes. Add a rendered terrain-triangle contact probe at
the rim/toe and a wider fixed vista of the same generated parent before judging
whether the layered landform reads as the reference composition.
The fixed 50°/40°/25 m/48° gameplay-camera diagnostic revealed a red/black patch on the
actual near representation even with cliff decoration disabled. White-albedo isolation on
a disposable terrain material removes it; this is a material/lighting investigation,
not a reason to change the source textures or assert production-scene color. The query
also ORs floor and wall semantics at canyon overlaps. A mirror-only nearest-segment
semantic fix passed 2 focused tests, but did not cure the render patch. Resolve both
before visual acceptance; [the receipt](Evidence/WorldCreator/CanyonSlice/README.md)
contains the same-camera images, render harness, and test result.
The next mirror pass laid the Workbench talus on its side with analytic box support;
its 20-chunk contact/identity fixture passed 2 of 2 and measured at most 0.22 m
center contact error. A canyon-floor material response reduced the most saturated
patch in a fixed camera, but the adjoining slope and repeated face remain below the
reference target. The canyon-enabled mirror's material suite passed 6 of 7; the dust
plan's rebase-equality test failed. A subsequent source-linked wake quantization fix passed
the full 7-test material suite, 11 deposition tests and a focused wake taper test. The
full-content Development Player traversed five automated waypoints across two chunk-center
steps in each direction, settled its pending queues, and exposed 41–75 ms arrival-window p99
hitches plus a 783 ms first-arrival window. Those measurements are a diagnostic, not a
target-machine acceptance. The floor-material and terraced topology changes remain mirror
candidates; the [receipt](Evidence/WorldCreator/CanyonSlice/README.md) retains the exact proof.

### Objective, authority, and fixture

Generate **one representative canyon from the canonical World Creator query**, then make its
terrain, visible rock layers, Rock Workbench-derived accents, and toe debris describe the
same geological feature. Success is a repeatable, navigable top/edge/face/apron/floor view
from the production camera, with the same landform recognizable in near, middle, and far
representations. The supplied 2026-09-28 reference image is already preserved as
[SteppedMesaCliffs_2026-09-28.png](VisualReferences/Terrain/SteppedMesaCliffs_2026-09-28.png)
and is a composition and material target; it does not set exact world scale, camera values,
rock density, or a new region of canon. Open floor and quiet stretches are intentional parts
of the result.

The [World Creator charter](WORLD_CREATOR_CHARTER.md) keeps the first playable territory
free of deep canyon cuts. For this slice, use an **isolated exact project mirror** with a
cloned proof profile that enables the existing canyon excavation path. Fix the seed,
absolute route/feature ID, camera poses, quality, lighting, and source fingerprint in an
evidence receipt before iterating. Choose the representative feature by a deterministic
bounded scan of the existing `CanyonSystemPlanner`, then record its exact ID and coordinates;
do not hand-place a canyon or silently turn on canyon excavation in the production profile.
The mirror exercises the production query, representation, decoration, and collider code
with a proof-only profile difference. The first-area rule and production scene remain intact.

Gottspan owns scope, integration, and the final evidence audit. Babineaux owns the safe
Unity/editor and automation handoff. Gear Ball handles an eventual task-owned local commit
only after verification; a push or release needs separate authority. The user owns the
creative acceptance of the image target, hands-on traversal, target hardware/quality bar,
and any change to first-area geography or existing-save compatibility. No lore change is
needed. The user subsequently authorized implementation; production topology cutover still
requires the compatibility decision and accepted visual result described here.

### Current source boundary and known conflict

| Boundary | Current fact and planned role |
| --- | --- |
| Terrain truth | `WorldCreatorProductionRuntime` and `UnboundedHybridWorldQueryService` own the world query; `CanyonSystemPlanner`, `HybridTerrainPlan`, and the history provider supply canyon/landform causes. Extend this path, never a chunk-local terrain generator. |
| Cliff detection | `WorldCliffSectionStudy` emits absolute 3 m candidate sections for the current decorator. Broad-ground `ParentFeatureId` is empty; per-position context IDs are not shared cliff IDs. Its rim/toe samples and strata family are useful inputs. |
| Present face | `TopDown3DCliffFaceDecorator` reciprocally joins compatible neighbors and `TopDown3DCliffFaceMeshBuilder` makes closed, colliding LOD0/LOD1 spans. This is a connected local span, not a stable canyon-wide parent, a layered wall, or a far-landscape feature. |
| Workbench source | `CliffWallSampleReference.prefab` preserves 18 editable rocks; the dedicated `TopDown3DCliffStoneBaker` produced five A-E recipes at three LODs in `Resources/WorldCreator/CliffStones`. The current connected face uses generated bedrock and the light-gray material, not these source-derived stones. The five recipes are vocabulary for selective accents, not an 18-rock wall stamp. |
| Streaming | `TopDown3DProceduralWorld` schedules near terrain, decoration, formation refresh, and middle/far landscape separately. A cliff currently arrives with decoration after terrain. Any added face/debris work must fit that lifecycle and dispose with its owning chunk. |
| Material and persistence | `WorldSurfaceMaterialService` supplies exposure; the terrain packing adapter and shader display it. `WorldPersistenceManifest` checks topology, landform, material, and decoration versions separately. Cosmetic layers, new collision/topology, and saved-place changes cannot share one unexamined version decision. |

The [2026-09-28 cliff survey](Evidence/WorldCreator/CliffStudy/README.md) sampled the
**uncommitted** topology-13 query at seed `24681357`; it found 129 steep candidates in four
48 m windows, but no parent cliff and no caprock/ledge/apron proof. That survey describes
the first-area broad ground, not the future canyon fixture. The working tree also has
uncommitted query/profile/representation edits, a large scene edit, unrelated assets, and
`Temp/UnityLockfile`. Batch 0 must record exact hashes and owner state again. Do not run
batchmode on the live checkout, activate Unity, overwrite the scene/profile, or include
pre-existing changes in this slice's Git batch.

### Compared approaches and selected design

| Approach | Benefit | Limitation and decision |
| --- | --- | --- |
| Only retune terrain material plus raise rock density | Minimal mesh and serialization risk; useful for exposure color. | Cannot make caprock, ledges, a continuous face, or source-linked talus. Use material response within the selected design, never as the whole slice. |
| Tile complete baked Workbench wall modules along the canyon | Strong authored shape and bounded runtime mesh cost. | Repeats a recognizable stamp, creates curved-rim gaps, and can misfit changing drop. Use five source-derived recipes as restrained accents and debris families under a parent plan. |
| Extend the connected closed bedrock span with one absolute parent-face plan | Existing runtime owner, collision path, and chunk lifecycle; can fit variable rims and toes. | Needs stable grouping, strata/LOD agreement, contact, and profiling. **Selected** as the continuous mass, with selective baked accents and material/debris response. |

One canyon/face plan owns the canyon feature ID, side, deterministic section order, rim/toe
stations, strata family and phase, protected openings, and debris source. `WorldCliffSectionStudy`
remains a candidate sampler. A new pure `WorldCliffFacePlanner` in
`WorldCreator/Landforms/WorldCliffFacePlanner.cs` groups candidates by their existing
canyon `ParentFeatureId` and side, with a bounded absolute halo; it does not invent a new
terrain query or treat empty broad-ground parents as canyon IDs. Chunk coordinates only clip
and realize its spans. Existing
`TopDown3DCliffFaceDecorator` and mesh builder consume those plans rather than making
independent layout decisions. Layer boundaries use the same geological datum and sampled
terrain heights for cap, exposed bands, ledges, and toe. The authoring source stays in the
Workbench; the runtime consumes baked A-E meshes and the existing material path. Broad
landscape sand remains terrain-owned; formation contact sand stays formation-owned.

### Procedural and save contract

- **Identity and ownership:** derive the parent from world seed, the relevant landform
  version, canyon feature/side, and an absolute owner address. Section and accent IDs use
  stable parent ordinals; never use load order, instance IDs, local-origin coordinates, or
  requesting chunk as identity. Keep the first-area broad-ground case separate from a
  canyon parent; do not treat its per-position `DominantFeatureId` as a shared face.
- **Streaming:** query a bounded absolute halo so adjacent chunks choose the same parent
  and endpoint stations. Each near chunk owns only its clipped mesh, collider, accent roots,
  and debris instances. A pending or unloaded near chunk falls back to truthful middle/far
  terrain; unload releases meshes and collision; reload/rebase regenerates identical plans.
- **Authored constraints:** screen full parent and child footprints against site/approach,
  spawn, formation envelopes, Booter and BigARM reserved routes, camera sightlines, and
  planned openings before accepting geometry. A visual ledge is not declared climbable
  without a matching contact/top-out and route contract.
- **Persistence:** static bedrock and debris regenerate and write no mutable save payload.
  If a later version makes rock harvestable or destructible, save only deltas keyed by stable
  feature IDs. Do not add that gameplay in this slice. Geometry/collision changes require
  an explicit topology or landform version and saved-place compatibility decision before
  production cutover; material or arrangement-only changes use their corresponding domains.
- **Proof:** hash plans and mesh endpoints for repeated seed, changed build order, border
  requests from both sides, unload/reload, and local-origin rebase. Compare saved-place
  compatibility with the same old manifest before accepting any version bump.

### Dependency-ordered batches and stop gates

| Batch | Task-owned boundary and output | Gate before continuing |
| --- | --- | --- |
| **0. Revalidate and lock the fixture** | Record `git status`, relevant diffs, `.meta`/GUID inventory, Unity lock, pinned editor, profile versions, source fingerprint, selected generated canyon ID/coordinates, five fixed camera poses, and a full-content baseline. Use a disposable mirror of the current task-relevant working-tree content, excluding `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and unrelated `Assets/_Recovery/`; do not import into the live project. Existing study exporter and canyon proof tools may inspect candidates but must be checked for writes first. | Stop if the canyon cannot be reproduced by the existing query, the mirror differs from the chosen source snapshot, owner changes are unresolved, or baseline content cannot be compared. |
| **1. Parent-face plan** | Add `WorldCreator/Landforms/WorldCliffFacePlanner.cs` in the existing runtime assembly. Consume `WorldCliffSectionStudy` candidates and their canyon `ParentFeatureId`, verify the canyon semantic/side, and group them through a bounded absolute halo with route/semantic checks. Keep `WorldCliffSectionStudy.cs` a sampler; change it only if the fixture proves a missing input. Add `Tests/Editor/WorldCreator/WorldCliffFacePlannerTests.cs` for ID/order, both sides of a border, openings, and two seeds. Do not alter terrain heights. | Every section in the selected continuous face resolves to one stable parent and station sequence without gaps, duplicate owners, or blocked reserved routes. |
| **2. Layered continuous mass** | Adapt `TopDown3DCliffFaceDecorator.cs` and `TopDown3DCliffFaceMeshBuilder.cs` to use parent station data. Keep a closed mesh and simplified static collider; vary cap thickness, recessed strata, ledge rhythm, fractured corners, and quiet runs along the parent. Use existing `CliffWall_LightGray.mat` and `WorldSurfaceMaterialService`/terrain packing for matching exposure. Add narrow mesh/contact tests beside `TopDown3DCliffFaceDecoratorTests.cs`. | At fixed cameras, top/edge/face/toe are legible and the body joins across 18 m boundaries; automated geometry proof shows no open ends, inverted normals, collider voids, or new route obstruction. |
| **3. Workbench accents and source-linked debris** | Consume the existing baked `CliffStone_A-E_LOD0-2` assets through a bounded catalog/selection path; preserve `CliffWallSampleReference.prefab` and its `.meta`. Add parent-owned buttress/crown/ledge accents and a toe-origin talus fan through `TopDown3DNaturalObjectPlanner`/decorator only where they fit. Reuse existing `Cliff`/`Talus` families when suitable; change the dedicated baker or assets only if the fixed-camera proof identifies a specific missing shape. | No repeated full-wall stamp, floating rock, unrelated valley-floor scatter, or route closure. Accent/debris IDs and counts remain stable across reload and chunk order. |
| **4. Distance and lifecycle integration** | Extend `WorldRepresentationCompiler`/middle-far landscape only as needed so the parent canyon wall and cap silhouette persist before near decoration loads; keep one world query. Update `TopDown3DProceduralWorld` scheduling/cleanup if the proof shows popping, stale roots, or collider work spikes. Document any serialized profile/settings field and its `.meta` reference impact before editing. | The same canyon remains recognizable at near, middle, and far range; moving through two chunk-ring transitions shows no geometry seam, false landmark, collider linger, or unbounded pending queue. |
| **5. Candidate review and cutover decision** | Refresh the isolated mirror from the final source snapshot; run focused import, EditMode, validator, deterministic, contact, and performance checks. Capture the five camera poses and prepare a normal-content build for the user's traversal review. Update this plan, the relevant world standard, and evidence receipt with actual outcomes. | Stop for user visual and hands-on acceptance. Broader generation, first-area canyon enablement, topology/save migration, and release remain separate decisions. |

### Proof matrix and evidence limits

| Claim | Required evidence | What it cannot prove |
| --- | --- | --- |
| Structure and reference safety | Exact task-owned diff, `.meta`/GUID comparison, source prefab/catalog references, `git diff --check`; inspect index before a docs or implementation commit. | Unity import or visual quality. |
| Unity compatibility | Pinned `6000.4.0f1` isolated-mirror import/compile and non-mutating `WorldCreatorProductionPathValidator`; inspect logs and exit state. | Gameplay contact or a correct landscape. |
| Deterministic generation | Focused EditMode tests for parent ID, station/strata phase, canyon versus first-area grouping, border ownership, build order, reload, rebase, and saved-place manifest handling. Require tests that fail if the intended relationship breaks. | Appearance or frame pacing. |
| Visible and physical agreement | Mesh endpoint/normal/closed-volume checks, terrain-triangle contact probes, simplified collider probes from above/below/along the face, route/formation/spawn clearances, and near/mid/far plan equality. | Hands-on climb feel or the full game-camera composition. |
| Visual result | Same seed, absolute positions, camera transforms, light, resolution, and quality for baseline/candidate stills. Review below, above, along the wall, overlook, and quiet floor. The user accepts or rejects cap/strata/apron hierarchy, readable negative space, repetition, and likeness to the supplied reference. | Determinism or target-machine performance. |
| Streaming and performance | Controlled full-content Development Player with `-topDown3DFullContentProfile`, plus a non-Development visual build when available. Record build SHA, source fingerprint, scene/seed/route, quality/resolution, CPU/GPU frame percentiles and hitches, pending terrain/decoration drain, mesh/collider creation and cooking, triangles/renderers, memory, and two ring crossings against an exact baseline. | Target-machine pass until the user specifies hardware and thresholds; reduced-stress or decoration-disabled runs do not qualify. |
| Save continuity | Old-manifest load/compatibility result, same-place reconstruction, and explicit version-domain decision before any production geography change. | Approval to migrate or invalidate existing saves. |

No gameplay smoke test is assigned to an agent. Hands-on traversal is user-owned. The
Unity GUI currently owns the live checkout, so all batchmode proof must use a fresh exact
mirror and inspect its NUnit XML as well as its process result. Static checks in this
planning turn establish only document/source coherence; no Unity or visual proof is claimed.

### Risks, decisions, and completion boundary

- **Most likely failure:** local connected spans can still look like a row of similar
  walls. Compare parent-level quiet intervals, cap continuity, ledge rhythm, and toe debris
  before increasing stone count. If the selected canyon needs a truly vertical or
  overhanging cut that the heightfield cannot express, stop after the fitted-face proof;
  design a bounded feature-geometry/topology change with near/mid/far and save review.
- **Contact and streaming risk:** decoration is later than terrain and the current face
  cooks a runtime `MeshCollider` from its low-detail mesh. Measure that cost and the period
  before collision arrives; do not claim the face is traversally safe merely because the
  rendered span is closed.
- **Asset risk:** the broad authored-formation bake can discover the source cliff prefab.
  Use only the dedicated cliff bake path after source/asset ownership review; preserve the
  original 18 workbenches, transforms, nonuniform scale, `.meta` files, and GUIDs.
- **Open user decisions:** accept/revise the fixed-camera visual result and hands-on route;
  specify target hardware/performance limits for final full-content acceptance; decide
  whether any later deep canyon enters the first playable territory or requires a
  topology/save change. These are not defaults for an agent to infer.

The **technical slice** is done when the fixed canyon is deterministic, visibly layered,
source-linked, collidable where represented, seam-free through streaming, and measured
against its full-content baseline with all proof receipts recorded. The **product slice** is
done only after the user accepts its game-camera look and traversal and the agreed target
profile passes. Stop at the first failed gate; repair that batch before widening density,
generating additional canyon families, or changing the production profile.

Superseded source-rock iteration, 2026-09-28: the earlier two broad catalog rocks were superseded by
five runtime rock recipes baked from the user's `CliffWallSampleReference.prefab`, each with
three LODs. The steep-section query remained the terrain authority. That decorator packed
three, five, or seven interleaved stones as sampled center slope crosses 43, 50, and 60 degrees.
Their heights followed the rim-to-toe drop while retaining the sample's nonuniform vertical
stretch; source choice, offsets, scale, and tint derived from the world seed and absolute
section owner. An absolute one-cell halo prevented neighboring chunks from independently
selecting nearby sections. Static rocks regenerated on chunk reload; no rock state was
written to a save. The sample wall itself was never stamped as one prefab. This pass was
rejected by the user after the game-camera view showed disconnected upright stones.

## Cohesive face correction and sparse vertical scarps, 2026-09-28

The user's game-camera screenshot of the source-rock iteration shows separated upright
stones on an orange slope. This is a rejected visual result for a cliff wall. The section
decorator selects independent 3 m candidates, keeps at most six per 18 m chunk, and places
three to seven small rocks around each selected point. It has no common crest, underlying
rock mass, contact seam, or shared toe. Adding more of those stones would thicken islands
without giving them a connected geological origin. The hand-built wall remains a useful
fracture and buttress reference, but its stone meshes should become embedded accents.

### Candidate constructions

| Approach | Useful role | Reason it cannot be the whole answer |
| --- | --- | --- |
| Dense independent source rocks | Buttresses, broken corners, rare free boulders | Repeats the current isolated-column failure and multiplies colliders. |
| Closed, contour-fitted bedrock volume | Continuous rock face, cap, buried sides and toe; can follow an existing sloped rim | Needs carefully matched terrain contact, chunk edges and simplified collision. |
| Interlocking procedural stone courses | Visible deep joints, layered ledges and thick fracture blocks inside the bedrock silhouette | Must share a parent face and keep gaps bounded; free placement becomes scatter again. |
| Local volumetric field / SDF | True undercuts, caves and complex silhouettes | Too costly as the default streamed terrain representation; reserve for rare landmarks. |
| Sharpen a heightfield alone | Broad flat top and lower approach around a scarp | A single height at each horizontal coordinate cannot contain a genuinely vertical surface. |

**Selected design to prototype:** a deterministic scarp feature owns a continuous, closed
3D bedrock body. Its rim and toe trace the canonical terrain; its front is built from
connected faceted bands with a few recessed fracture seams and projecting strata shelves.
The saved source rocks are partially buried into that body as irregular buttresses, crown
breaks and toe fragments. A source-linked talus field extends outward from the toe. All
three layers use the same light-gray stone family and stable parent identity. This is an
actual thick rock formation with a simplified face collider, not a one-sided sheet or
texture projection. [Unity's *Survival Kids* terrain workflow](https://unity.com/blog/level-layout-and-terrain-workflows-in-survival-kids) used cliff-side modules,
simplified collision and a continuous terrain top; the useful lesson here is the shared
mass and contact, adapted to our streamed procedural world rather than fixed prefabs.

For existing steep ground, trace connected slope bands across chunk boundaries before
placing geometry. Measure drop, length, rim curvature and toe clearance over a world-space
halo. Build face samples at stable absolute positions along the rim, then connect buried
back, crown, stepped front and buried toe into a closed mesh. The front leans with moderate
slopes and becomes increasingly upright as the measured angle rises. Vary layer thickness,
ledge spacing, fractures and buttress locations *along the parent feature*, so adjacent
segments share one silhouette and rock strata. The old point-cluster decorator must be
replaced rather than layered on top.

**Sparse true vertical areas are a separate landform decision.** A seeded macro-scale
planner should admit occasional mesa edges, fault scarps or broken plateau lips only where
the shared world plan has enough drop and room at both the upper and lower approaches.
Authored sites, spawn clearances and Booter/BigARM route connectivity screen the whole
planned footprint. The planned scarp creates a level upper surface and lower toe in the
canonical height query, while the near terrain mesh is split at its rim/toe and the face
volume supplies the vertical polygon surface. One coordinate may then have both upper and
lower vertices; a height-only grid cannot represent that seam, as described in
[SideFX's heightfield limitations](https://www.sidefx.com/docs/houdini/heightfields/index.html).
The same feature must own near collision, mid/far silhouette and stable ID. A vertical
cliff is impassable unless an explicit traversable opening or separately validated climb
route is planned. No change to
the current first-area canyon switch is implied.

Implementation should proceed through one fixed world-space proof area: (1) contiguous
closed face following an existing steep band, without accent stones; (2) fractured stone
courses and sparse sample-derived buttresses; (3) shared terrain contact, collision and
source-linked toe debris; (4) one intentionally planned true-vertical scarp with matching
upper/lower terrain and distance representations. Compare the same camera positions against
the screenshot failure after each step. Check chunk seams, deterministic rebuild, route
clearance and normal-content streaming cost before widening generation. New vertical
physical geography requires an explicit topology-version and old-save review before
production cutover. Thresholds and frequency remain calibration values until visually
accepted by the user.

Initial implementation checkpoint, 2026-09-28: `WorldCliffSectionStudy` provided a read-only, absolute-coordinate candidate and toe-influence study with stable landform-domain IDs and a Booter/BigARM route screen. Four focused EditMode tests passed in an isolated Unity project mirror. The following runtime checkpoint supersedes its former read-only status.

Runtime checkpoint, 2026-09-28: the user asked for automatic generation wherever terrain is steep enough. The first `TopDown3DCliffFaceDecorator` built shallow face panels. The user's game screenshot showed these as blue rectangular stickers; that implementation was superseded. The next version consumed the canonical section study during near-chunk decoration and placed two overlapping **3D cliff rock meshes** from the baked natural-object catalog for sections meeting a 43-degree, 1.75 m drop technical threshold. It had LODs and simplified box collision, but used generic wide rocks and is superseded by the source-rock iteration above.

Calibration checkpoint, 2026-09-28: the [production-query section survey](Evidence/WorldCreator/CliffStudy/README.md) sampled four fixed 48 m windows at seed `24681357`. It found 129 candidate steep sections, including 34 near an 18 m chunk border, with high-drop examples around 5–6 m. One window had none. Broad-ground query context IDs proved point-specific, so the study now uses absolute owner cells for candidate identity and leaves the parent feature empty until a larger scarp plan exists. The sampled slopes do not yet express the hard caprock, layered ledges, and scree apron in the user's reference. This survey is a working-tree technical snapshot, not visual acceptance; the next design step is stable cross-section grouping and a fixed-camera material/modules/fitted-face comparison.

## User visual reference, 2026-09-28

The user supplied [this stepped-mesa cliff image](VisualReferences/Terrain/SteppedMesaCliffs_2026-09-28.png) as the intended terrain feel. It is a composition and geological-form reference, not a production screenshot or a request to replace the accepted heightfield, sky, camera, or first-area no-deep-canyon rule.

The defining shapes are broad open valley floors between long, staggered mesas; a hard, nearly horizontal dark caprock edge that breaks into ledges and buttresses; exposed lighter strata beneath it; and a sloping scree apron joining each wall to the valley floor. The large walls have quiet stretches and abrupt broken corners rather than a uniform jagged contour. Near rocks are large and irregular, while smaller fragments gather near cliff toes and thin out across the open floor. Repeated mesa bands create depth without filling every route with rock.

For the controlled comparison, judge whether the approved terrain can read as **top/edge/face/apron/floor**, especially from the production camera. Test material bands and caprock accents before adding large fitted geometry. The present `WorldCliffSectionStudy` supplies only a rim, toe, and deposit candidate; it does not yet infer the caprock break, intermediate strata ledges, or the full debris fan shown here. Those features need a shared parent-face plan and visual proof, not independent scatter at each steep sample.

### Authored cliff wall sample, 2026-09-28

The [member-by-member source audit](Evidence/WorldCreator/CliffStudy/2026-09-28-authored-wall-audit.md) supersedes the earlier broad visual description below. The user's clarified intent includes the **nonuniform vertical scales** on the 18 hand-built rocks. The current two-wide-rock runtime cluster is physically volumetric but does not reproduce this source grammar; the next cliff bake must use the source volumes and preserve the stretched forms before visual comparison.

The user also supplied [a Unity screenshot of their wall](VisualReferences/Terrain/CliffWallSample_2026-09-28.png). The exact saved `cliff wall` scene hierarchy was captured as [`CliffWallSampleReference.prefab`](../Assets/_Project/Art/Environment/Rocks/Source/CliffWallSampleReference.prefab): 18 editable rock workbenches with their original transforms, seeds, and source volumes. The source prefab uses a separate `LightCliffGray` workbench preset; the original scene and the shared material were not recolored. The material already contains gray layered albedo maps, while the previous `DarkFracturedDesert` preset applied a strong charcoal multiplier. The new preset removes that multiplier and adds restrained seed-based neutral-gray variation. Its final appearance still needs a game-camera review in the existing light.

This is the **fractured face vocabulary**, not one wall to stamp repeatedly. The saved source has no durable preview mesh; rebuilding its 18 members produced about 61,000 vertices and 122,000 triangles for a roughly 3 m wide, 3.3 m high composition. It is an authoring source, not a runtime prefab. Before streamed placement, bake a bounded set of variants by changing member shapes and spacing within the approved silhouette, combine/simplify the visible exterior for near/mid LODs, and use a smaller collision proxy. Select a variant and restrained transform offsets from a stable scarp-section ID. Fit each section to the measured rim/toe and leave quiet gaps, ledges, and route openings. Never reseed by chunk load order or place 18 raw workbench meshes per repeated wall.

## Goal and authority

Make steep terrain read as exposed rock, make valleys and scarps feel like connected places, and make debris visibly originate from nearby faces. Preserve the current broad terrain shapes while testing the treatment. The controlling product rules are `WORLD_BASIS.md` and `WORLD_CREATOR_CHARTER.md`; `WORLD_CREATOR_ARCHITECTURE_PLAN.md` controls world identity and representation; `TOP_DOWN_3D_LANDSCAPE_BENCHMARK.md` controls the production-camera comparison. The root Unity project is the sole production lane.

The Broken World is dry. Valley and drainage forms can encode ancient or abstract causes but must not imply an active river or rain cycle. The provisional first playable area has rolling terrain and no deep canyon system; the serialized `ProductionWorldCreatorProfile.asset` currently leaves canyon excavation disabled there. A separate technical comparison area may exercise the existing canyon planner without changing that rule.

## Audit of the current terrain

| Surface | Current implementation fact | Consequence |
| --- | --- | --- |
| World truth | `WorldCreatorProductionRuntime` exposes a shared query; `TopDown3DWorldGenerator` is a compatibility-facing adapter, not an independent terrain generator. `HybridTerrainPlan` holds canyon segments and bounded landform requests with stable IDs. | Extend the shared plan/query family. Do not revive the retired escarpment sampler or add a chunk-local geology authority. |
| First-area shape | With canyons disabled, `HybridTerrainWindowQueryService.SampleBaseHeight` strongly attenuates broad relief and adds faceted 36 m shelf/rise signals, 24 m pits, 4.5 m small shelves/divots, and 3 m fine relief. The current uncommitted query also blends broad flat cores from absolute 256 m owner cells. | The immediate cliff study should follow the approved shelf edges and pit shoulders. It should not replace them with generic canyon walls or mistake fine relief for a cliff. The flat-core work is live but uncommitted and must be re-audited before implementation. |
| Terrain representation | `WorldRepresentationCompiler` samples absolute positions into near/mid/far grids. The near 18 m tile is 25 vertices across, or 0.75 m per edge. `TopDown3DProceduralWorld` builds its near mesh and later assigns the same mesh to a streamed terrain `MeshCollider`. | Broad hills, slopes, and shelves fit this surface. Sub-grid cracks, true vertical walls, caves, and overhangs require bounded feature geometry. A face overlay must not misrepresent its physical contact. |
| Materials | `WorldSurfaceMaterialService` already raises strata exposure on slopes and canyon walls. `WorldTerrainMaterialPackingAdapter` maps deposit, gravel, bedrock, and weathering to vertex color for the existing terrain shader. | Material calibration is one candidate, but another slope-only color rule cannot create a fractured silhouette or a coherent cliff-to-talus relationship. |
| Valley semantics | `WorldSurfaceSemantic` has canyon floor, shelf, and wall, but no general rim/toe/gully/debris-source contract. The active first-area query does not excavate canyon segments. | A first-area scarp needs its own planned cause or a measured terrain-derived face band; it must not be labeled a canyon simply to borrow the wall flag. |
| Debris | The natural-object catalog has baked `Cliff` and `Talus` families. General scatter is density/competition based. The current `TopDown3DWorldGenerator.Sample` adapter places a slope proxy in its `Talus` field, and the scatter planner uses that value for admission. | The present value describes a steep *source*, not an accumulation at the *toe*. A source-linked debris field is needed before increasing rock count. |
| Traversal | The climbing plan uses the true collidable surface. BigARM requires a real route and physical regroup. | Decorative faces cannot claim climbable ledges. Feature collision, contact, top-out, and route reservations must derive from the same plan. |
| Evidence | Batch 10 proved deterministic long transects and near/mid/far data agreement, but its visual acceptance and full-content target profile remained open. The current worktree also contains unrelated uncommitted World Creator and gameplay edits. | Treat this as a source-and-contract audit, not a fresh visual or frame-rate verdict. Start experiments in isolated comparable fixtures and preserve the live worktree. |

## Candidate methods and decision

| Method | What it does well | Limitation in this project | Decision |
| --- | --- | --- | --- |
| A. Slope-driven material alone | Cheap, seamless with existing terrain and shader. | Cannot change rim/toe silhouette or create large fractures; already partly implemented. | Keep as the base response and tune only against fixed-camera evidence. |
| B. Scatter cliff prefabs at steep samples | Reuses baked catalog and LODs quickly. | Independent pieces make repeated stamps, floating contacts, blocked routes, and debris without a parent face. | Use individual assets as accents, not as the layout authority. |
| C. Place baked rock modules along a planned contour | Strong authored silhouettes, reusable LODs and simple baked collision. | Curved rims and unequal drops can leave gaps or repetitive joins. | Test as a face treatment and retain for prominent ledges, buttresses, and broken corners. |
| D. Generate a low-poly, contour-fitted face representation | Continuous rim-to-toe coverage and deterministic chunk seams; supports varying drop and strata bands. | Runtime mesh creation, overlaps, shadowing, and collider mismatch need proof. | Preferred continuous substrate; keep it bounded and visual-only until contact is proven. |
| E. Use volumetric/SDF geometry for every cliff | Can represent undercuts, arches, and caves. | Unnecessary complexity and meshing/collision cost for ordinary slopes. | Reserve for rare bounded physical features with an explicit traversal role. |
| F. Run erosion simulation during chunk loading | Can produce natural channel and debris patterns. | Load-order, runtime cost, and save/topology stability are difficult to bound. | Use editor/offline experiments to derive rules; ship deterministic plans and samples. |

**Preferred combination:** one absolute-coordinate valley/scarp plan defines the route, floor, shoulders, rim, toe, strata, and debris sources. The existing heightfield renders broad ground. The existing material service exposes rock along the face. A bounded contour-fitted face provides continuity; baked catalog pieces add selective silhouette breaks. A separate source-linked talus/debris plan places fragments below that face. True vertical or overhanging pieces are sparse collidable feature meshes with the climbing and route contracts. Compare C alone against D+C before choosing the final face representation; do not assume the generated substrate is visually necessary.

This selection follows feature-primitive terrain research: high-level valleys and ridges are planned first, then combined with local detail. It also adopts the useful part of modular cliff production—authored rock vocabulary and simplified collision—without making fixed modules define an effectively infinite world.

## Generation contract

1. **Purpose and parent:** Each valley, scarp, or gully has a stable parent feature, cause, strata family, and intended spatial role: open travel, reveal, overlook, narrow passage, or quiet basin. A valley centerline and its crossings are planned at a larger scale than an 18 m chunk. First-area scarps can be derived from the existing approved landform shape without enabling canyon excavation.
2. **Face admission:** Detect a continuous band with sufficient slope, vertical drop, run length, convex upper rim, and lower toe. Use hysteresis and a bounded world-space halo so small normal changes or chunk boundaries do not split a face. Reject isolated steep triangles and keep authored site, spawn, formation, and Booter/BigARM route reservations visible to the classifier. Thresholds are calibration variables, not accepted design numbers.
3. **Face form:** Derive strata breaks and fracture orientation from the parent geological context. Vary long stretches by unequal ledge spacing, eroded notches, buttresses, and quiet intervals; do not repeat a complete module arrangement. Ground the face against the *rendered near terrain triangles*, not only the continuous sampled height, to avoid visible/collider disagreement at 0.75 m mesh spacing.
4. **Debris:** Emit a stable source intensity from each exposed face section. Transport it downhill in a bounded field to produce a dense toe apron; widen to a fan at a gully mouth; allow rare outliers. Match fragment family and weathering to the source, grade size and density with travel distance, and preserve clear movement/camera sightline corridors. Talus is a deposit field, not the local slope value.
5. **Representations:** Near receives face detail and physical contact only where authorized; mid/far retain the same large silhouette and stable feature identity without full debris geometry. Cosmetic changes use the appropriate material/decoration version; changed landform or collision truth requires a deliberate topology/landform version and saved-place review.
6. **Lifecycle:** A feature ID derives from world seed, relevant version domain, canonical parent/owner, and stable ordinal. Chunk roots hold only the clipped representation and release it on unload. Reload and local-origin rebase reproduce the same absolute face/debris plan. Static geology regenerates; any future mineable/destructible face saves only deltas against its stable ID.

## Controlled comparison before production integration

Use one 20–30 m approved steep section, one valley shoulder, one gully/toe, and a section crossing a chunk boundary. Fix seed, absolute coordinates, camera, quality, lighting, and material set. Capture near, gameplay, and mid-distance views from below, above, and along the face. Compare:

1. current terrain/material baseline;
2. material calibration only;
3. baked contour modules only;
4. fitted face alone;
5. fitted face plus sparse baked accents;
6. winning face with source-linked toe apron and gully fan.

Score each on geological attachment, silhouette, joins, stratum continuity, route readability, contact honesty, camera occlusion, visible repetition, render cost, collider cost, and chunk transition. A treatment wins only when it improves the fixed-camera view and does not create a worse contact or streaming result. Preserve the current height profile through this comparison. The user's visual choice is the gate before applying the winner broadly.

## Implementation sequence after the comparison

### 1. Semantic plan and deterministic field

Add a bounded face/debris plan under World Creator, keyed to existing feature IDs and absolute coordinates. Expose rim, toe, source, transport/deposit, strata, and route-clearance signals without encoding them as GameObjects. Add repeated-seed, different-build-order, border, rebase, and ownership tests. Stop if the same world place changes with load order or if a protected route is narrowed.

### 2. Terrain material and near face

Connect exposure/deposit signals to the existing surface-material and packing path. Implement the selected face treatment under the near chunk/feature representation lifecycle, with no second height authority. Verify common border samples, no z-fighting or gaps, correct normals and shadows, and safe unload/reload. Keep purely cosmetic overlays non-colliding.

### 3. Source-linked debris and art

Use the existing baked catalog for fragments and accent modules; add only families proven missing by the comparison. Generate apron, fan, and outlier plans with a fixed per-feature and per-chunk budget. Check parent lithology, spacing, route clearance, contact sand/gravel, LOD, and repeated-seed identity. Do not copy the expensive editor-only formation treatment directly into runtime streaming.

### 4. Physical walls and climbing

For the limited sections selected for vertical traversal, add simplified static collision that matches the visible face and publish the same feature identity to climbing and path/affordance queries. Verify contact loss on unload, top-out clearance, Booter/BigARM route truth, and save restore. Apply a terrain-version change only if physical geography changes; document the old-save behavior before cutover.

### 5. Broader valley grammar and profile

Extend from the accepted section to variable-width valleys, shoulders, shelves, scarps, gullies, basin pauses, and openings. Keep the first playable area's no-deep-canyon rule unless the user changes it. In a normal-content Development Player, profile startup, steady traversal, and chunk crossings separately, including terrain integration, decoration, renderers, triangles, shadows, mesh-collider cooking, memory, and frame percentiles. Do not infer full-content performance from a fast-terrain or decoration-disabled profile.

## Acceptance and stop conditions

- A fixed-camera comparison demonstrates a substantial improvement over the current terrain in at least two approach directions and three distances; the current approved broad shapes remain recognizable.
- Valley lows, rims, toes, shelves, and debris sources cross chunk boundaries without a visible or semantic discontinuity.
- A face's visible contact agrees with its authoritative collider. Vertical climbing is offered only where a matching collider, clearance, and top-out exist.
- Booter and BigARM retain their declared route connectivity, including when the detailed chunk is unloaded. No decorative feature silently blocks a reserved route.
- Stable IDs, plan hashes, unload/reload, local-origin rebase, and saved-place/version behavior pass focused checks.
- Full-content Development Player profiling shows that the chosen representation fits the agreed target hardware budget. The target machine, resolution, preset, and frame-rate threshold remain a product gate rather than invented numbers.
- The user accepts the production-camera look and hands-on traversal. Automated checks and a technical screenshot do not substitute for that decision.

No gameplay smoke test is part of agent validation unless the user explicitly requests one. The user's hands-on review is recorded as a separate acceptance gate.

## Research basis

- Génevaux et al., [Terrain Modeling from Feature Primitives](https://perso.liris.cnrs.fr/eric.galin/Articles/2015-terrain-from-primitives.pdf): hierarchical feature primitives for valleys and ridges, combined by carving and blending. This supports a planned parent landform rather than independent slope props.
- Génevaux et al., [Terrain Generation Using Procedural Models Based on Hydrology](https://perso.liris.cnrs.fr/eric.galin/Articles/2013-river-networks.pdf): connected networks and valley profiles. Used as shape grammar for dry ancient landforms, not as an active-water rule.
- Unity's [Survival Kids terrain workflow](https://unity.com/blog/level-layout-and-terrain-workflows-in-survival-kids): authored cliff modules, continuous terrain tops, simplified collision, and low-density context-aware small objects. Its fixed-level layout is not the streaming authority here.
- [USGS talus definition](https://apps.usgs.gov/thesaurus/term-simple.php?code=1.5.5&thcode=4) and [USGS rockfall study](https://www.usgs.gov/publications/rock-fall-dynamics-and-deposition-integrated-analysis-2009-ahwiyah-point-rock-fall): face-derived toe debris and occasional farther runout motivate the source-linked field.
- Unity [mesh-collider performance guidance](https://docs.unity.com/en-us/engine/6000.3/manual/physics-section/physics-overview/physics-optimization/cpu/collider-types): simplified, pre-cooked static collision is preferable for bounded physical features; runtime cooking must be profiled.
