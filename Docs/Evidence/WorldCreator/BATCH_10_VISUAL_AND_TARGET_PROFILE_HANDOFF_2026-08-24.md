# Batch 10 visual acceptance and target-profile handoff

Date: 2026-08-24

Candidate commit: `4f4303a60cd3be1ee0c2aee3599f33d36853b7fc`

Production scene: `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`

Production seed: `24681357`

## Purpose and authority

This is the final user-review contract for the World Creator foundation. It does not approve the visuals, choose target hardware, change runtime content, or define the future thematic coordinate system. Batch 10 closes only after both gates below pass:

1. the user accepts the production-camera landscape and hands-on traversal;
2. the agreed Windows target passes a controlled full-content profile.

If either gate fails, classify the problem as composition, traversal, representation, or performance. Do not reopen the architecture without evidence.

## Important build distinction

Do not use Editor Play Mode or the current Development Player for final visual acceptance. `TopDown3DPlaytestPerformanceProfile` automatically applies there and deliberately uses 50% render scale, reduced shadows and texture mips, a two-chunk terrain ring, no runtime decoration, the fast terrain path, and one generation stage per frame.

- Visual acceptance must use a non-Development Player with the production quality posture and runtime decoration present.
- Target profiling must use a Development Player only after the stress profile can be disabled while retaining telemetry/profiler access. The current stress-profile receipt is diagnostic evidence, not the full-content target result.
- Record the exact commit and whether the stress profile is active for every capture. A mixed or unknown posture is invalid evidence.

No Unity window should be opened, focused, or controlled by an agent unless the user explicitly requests visible interactive work.

## Gate A — fixed-camera and hands-on visual acceptance

### Review setup

- Build type: non-Development Player.
- Scene: production scene listed above.
- Seed: begin with `24681357`; use F8 only for explicitly labeled comparison-seed observations.
- Quality: record the selected quality level. Standalone currently defaults to `Ultra`.
- Resolution: record the exact output resolution and display scaling.
- Production camera baseline: perspective, 25 m follow distance, 48-degree vertical FOV, 50-degree pitch, 40-degree initial yaw. Do not tune the camera during this review.
- Wait for initial terrain and decorations to settle before judging a still view. Streaming behavior is judged separately while moving.
- Controls: WASD or arrows move, mouse delta or right stick changes view, left Shift or right shoulder sprints, F1 or left shoulder recalls BigARM, F5 saves, F6 loads, F7 rebuilds the current world, and F8 advances the seed.

### Capture metadata

Use the same metadata for every accepted or rejected shot:

| Field | Value |
| --- | --- |
| Candidate commit/build ID | |
| Seed | |
| Absolute position or saved-place ID | |
| Topology/site versions | `2 / 2` unless the build reports otherwise |
| Camera position/rotation, pitch, yaw, distance, FOV | |
| Resolution/quality level | |
| Stress profile active | Must be `No` |
| Screenshot/video path | |
| Result | Pass / Needs work |
| Classification when rejected | Composition / Traversal / Representation / Performance |
| Observation | |

The current absolute coordinate labels are technical evidence only. They do not establish the later thematic globe notation or regional lore.

### Required seven views

1. **Canyon reveal** — approach from obscured or partial sight, then reveal the main spine or a tributary. The destination should become readable without feeling staged or pasted in.
2. **Canyon interior** — show wall profile, shelves, deposits, route choices, and continuity through the corridor. Look for smooth tubes, repeated wall modules, seams, or implausible drainage.
3. **Overlook with near/mid/far agreement** — show local material breakup, a mid-ground formation or basin, and a distant landmark together. The same place and silhouette must survive representation changes.
4. **Quiet basin** — show deliberate negative space. The area should feel composed and navigable without uniform clutter or empty procedural noise.
5. **Geological silhouette** — circle or approach a major rock formation from at least two directions. Reject recognizable repeated assemblies, radial stacks, floating contacts, collider mismatch, or silhouette swaps.
6. **Proof-influence boundary** — cross the non-canon technical influence transition. The change may be readable, but it must remain geographically believable and must not appear as a texture, height, or density seam.
7. **Streaming transition** — travel continuously through at least two streaming-ring transitions while watching the route and horizon. Reject visible chunk edges, false landmarks, major popping, blocked movement, or long generation stalls.

