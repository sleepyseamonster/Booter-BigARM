# Decision Log

This log preserves the rationale and evidence behind consequential project decisions. It is an index, not a substitute for current truth: every accepted decision must also update its controlling canon, standard, roadmap, code, or project setting.

## Entry Template

### YYYY-MM-DD — Decision Title

- **Status:** proposed / accepted / superseded
- **Decision owner:** user / named owner
- **Question:**
- **Decision:**
- **Why:**
- **Evidence:**
- **Controlling files updated:**
- **Consequences / follow-up:**
- **Supersedes:** none / link to prior entry

## 2026-08-12 — Repo And Unity Project Management Model

- **Status:** accepted
- **Decision owner:** user
- **Question:** How should repo ownership and multi-agent development be managed?
- **Decision:** Gottspan is the canonical repo and Unity project manager. Specialists are temporary task-scoped seats; Gottspan owns briefing, integration, evidence, and maintenance of the top-level agent contract.
- **Why:** The user explicitly assigned Gottspan ownership and requested a durable multi-agent game-development environment.
- **Evidence:** The current task and the read-only repo, Unity, and game-design workflow audits.
- **Controlling files updated:** `AGENTS.md`, `Docs/Agents/Gottspan/`, `Docs/DOCS_INDEX.md`.
- **Consequences / follow-up:** Use bounded briefs, disjoint file ownership, single-writer Unity operations, evidence-first handoffs, and narrow integration commits.
- **Supersedes:** none

## 2026-08-12 — Isometric 2.5D Conversion Program

- **Status:** accepted
- **Decision owner:** user
- **Question:** What presentation direction should Booter & BigARM take, and who should own the conversion program?
- **Decision:** Work toward a top-down, isometric-style 2.5D game using 3D characters, environments, props, lighting, collision, and effects. Keep the existing Unity project and preserve the current 2D prototype as a protected reference during a parallel conversion lane. Gottspan owns planning, task design, architecture coordination, integration order, validation, and evidence for the conversion; the user retains creative, product, spending, destructive, release, and final-acceptance authority.
- **Why:** The user explicitly selected the isometric 2.5D direction, asked Gottspan to take ownership, and requested a complete audit and conversion plan. The repository audit shows substantial reusable game-state code but central 2D coupling in movement, world generation, BigARM locomotion, scenes, renderer setup, prefabs, and assets.
- **Evidence:** Live inspection of runtime/editor code, both enabled scenes, URP and quality assets, packages, input actions, physics/navigation settings, prefabs, art inventory, tests, build automation, and controlling docs.
- **Controlling files updated:** `Docs/3D_CONVERSION_AUDIT_AND_CHECKLIST.md`, `Docs/ROADMAP.md`, `Docs/PROJECT_STATUS.md`, `Docs/DOCS_INDEX.md`, `Docs/DECISION_LOG.md`, and the bounded Gottspan/Babineaux project-memory routing notes.
- **Consequences / follow-up:** Execute the gated conversion program without in-place scene or asset replacement. The user authorized execution on 2026-08-12, so the recommended spike defaults are recorded in `Docs/ISOMETRIC_DIRECTION_BRIEF.md`; final camera, elevation, aiming, art, platform, sourcing, package, cutover, and cleanup decisions remain gated as documented.
- **Supersedes:** none; current 2D implementation standards remain active for the preserved legacy path until individually superseded by evidence and the final cutover.

## 2026-08-12 — Protected Isometric Spike Ready For Review

