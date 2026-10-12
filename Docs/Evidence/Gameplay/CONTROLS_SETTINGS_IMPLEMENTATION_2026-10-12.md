# Controls settings implementation — October 12, 2026

The author subsequently requested pursuit of the [input remediation plan](../../Design/Gameplay/INPUT_INTERACTION_REMEDIATION_PLAN.md) as a goal. This advances the controls-settings slice while physical-controller acceptance remains author-owned.

## Behavior

Open **System menu → Settings → Controls**. Choose Gamepad or Keyboard, cycle the shipped active button actions and capture a replacement button/key. Prompts use connected control names. Unsupported axes, movement/camera controls, UI confirmation/back controls and coactive conflicts reject the draft edit. The persistent inventory opener additionally cannot share UI navigation/transfer buttons. D-pad reassignment stays within the four algae/deployment commands, preserving diagonal arbitration. Menu navigation and escape remain usable throughout capture; Back, Escape, Start, focus loss or device disconnect cancels listening. Listening times out after ten seconds.

Cargo timing cycles through 0.20, 0.35, 0.50, 0.65 and 0.80 seconds. Tap and hold use the same selected threshold and binding. Input System parameter overrides replace the actual action timing without appending duplicate Tap/Hold interactions. A 0.65-second virtual-input regression verifies that 0.40 seconds still taps and a completed hold never also taps.

Edits remain drafts until Apply. Conflict rejection, cancellation and failed writes preserve the draft and runtime controls. Back offers the existing discard dialog. Reset changes only the draft after confirmation; Apply writes it. Startup loads preferences and refreshes existing prompts. Overrides use stable binding IDs and preserve unrelated action contracts.

`controls-v1.json` is separate from world saves. Missing files use defaults. Current-schema validation rejects unsupported timing, duplicate or incompatible IDs, wrong schemes, reserved paths and conflicts. Corrupt, newer and incompatible files remain intact with saving disabled; defaults stay usable. Saves use a unique temporary file and atomic replacement before runtime application. Preferences and tests never create or alter world, terrain, resource or carrier snapshots.

## Integration and proof

Production `SystemMenu.prefab` was upgraded from its exact current source in `D:/BooterBigArmValidation/Inventory20261011-01`. Existing prefab document file IDs remained a subset of the upgraded asset, and its `.meta` remained byte-identical. The existing camp entry and EventSystem ownership are preserved. No production scene, terrain, package, renderer asset or material was edited by this task.

**187/187 focused EditMode tests passed**, zero failed/skipped, with current Unity 6000.4.0f1 import and assembly compilation. Diagnostics: candidate `Logs/controls-settings-release-20261012.log` and `Logs/controls-settings-release-20261012.xml`.

The canonical Controls prefab validator passed. Six captures at 1920×1080, 1280×720 and 1024×768, normal and large text, were visually inspected: labels, notices, footer and hints fit without clipping. Diagnostics: `Logs/controls-prefab-sealed-capture-20261012.log` and `Logs/ControlsCaptures/`. Captures use temporary renderer instances with gameplay effects removed for UI inspection; no renderer asset is changed. The initial graphics capture inherited gameplay effects; the isolated capture path was corrected before accepting visual proof.

Source/prefab hashes match the tested candidate. Native prefab trailing whitespace was checked separately: all such lines are Unity serializer empty scalar fields, retained to keep the exact tested asset; code and documents use strict whitespace checks. No gameplay smoke test, production Play Mode operation, window focus or external publication is part of this proof.

The first interactive-capture tests detected that the Input System requires an existing binding on a temporary capture action. The capture now uses a disposable bound action and intercepts its result into the draft, preserving runtime bindings until Apply. The repaired suite passed before final conflict/reset checks were added; final results follow below.

## Iteration boundaries

Analog movement/camera, UI navigation, dormant Jump/Attack/Scout/debug contracts and gameplay action durations were preserved. No production latency has been measured in this task, so profiling-driven cadence/pooling/repeat changes remain conditional as the plan requires. The next author-owned checks are preference changes on actual devices, hold comfort, cancellation, reload/reset, mining beside Legger and canister pickup around other targets.
