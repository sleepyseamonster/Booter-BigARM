# Current Engine Handoff

Updated 2026-09-09, America/Phoenix. Accepted direction: proprietary engine, regular third-person game, Windows PC target, optional Mac development, open Greater Wasteland rock workload and deferred canyons. Work exclusively under `Engine/`.

## Completed Preparation

- Requirements, decision register, proposed ownership boundaries and staged foundation plan are linked from [the workspace index](../README.md).
- Research now includes an exact six-component experiment cohort, checksums, primary license notices, inspected API/build findings and reproduction instructions.
- Historical rock images and a source manifest preserve useful references. The collected images predate the final accepted Unity sand treatment and do not establish third-person quality. Lorekeeper's narrow world-context synthesis distinguishes source proposals from local game truth.
- Five Python standard-library tools provide environment inventory, document checks, verified source preparation, bounded command receipts and shared output-path handling. Three SOPs define dependency evaluation, experiments and handoffs.
- EXP-001 compiles and links C++20 with SDL3, bgfx and standalone Dear ImGui on the current Mac, then passes its headless technical check. This is an isolated compatibility executable, not a production engine foundation.

## Evidence

| Check | Observed result | Receipt |
|---|---|---|
| Environment inventory | Apple arm64, Apple Clang 21, Xcode 26.6, SDK 26.5, CMake 3.28.1, Python 3.13.2; Ninja missing | [Inventory](../Evidence/environment-2026-09-09.json) |
| Six pinned source trees | Archive and extracted-content verification, repeated after building | [Final source inventory](../Evidence/EXP-001-sources-final.json) |
| Native compilation/link | Passed after three recorded failed attempts; incremental build with CMake regeneration | [Build 4](../Evidence/EXP-001-build-04/result.json), [failure explanations](../Research/STACK_AUDIT.md) |
| Headless execution | 1/1 technical test passed: synthetic SDL events, bgfx Noop resource lifecycle, ImGui CPU draw data | [Run log](../Evidence/EXP-001-run/output.log), [receipt with executable hash](../Evidence/EXP-001-run/result.json) |
| Preparation tool behavior | 12 tests passed: success/failure, timeout, input mutation, receipt protection, links and unsafe paths/archives | [Final test log](../Evidence/preparation-tests-final/output.log), [receipt](../Evidence/preparation-tests-final/result.json) |
| Reference and evidence integrity | Six notices, nine source records, two collected images and seven log receipts match their hashes; three unrelated Unity files match their pre-task content | [Integrity snapshot](../Evidence/content-integrity.json) |

Run `python3 Tools/check_workspace.py` for current document/record checks. Run commands and source hashes are retained; the large dependency and build caches are ignored. See [reproduction](../Research/Experiments/README.md). The final candidate has not been rebuilt from an empty build directory or tested on a separate clean checkout; no clean-machine claim is made.

## Boundaries and Unresolved Decisions

No real GPU backend, shader compiler, rendered inspector, native Windows build, physical input or gameplay was exercised. No production renderer, physics package or package manager has been selected. Jolt and the later asset/profiling libraries remain proposals. There is no runtime world generator or third-person controller yet.

The headless fixture has no world data: geographic identity, chunk streaming, generated-object IDs, authored world constraints and persisted runtime deltas are inapplicable to its library plumbing. Their future contracts are recorded in [architecture](./ARCHITECTURE.md); this experiment does not validate them.

Target PC hardware, frame-time/resolution goals and visual/camera acceptance remain open. These affect later evaluation rather than preventing preparation. Mac compatibility work must remain bounded. Final world-map coordinates and canyon development remain deferred.

## Next Bounded Action

Begin F1/F2 with a small native application/rendering fixture using the pinned cohort as a candidate: window/input, perspective mesh, a pinned shader pipeline, actual inspector rendering, diagnostics, resize and clean shutdown. First establish a fresh build recipe. Use one neutral mesh on open ground, with temporary local scale/axes explicitly labeled. Record real GPU output and resource ownership before expanding into rock generation.

Keep the ImGui adapter question explicit: EXP-001 uses its official core independently of bgfx's customized example integration. Verify a compatible adapter rather than assuming the headless result covers it. If the next step needs substantial Mac backend work, record the exact blocker and move development/testing to Windows as the user permits.

Continue with [FOUNDATION_PLAN.md](./FOUNDATION_PLAN.md), [DECISIONS.md](./DECISIONS.md) and [the experiment SOP](../SOPs/RUN_EXPERIMENT.md). Hands-on gameplay and creative acceptance remain user-owned. Unity files and the Arc & Dust source repository were not modified by this preparation work.