- **Status:** proposed
- **Decision owner:** user
- **Question:** Does the protected isometric technical spike justify proceeding to shared runtime seams?
- **Decision:** Gottspan recommends proceeding with fixed orthographic framing as the working default, keeping 48-degree mild perspective as a comparison until the user selects a lens. BigARM remains camera-relative screen-left during the spike, elevation remains limited to modest ramps, and binary occlusion remains temporary.
- **Why:** The parallel lab compiles, preserves the legacy scenes and renderer default, passes 8 focused tests, and passed live keyboard movement, ramp, harvest, pickup, camera, occlusion, and BigARM scale checks without runtime exceptions.
- **Evidence:** `Docs/ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md` and its linked captures.
- **Controlling files updated:** `Docs/3D_CONVERSION_AUDIT_AND_CHECKLIST.md`, `Docs/PROJECT_STATUS.md`, `Docs/ROADMAP.md`, and the protected lab implementation under `Assets/_Project/`.
- **Consequences / follow-up:** Stop at CP-06 until the user chooses proceed, revise, or stop. A proceed decision authorizes planning and implementation of CP-07 only; it does not authorize packages, purchases, Build Settings cutover, production assets, release, or legacy deletion.
- **Supersedes:** none

## 2026-08-13 — Perspective Top-Down 3D Revision

- **Status:** accepted
- **Decision owner:** user
- **Question:** Should the conversion keep an orthographic/isometric presentation or move farther toward a fully 3D presentation?
- **Decision:** Use a perspective, elevated top-down 3D camera and fully 3D procedural world. Prioritize camera feel, player movement, deterministic 3D world generation, correct gamepad control, and a smaller BigARM with simple intelligent follow behavior. Defer detailed harvesting, item, survival, and final companion mechanics.
- **Why:** The user wants visible perspective depth and a more fully 3D result while preserving the readability and navigation feel of a top-down game.
- **Evidence:** The user's current direction following review of the protected orthographic and mild-perspective spike.
- **Controlling files updated:** `Docs/TOP_DOWN_3D_FOUNDATION_PLAN.md`, `Docs/DOCS_INDEX.md`, and the new implementation under `Assets/_Project/`.
- **Consequences / follow-up:** Orthographic remains historical comparison evidence only. The first perspective foundation stays in a separate scene, preserves the 2D baseline and prior spike, and does not authorize packages, production assets, save migration, Build Settings changes, cutover, release, or legacy deletion.
- **Supersedes:** the camera and presentation direction in the 2026-08-12 protected-spike proposal; the preservation contract remains active.

## 2026-08-13 — Right-Stick Perspective Camera Orbit

- **Status:** accepted
- **Decision owner:** user
- **Question:** How should the player control the elevated perspective camera on gamepad?
- **Decision:** The right stick rotates the camera in the conventional 3D manner: horizontal input orbits around Booter, vertical input adjusts pitch within a constrained elevated top-down range, and releasing the stick holds the chosen view. Player movement remains camera-relative.
- **Why:** The game is now fully perspective and 3D-focused, while still requiring the readability and navigation feel of a top-down game.
- **Evidence:** The user's explicit instruction after accepting the initial perspective foundation.
- **Controlling files updated:** `Docs/TOP_DOWN_3D_FOUNDATION_PLAN.md`, `Docs/PROJECT_STATUS.md`, `TopDown3DInputRouter`, `TopDown3DCameraRig`, validator, and focused EditMode tests.
- **Consequences / follow-up:** Physical-controller orbit speed, vertical direction, pitch limits, and interaction with movement remain user-owned feel tuning. This does not authorize free-look mouse behavior, camera lock-on, shoulder swapping, packages, or cutover.
- **Supersedes:** the fixed-yaw/no-player-orbit camera statement in the initial foundation plan.

## 2026-08-13 — BigARM Companion Direction And Physical Traversal Rule