### Hands-on decisions

Mark each item independently.

| Question | Pass / Needs work | Notes |
| --- | --- | --- |
| Does a 10-minute Booter route remain readable and physically traversable? | | |
| Does BigARM follow, diverge where its profile should differ, and regroup without breaking world truth? | | |
| Can visible destinations be approached without turning into false or relocated landmarks? | | |
| Do dense, sparse, narrow, and open intervals create a convincing rhythm? | | |
| Do rocks, canyons, shelves, deposits, and materials appear causally related? | | |
| Are nearby compositions free of recognizable cookie-cutter arrangements? | | |
| Do near, mid, and far representations preserve identity without obvious popping or seams? | | |
| Does the landscape feel like a stage worth exploring even before later emergent gameplay systems? | | |

Gate A decision: **Pass / Needs work**

User approval and date: ______________________________

## Gate B — controlled target-Windows profile

### User-owned target definition

These fields must be approved before the result can be called the target-machine proof.

| Field | Approved target |
| --- | --- |
| CPU | |
| GPU and VRAM | |
| System RAM | |
| Storage type | |
| Windows version | |
| Output resolution | |
| Quality preset | |
| Display mode and refresh | |
| Frame-rate threshold | |
| Frame-time / 1% low threshold | |
| Maximum acceptable transition hitch | |

No provisional hardware class is recorded here. The user retains the target decision rather than inheriting an unapproved or time-sensitive market assumption.

### Required build posture

- Exact candidate source and build ID recorded.
- `StandaloneWindows64`, Development build, Autoconnect Profiler or an equivalent recorded profiler session.
- Full production render scale, shadows, texture mip posture, streaming radius, runtime decoration, and generation scheduling enabled.
- Stress profile explicitly reported as inactive.
- No editor process or unrelated foreground workload contaminating the run.
- Same resolution, quality, and display mode throughout the capture.

The repository build entry point is `BooterBigArm.Editor.BuildAutomation.BuildFromCli`. It accepts `-buildTarget StandaloneWindows64`, `-development`, and `-buildOutput`. The active Unity target must already be `StandaloneWindows64`; changing build target is a separate controlled step and is not performed by this handoff.

### Profile sequence

Capture a single continuous session, with markers or timestamps for each phase:

1. cold launch through initial terrain settlement;
2. 60 seconds stationary at the starting view;
3. 10 minutes of representative travel containing open ground, canyon interior, overlook, and geological density;
4. at least three streaming-ring crossings at normal movement speed;
5. at least three streaming-ring crossings while sprinting;
6. BigARM recall/regroup during or immediately after a transition;
7. return to one previously visited place and confirm stable reconstruction;
8. 60 seconds stationary after travel to expose retained memory or queue growth.

### Required receipt

| Measurement | Result |
| --- | --- |
| Average FPS / frame time, stationary | |
| Average FPS / frame time, representative travel | |
| 1% low and 0.1% low | |
| CPU main/render thread p95 and p99 | |
| GPU frame p95 and p99 | |
| Worst transition hitch and phase | |
| GC allocations per frame and largest collection | |
| Managed, native, graphics, and total memory | |
| Draw calls/batches, SetPass calls, triangles, vertices | |
| Loaded terrain/decorated chunks and peak pending queues | |
| Build log path | |
| Profiler capture path | |
| Player log path | |
| Candidate commit/build ID | |
| Stress profile active | Must be `No` |

Separate startup, steady-state, and transition results. A startup compile or loading spike does not become the steady-state number, and a steady average does not hide transition hitches.

Gate B decision: **Pass / Needs work**

Approved target and user/date: ______________________________

## Final closeout

Batch 10 final decision: **Approved / Not approved**

- Gate A passed: Yes / No
- Gate B passed: Yes / No
- Remaining issue classification, if any: Composition / Traversal / Representation / Performance
- User approval and date: ______________________________

Until both gates pass, the automated World Creator foundation is technically sealed but not finally approved. No coordinate-region canon, new site-generator breadth, or legacy-authority deletion is implied by this handoff.
