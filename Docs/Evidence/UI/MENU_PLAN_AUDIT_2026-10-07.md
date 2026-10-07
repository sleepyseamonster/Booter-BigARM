# Menu implementation plan audit

Date: 2026-10-07. Scope: source-based audit of the [menu and GUI plan](../../Design/Gameplay/MENU_SYSTEM_IMPLEMENTATION_PLAN.md) against the active TopDown3D implementation. Findings were corrected in the plan only. No runtime/input asset, scene, prefab, or user-owned material changed; no Unity tests or builds ran.

The proposed compact menu is aligned with the user's direction. Before this audit, several lifecycle/input/proof contracts were insufficient for autonomous implementation. The corrected plan resolves the technical ambiguities below; pause policy and final product defaults remain decisions rather than inferred approval.

| Finding | Severity | Source evidence and consequence | Plan correction |
| --- | --- | --- | --- |
| Back and opener ownership was abstract. | P1 | Input router publishes UI Cancel while InputSystemUIInputModule independently dispatches it; inventory toggle is guarded only against Radial. Escape is shared with radial cancellation. A callback-order-dependent implementation could close two layers, reopen inventory, or commit a wheel action while opening the menu. | Explicit context/input-update arbitration, Start priority, state transition table, one modal Back consumer, suppression of underlying System openers and UI-module cancellation, text-edit precedence. |
| Setup entered from inventory had no consistent pause lifetime. | P1 | Radial OpenCustomization enters Inventory mode; FinishEditor always reopens inventory. Merely adding a root menu leaves that existing entry outside the menu policy. | Route both entry points into the same coordinator/SystemMenu lifetime with explicit parent return. Nested dialogs never release pause early. |
| Suspension and resume could leak gameplay or lose cargo focus. | P1 | Inventory IsOpen depends on canvas visibility; Close and OnDisable enter Gameplay. Focus memory stores only a slot index. Router's right-stick neutral guard only covers a return from Radial. | Distinct suspension without controller disable, player/cargo focus identity, uncommitted source clear, range refresh on return, guarded teardown, and neutral-stick/held-submit protection. |
| Installation repeatability and prefab creation had no executable boundary. | P2 | Existing inventory installer reconfigures its controller on repeat installation. The new Resources prefab had no specified background-safe authoring path. Configure could reset open UI or drafts. | Configure-before-activation, no repeated Configure/Build, one chained installer, missing-asset containment, narrow Editor prefab authoring helper with no scene writes or fallback UI. |
| GUI layering and expanded layout were unspecified. | P2 | Existing canvases use 120/220/240; new root/dialog order was absent. The proposed setup had no concrete behavior for eight sectors, advanced controls, large text, or long names. | Menu order 300, child dialog sibling order, active-panel focus confinement, responsive stacked columns, scrolling options with fixed footer, stable selection IDs, expanded layout proof. |
| Pause proof could overclaim EditMode evidence. | P1 | Survival/harvester tick on deltaTime; follower physics ticks on FixedUpdate. Direct advancement and timeScale assertions do not establish actual paused frames or responsive UI. | Separate isolated frame-based EnterPlayMode test using the existing Editor test assembly, teardown restoration, clean scene gate, distinct result files; no production gameplay smoke test. |
| Settings persistence and audio scope were underspecified. | P2 | No runtime audio owner was found in the inspected production source/scene. Plan named version 1 but omitted path/schema, unsupported-version behavior, and preview restoration on teardown. | Exact menu preference path/fields, atomic writes, injected test storage, future-version protection, preview rollback; isolated audio proof or omit the proposed volume row if it lacks a useful consumer. |

Additional source check: `Interaction/TopDown3DPlayerActionController.CancelGather()` already cancels placement preview and incomplete harvester actions on a non-Gameplay mode change. The plan now reuses that existing interruption authority and requires pre/post-commit assertions instead of adding competing cancellation logic.

## Proof and remaining gates

The audit inspected the input router, inventory canvas/controller/installer, radial canvas/controller/installer/state, action controller, survival and harvester tick paths, existing tests, and installed Test Framework EnterPlayMode support. Documentation links, fenced blocks, scoped diff whitespace, and task ownership are checked before committing the audit. Those checks establish document coherence, not Unity compilation or runtime correctness.

The following remain explicit implementation gates:

- User decision on whether the menu pauses the world; no response has been treated as approval.
- Acceptance of proposed compact GUI/settings/exit defaults through implementation authorization.
- Safe Editor ownership/validation route at Batch 0, with no foreground activation or competing editor.
- Candidate-bound compile, focused input/UI tests, isolated pause integration if selected, rendered layout review, and separately reported standalone exit proof.
- Final visual and physical-controller acceptance by the user.

Verdict: the plan is ready for product review and bounded implementation authorization once the pause policy is resolved. No technical finding from this source audit remains intentionally unaddressed in the plan; actual implementation proof is still outstanding.