- **Status:** accepted
- **Decision owner:** user
- **Question:** What is BigARM's role, and what must happen when he is separated from Booter?
- **Decision:** BigARM is a simple companion and synergistic, non-separable part of Booter's gameplay mechanics. He is no longer a rover, mobile habitat, safe zone, storage point, crafting platform, or home base. BigARM may wander and perform autonomous tasks, but he always retains a true world position and must physically traverse the world to reach Booter. Calls, distance, unloaded terrain, stuck recovery, save/load, and scene transitions do not authorize snapping or teleporting him near Booter.
- **Why:** The user fundamentally revised BigARM's story and game-design role and explicitly rejected magical distance recovery.
- **Evidence:** The user's current instruction and the first natural-follow implementation in the perspective runtime lane.
- **Controlling files updated:** `Docs/WORLD_BASIS.md`, `Docs/BIGARM_COMPANION_STANDARD.md`, `Docs/ROADMAP.md`, `Docs/PROJECT_STATUS.md`, and `TopDown3DBigArmFollower`.
- **Consequences / follow-up:** The first slice covers loaded-world route following and physical catch-up. The next approved slice must decide how deterministic coarse traversal, multi-anchor streaming, route persistence, and physical re-entry cooperate when BigARM's true location is outside loaded terrain.
- **Supersedes:** the mobile-habitat, moving-safe-zone, crafting-hub, storage/recovery, home-base, camera-relative recall, and teleport-style distance-recovery assumptions in older documents and prototypes. Protected legacy assets remain preserved as implementation history rather than current design authority.

## 2026-08-13 — Procedural Generation First Across Game Systems

- **Status:** accepted
- **Decision owner:** user
- **Question:** What architectural concern takes priority when new game systems are designed and built?
- **Decision:** Procedural generation is the first architectural compatibility check for every new or extended gameplay system. Each system must explicitly address deterministic world identity, stable generated-object identity, chunk streaming and unload/reload behavior, authored constraints, persistence deltas, and deterministic proof wherever those concerns apply.
- **Why:** The production game depends on an effectively infinite generated world. Systems built around permanently authored or always-loaded scenes would create expensive rewrites and incompatible behavior later.
- **Evidence:** The user's explicit 2026-08-13 direction that procedural generation must be the priority when building any game system.
- **Controlling files updated:** `AGENTS.md`, `Docs/WORLD_SYSTEMS_STANDARD.md`, `Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md`, `Docs/ROADMAP.md`, and `Docs/Agents/Babineaux/templates/UNITY_TASK_BRIEF.md`.
- **Consequences / follow-up:** Every future system brief must complete the procgen compatibility contract before implementation. Concerns may be marked not applicable with a reason. This does not require every mechanic to be random; authored content should constrain and anchor procedural output.
- **Supersedes:** none; this elevates and broadens the existing deterministic world-generation baseline.

## 2026-08-13 — TopDown3D Production Cutover And Legacy 2D Isolation

- **Status:** accepted
- **Decision owner:** user
- **Question:** Which implementation is the primary product, and how should the former 2D prototype be organized?
- **Decision:** TopDown3D is the primary production product. Preserve the former 2D prototype as reference content under `Assets/_Project/Legacy2D/`, separated by asset and assembly boundaries. Make the TopDown3D scene the enabled build entry point and the 3D renderer the project default; keep legacy scenes disabled and explicitly bound to the retained 2D renderer.
- **Why:** The user accepted the 3D version as primary production and requested complete separation of legacy 2D content.
- **Evidence:** The user's explicit 2026-08-13 acceptance plus the GUID-preserving migration, Build Settings update, renderer-default update, and assembly boundaries in the repository.
- **Controlling files updated:** `AGENTS.md`, `Docs/LEGACY_2D_BOUNDARY.md`, `Docs/PROJECT_STRUCTURE.md`, `Docs/PROJECT_BASELINE.md`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset`, and the assembly definitions under `Assets/_Project/Legacy2D/` and `Assets/_Project/Scripts/Runtime/Isometric/`.
- **Consequences / follow-up:** New production work must not depend on the legacy folder. Legacy content remains preserved and usable but is not co-equal production architecture. This decision does not authorize legacy deletion, package removal, save migration, release, or publication.
- **Supersedes:** The build-entry, renderer-default, and no-cutover constraints in the 2026-08-13 perspective foundation decision. It does not supersede the procedural-generation-first architecture or the legacy preservation requirement.
