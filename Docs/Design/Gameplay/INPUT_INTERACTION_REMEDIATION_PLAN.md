# Input and interaction remediation

## Authority and baseline

The author requested this plan and implementation on October 12, 2026 to make interaction faster and smoother. The [first audit](../../Evidence/Gameplay/INPUT_CONTROL_AUDIT_2026-10-12.md) established hold-West cargo separation and 101 passing focused tests. The root Unity production lane and background-only agreement remain authoritative.

## Pass 1 — implement now

| Problem | Change | Acceptance proof |
|---|---|---|
| Nearby targets can alternate with tiny movement | Retain a valid target through a small distance margin; switch for a substantially closer or more clearly faced target | Jitter, intentional switch, obstruction, availability and range regression tests |
| Canister pickup depends on the generic selected target | East/C queries reachable canisters independently with the same physical checks | Canister beside another interactable, blocked/out-of-range rejection |
| Tap may select a different object between press and release | Capture the world target at gesture start; recheck reach and availability on release | Changed target cannot silently redirect the tap; invalid captured target rejects |
| Simultaneous cancel and interaction can depend on callback order | Resolve world tap/pickup/cancel at the end of input processing; cancel takes priority and suppresses cargo/dosing/deployment that update | Combined-event and modal-boundary tests |
| Cargo hold and action feedback feel slow | Trial 0.35-second cargo hold; display hold progress and retain actionable prompts alongside feedback; hide gameplay prompts in modal UI | Timed keyboard/gamepad holds, mode and feedback tests; author-owned feel acceptance |
| Both inventory shoulders toggle rather than choose a pane | Left shoulder/Q selects Booter or cache; right shoulder/E selects Legger; repeated input is idempotent | Physical binding and repeated selection tests |

Keep the tap/hold duration equal so a long press never starts mining or pickup. Retain ordinary tap execution on release: an immediate press would begin mining before a cargo hold can be distinguished. Do not add speculative input buffering across busy actions or modal transitions.

## Constraints

This pass changes command dispatch, transient targeting and presentation. Deterministic world identity, generated-object IDs, authored IDs/constraints and persisted runtime deltas remain under their existing owners. Greater Wasteland terrain remains fixed and always loaded. No terrain streaming, chunk unload/reload, generation, save schema, asset relocation, package upgrade or scene rebuild is needed. Cargo access retains same-scene, range and line-of-sight checks; no remote transfer or teleportation is introduced. Resource and canister transactions retain completion-time checks and existing action durations.

## Verification and integration

Preserve unrelated dirty files; work on main. Run focused EditMode input, interaction, inventory, radial, menu, camp, locomotion, traversal and resource/canister tests in the existing isolated validation candidate after refreshing exact source. Verify hashes, documentation links and scoped diffs before committing. Keep logs outside production content. Record results in this plan after validation. No gameplay smoke tests or foreground activation.

## Following iterations

1. Author hands-on acceptance: short taps beside Legger, 0.35-second hold, adjacent deposits, East pickup near other objects, cancellation and menu transitions. Record symptom, device, control, context, expected action and observed action. Tune only the affected threshold and rerun its regression checks.
2. After first-pass acceptance, design a small controls-settings surface for binding overrides and hold timing. Save preferences separately from world state, provide device-specific prompts and safe conflict handling, and validate migration/reset before enabling it. Rebinding remains a separate implementation slice, not implied by this pass.
3. Profile interaction/UI cost in a dedicated performance slice if there is observed latency under production load. Use measured frame/input timing before reducing scan cadence, pooling queries or changing menu navigation repeat. Preserve deterministic selection and physical reach checks.
4. Resolve dormant action contracts deliberately when their mechanics are designed. Do not expose Jump, Attack, Scout or debug-save bindings as functional production controls.

## Results

Pass 1 is implemented. Unity 6000.4.0f1 import/compilation and **136/136 focused EditMode tests passed**, zero failed/skipped, in `D:/BooterBigArmValidation/Inventory20261011-01`. Diagnostics are `Logs/input-remediation-sealed-20261012.log` and `Logs/input-remediation-sealed-20261012.xml`; the previous candidate source was preserved under `Logs/input-remediation-20261012-prechange`.

The initial run passed 114 and failed 22: a destroyed manually configured feedback HUD retained input callbacks and contaminated later UI/menu checks. Teardown now unsubscribes on destruction, removes managed subscriptions even after a Unity owner becomes a destroyed-object reference, and rejects late callbacks. The complete focused set was rerun successfully after the repair; no assertions were weakened.

Proof includes gesture target capture, invalid captured-target rejection, cancellation/modal precedence, independent canister selection, target jitter/obstruction/depletion checks, idempotent inventory pane selection, hold progress and faster cargo completion, feedback visibility across modes, and existing input/menu/camp/movement/resource transaction coverage. This establishes command behavior, not measured production latency or physical-controller feel. Mining/action timing, movement/camera tuning, terrain and persistence were preserved. Hands-on acceptance and future settings/profiling slices remain as listed above.
