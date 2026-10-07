# Menu system and GUI implementation plan

Date: 2026-10-07. Status: planning only. Gamepad Start opening the menu and the simple supporting-menu direction are approved requirements. GUI dimensions, secondary controls, settings contents, and pause behavior below are proposed defaults. No implementation is authorized by completion of this document.

Audited and corrected 2026-10-07. See the [plan audit](../../Evidence/UI/MENU_PLAN_AUDIT_2026-10-07.md) for findings, source evidence, and remaining product/proof gates.

## 1 Objective and observable success

Pressing the gamepad Start/Menu/Options button opens a compact menu in Greater Wasteland. The player can resume, configure existing radial commands, adjust a small set of working settings, or exit. Every screen works with a controller and keyboard/mouse, has visible focus and predictable Back behavior, and returns promptly to play. The GUI supports immersion through restrained material and typography rather than a mandatory device interaction.

Success includes an actual designed GUI, stable input/navigation, radial preference continuity, honest exit behavior, and focused proof. A placeholder list or a menu that forces repeated management is insufficient.

## 2 Owner lane authority and stop conditions

The user owns product behavior and visual acceptance. Gottspan owns scope/integration; Babineaux owns Unity wiring and background-safe validation; Gear Ball owns scoped commits and any separately authorized publication. No narrative device or source-art work is required. These are role responsibilities, not a request to start additional agents.

Production code stays in `Assets/_Project/Scripts/Runtime/TopDown3D/`, assembly `BooterBigArm.TopDown3D.Runtime`, namespace `BooterBigArm.TopDown3D`. Editor-only creation/validation belongs in `BooterBigArm.Editor`; focused tests in `BooterBigArm.Editor.Tests`.

Stop implementation on an unresolved pause decision, a needed gameplay save system, conflicting input ownership, unavailable safe Unity validation, or changes requiring package/settings/archive authority. Do not broaden the menu task to resolve those by assumption. No pushes, branch switches, gameplay smoke tests, or foreground application activation are included.

## 3 Sources of truth

1. Current user request: Start opens the menu; create the plan and include GUI.
2. Root `AGENTS.md`, [local workspace](../../LOCAL_WORKSPACE.md), Gottspan/Babineaux role contracts.
3. [Accepted menu direction](./MENU_SYSTEM_DIRECTION.md), [world basis](../../WORLD_BASIS.md), inventory/cargo and survival standards.
4. [Input standard](../../Engineering/INPUT_ARCHITECTURE_STANDARD.md), [Unity automation](../../Engineering/UNITY_AUTOMATION.md).
5. Live input asset, router, UI controllers/views/installers, assembly definitions, scene routing, validators, and focused tests.
6. [Wilds audit](../../Research/UI/MONSTER_HUNTER_WILDS_MENU_AUDIT.md) and [immersive research](../../Research/UI/IMMERSIVE_MENU_SYSTEMS_RESEARCH.md), as presentation references only.

## 4 Audited repository truth

On 2026-10-07, the checkout is `main` tracking `origin/main`. `Greybox_BigARM.mat` is pre-existing user-owned dirty content. Batch 0 must refresh status because concurrent work can change the checkout.

- Editor is pinned to 6000.4.0f1; uGUI 2.0.0 and Input System 1.19.0 are installed dependencies. Runtime/test assemblies already reference both.
- Greater Wasteland is the only enabled build scene. Generation and streaming remain deferred.
- `InputSystem_Actions.inputactions` contains Gameplay, System, UI, and Radial maps. System has inventory/radial and generated-reference/debug actions; there is no Start binding or menu toggle action. UI Cancel is the generic `*/{Cancel}` binding; Radial Cancel explicitly includes Escape.
- `TopDown3DInputRouter` controls maps, gameplay intent, prompts, and cursor. Modes are Disabled, Gameplay, Inventory, Radial. System stays enabled in modal states.
- `TopDown3DInventoryUiSceneInstaller` wires one scene EventSystem and InputSystemUIInputModule. `TopDown3DRadialSceneInstaller` attaches customization to inventory.
- `TopDown3DRadialMenuController.OpenCustomization()` enters Inventory mode; `FinishEditor()` always opens inventory. Draft validation, dirty-departure checks, stable page IDs, and atomic replacement of `radial-layout-v1.json` already exist.
- `TopDown3DRadialCanvas.ShowEditor()` recreates controls after edits and recovers selection by object name. New setup navigation should preserve stable control identity rather than depend on recreated names.
- Inventory Open/Close/OnDisable currently change modes directly; `IsOpen` is tied to visibility. Remembered focus is a slot index without player/cargo ownership. Suspension must be a distinct state, not a call to Close or disabling the controller GameObject.
- The EventSystem also processes UI Cancel independently of the router's cancel event. Inventory toggle remains enabled through System; the router currently blocks it only during Radial mode. Both paths need explicit menu ownership gates.
- `Interaction/TopDown3DPlayerActionController.HandleInputModeChanged()` already invokes `CancelGather()` outside Gameplay; cancellation also destroys canister preview and cancels pending placement/pickup before commit. Reuse that path rather than adding another interruption authority.
- Current Canvas orders are HUD 120, Inventory 220, Radial 240. Menu overlay/dialog ordering and underlying interactability need an explicit contract.
- The survival driver and authored harvester use scaled delta time; the Legger uses FixedUpdate. Input suppression alone does not pause them. No runtime timeScale owner was found by the source search.
- `BadwaterPlayableSceneBuilder.ValidateFromCli()` rejects `TopDown3DGameStateSaveService` in production. The retained save service references a procedural world; it is not an approved Greater Wasteland save path.
- `Temp/UnityLockfile` exists and Unity processes are running. No Unity command ran during planning. Do not assume which process owns this checkout; verify command lines and lock ownership before implementation validation.

## 5 Approved scope and preservation

Deliver one menu root, one radial setup page, one small settings page, and reusable confirmation dialogs. Preserve quick-wheel assignments, controller stick-release semantics, inventory controls, cargo range, camera feel, existing preferences, and stable Unity asset/action GUIDs.

Exclude a title-screen flow, journal/map, new crafting/inventory mechanics, device model, inspection camera, graphics settings suite, rebinding framework, generation/streaming, gameplay save integration, and package upgrades. Do not expose nonfunctional controls to make the menu look complete.

## 6 Decisions and proposed defaults

| Item | Requirement or proposed default | Implementation gate |
| --- | --- | --- |
| Gamepad opener | Approved: Start opens the menu, using `System/ToggleMenu` bound to `<Gamepad>/startButton`. | Add during the approved implementation, not during planning. |
| Pause policy | Proposed: pause the world while the menu family is open. An optional clarification is pending in chat. | Record the user's answer or obtain a decision before changing simulation behavior. |
| Keyboard | Proposed: Escape opens from gameplay; Escape/Back closes only the top screen thereafter. | Include in plan approval; handle shared Cancel bindings once per press. |
| Repeated Start | Proposed: close the root; from a child, return to root through dirty-draft protection. | Never bypass a pending discard or exit decision. |
| Initial settings | Proposed: Master Volume, Menu Text Size, Reduced Menu Motion, with Apply and Back. | Implement real owners and persistence for all three; no graphics controls in this slice. |
| Exit | Proposed: explicit Exit Game confirmation, default focus Keep Playing. No Save and Exit. | Approved implementation must acknowledge that current session progress is not saved by this menu. |
| GUI | Proposed: compact iron panel, warm text, amber focus, readable dimmed world. | User visual acceptance after bounded layout review. |

The pause clarification is not needed to finish this plan. Until resolved, pause-specific batches stay conditional; elapsed time without a reply is not approval.

## 7 World and persistence contract

World seed/version and chunk ownership are not applicable to the menu itself: it presents player/UI state and does not create world content. Greater Wasteland is authored and always loaded; do not implement streaming. Installation is scene-scoped and idempotent; unloading the gameplay scene closes screens, releases input/pause ownership, and unsubscribes safely. No cross-scene UI singleton or persistent scene reference is required.

Stable item IDs and existing radial page IDs remain authoritative; a selected row index never becomes an item identity. Existing inventory/placement services own persisted gameplay deltas and validate authored constraints. Legger cargo cannot become remote storage. Menu preferences use a separate versioned UI file; retain `radial-layout-v1.json` and its schema. A future world-save redesign is a separate approved task.

No repeated-seed terrain tests or streaming budgets are needed because this change neither generates nor loads world content. Relevant performance concerns are menu allocations, Canvas rebuilds, and responsive navigation.

## 8 Candidate approaches

| Approach | Boundary and benefits | Risks and proof |
| --- | --- | --- |
| Extend runtime-created uGUI | Add the root/settings to existing dynamic canvas construction. Minimal new asset wiring; same input technology. | Continued embedded styling and recreation make GUI tuning/focus harder. Requires focused navigation/layout proof. No schema/GUID migration. |
| Authored uGUI views with a bounded coordinator | Prefab-owned root/settings/dialog views, existing router and services, runtime-populated radial assignments. Clear layout ownership and stable selectable controls. | New prefab/meta pairs and a narrow radial return-path cutover. Verify prefab references, installation, single EventSystem, and input regressions. |
| UI Toolkit menu shell | Named UXML/USS layouts for root/settings with explicit input/focus integration alongside the current uGUI wheel. | Viable for forms, but introduces another presentation/focus boundary and asset vocabulary. Prove coexistence and routing; do not change packages to make it work. |

Choose authored uGUI views plus a bounded coordinator. It fits the installed production stack, allows GUI review, and avoids a framework migration. Use a small finite set of screen states, not an extensible application framework. Keep all domain and preference owners singular.

## 9 Recommended architecture

`System/ToggleMenu -> TopDown3DInputRouter.MenuToggleRequested -> TopDown3DMenuController -> view and active screen`

- Router remains the input/map/cursor/prompt authority. Add SystemMenu mode at the end of the existing enum to preserve existing numeric values. UI and System are active; Gameplay and Radial are disabled.
- Menu controller owns Root, RadialSetup, Settings, ExitConfirm, and DiscardConfirm, parent restoration, focus, and one consumed Back request per physical press. Do not let several subscribers independently respond to Cancel. All setup entry points acquire the same menu lifetime; setup opened from inventory does not remain an unrelated Inventory-mode editor.
- Radial controller remains the sole radial preferences/draft/command authority. Extract its setup view presentation if necessary; do not clone its data owner into the new menu.
- Menu settings service owns only menu settings persistence and application. Views edit a draft; Apply writes before replacing accepted settings, retaining the draft on failure.
- If pause is approved, the controller owns a single lifetime pause lease covering all its child screens. Capture the prior timeScale and restore exactly once on close/disable/unload; do not force 1 or change fixedDeltaTime. Audit every relevant unscaled/real-time simulation path before claiming a full pause. Coordinate any new timeScale writer discovered during Batch 0.
- Exit is behind an injected request callback for tests. Production uses `Application.Quit`; Editor testing never quits the Editor. Unity documents that this call is [ignored in the Editor](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application.Quit.html).

### Input routing and return context

| Current surface | Start | Escape or UI Back |
| --- | --- | --- |
| Gameplay | Open root. | Escape opens root; unrelated gameplay buttons keep their existing meaning. |
| Quick radial | Cancel without command, then open root. | Cancel radial only; do not open root on the same press. |
| Inventory | Suspend inventory and open root. | Close inventory only; do not open root on the same press. |
| Menu root | Resume the original context. | Resume the original context. |
| Radial setup or Settings | Request root through dirty-draft handling. | Return to the invoking surface through dirty-draft handling. |
| Confirmation | Keep the confirmation open; never accept a decision. | Cancel the confirmation and restore its invoking selection. |
| Text editing | Request root through dirty-draft handling after ending edit safely. | First cancel/end the text edit; a later Back requests screen departure. |

For setup entered directly from inventory, Back/Apply returns to inventory; Start requests the menu root, which then resumes inventory. Apply from root-entered setup returns to root. Leaving settings after Apply returns to its parent. Root Resume restores Gameplay or Inventory; the quick wheel never resumes automatically.

Collect router toggle/cancel requests with their originating device/control, input-update identity, and context captured before changing action maps. Resolve one navigation transition per update after collecting these requests. Start takes priority over a simultaneous wheel selection/release; cancellation never commits the wheel. Escape is ToggleMenu only when its captured context is Gameplay; in other contexts it is Back. Do not change maps inside competing callbacks before request classification.

During SystemMenu, the coordinator is the only Back consumer. Prevent InputSystemUIInputModule.Cancel from independently bubbling into the selected control, and suppress the inventory/radial cancel subscribers for this context. Text-field edit cancellation is routed explicitly before screen Back. Outside SystemMenu, preserve existing UI Cancel module wiring and behavior. Reject System/ToggleInventory and System/OpenRadial while this menu owns input; generated/debug System handlers must respect the menu context if they exist in an installed scene.

ToggleMenu is a Button action with one press-only performed event, no hold/repeat interaction, and no initial-state activation on map enable. Its bindings use the existing Gamepad and Keyboard&Mouse groups. Preserve every existing action/binding ID and add new IDs only for the new action/bindings. Verify the asset's update mode supports UI input while timeScale is zero; if it does not, stop for the smallest reviewed input-update change rather than altering project settings silently. Keep menu input disabled when the router is Disabled and ignore opener requests while the application lacks focus.

Use an opening/transition guard so a held Submit or mouse press cannot select a newly shown row/dialog. On final resume, retain mouse-delta suppression and extend neutral-stick protection to the stick used for UI navigation or radial selection; currently the router's right-stick guard only covers Radial-to-Gameplay. Movement resumes after that held navigation stick returns to neutral. Controls released during the menu cannot execute a deferred gameplay command. Test these rules through router actions and the real UI module, not only direct state calls.

Inventory suspension captures player-versus-cargo focus plus slot index, clears an uncommitted transfer-source selection, hides/blocks its view without disabling its controller, and leaves subscriptions safely attached. Resume refreshes cargo range and inventory contents before restoring a valid selectable; if cargo is unavailable, fall back to the last valid player slot. Close/unload of a suspended controller must not re-enable gameplay while the menu remains active. The menu releases ownership before explicitly restoring its saved parent mode.

## 10 GUI specification and source boundary

### Root layout

Use a safe-area-centered, approximately 480 by 430 reference-unit panel at 1920 by 1080. Size to available space; never crop on a 1280 by 720 or 4:3 display. Dim the background with a simple overlay rather than blur or a camera change. Keep world context visible and block pointer interaction behind the menu.

Reserve sorting order 300 for the menu Canvas, above the existing 240 radial Canvas. Child dialogs use later sibling order within this Canvas; do not create a second EventSystem. Covered panels are non-interactable and do not block raycasts; the menu backdrop does. All focus navigation is confined to the active panel/dialog and never reaches hidden inventory or setup controls.

```text
                MENU
          > Resume
            Radial Setup
            Settings
            Exit Game

        [Select]       [Back]
```

Resume receives initial focus. Root rows are at least 52 reference units high with generous spacing; titles around 28 and ordinary labels around 22 reference units are starting values, not final physical-pixel acceptance. Primary text `#F2EADD`, backing `#1B1D1C`, amber focus `#E8B86D`; pair color with an outline/marker. Use a stable near-opaque reading surface. Disabled, focus, hover, selected assignment, and unsaved draft states must remain distinguishable.

Use the existing builtin font for the first authored layout so this slice needs no new licensed asset. Measure text at output size; provide Normal/Large menu text with reflow, not just scale. Any later font replacement requires glyph/asset review. No CRT noise, device glare, background animation, elaborate decoration, or repeated opening ritual.

### Radial setup layout

One screen: title and current page at top; a familiar compass preview/slot selector on the left; short assignment list on the right; selected command description beneath; Apply and Back in a fixed footer. Select a slot, choose an assignment, Apply. Choosing an assignment never executes a gameplay command.

```text
RADIAL SETUP                         Field   <  >
        North: Inventory             Assign action
West: Canister     East: Empty        Inventory
        South: Call Legger            Call Legger
                                     Dust Canister
                                     Auto-Pack / Clear
More options
[Apply]                     [Back]
```

Expose existing page creation/deletion, renaming, four/eight sectors, activation style, and wheel scale through a collapsed More Options area. Keep the current capabilities and imported pages usable; do not silently reduce capacity or rewrite preferences. Gamepad renaming may retain the name without typing; if editing text requires a keyboard, state it and leave all other setup operations fully controller-operable. Page deletion and dirty departure receive explicit choices, avoiding the current ambiguous use of Apply to confirm unrelated operations.

Make the two columns stack vertically at narrow sizes or Large text. The assignment list and expanded More Options may scroll; Apply/Back stay reachable in a fixed footer. Provide a real eight-sector preview for saved eight-sector pages. Slot-to-assignment navigation is explicit left/right; scroll follows focused controls. Store selection by page ID and control purpose/sector index rather than label text. Test longest supported page names, command names, a full four-page layout, and deletion/resizing while focused. Never hide the only path to a saved page or its options.

The shared text-size/reduced-motion settings apply to setup and dialogs too. Preserve existing per-wheel scale/interaction settings under radial ownership. Assignment choices use existing supported commands and availability rules.

### Settings and dialogs

Settings uses one list with Master Volume (0–100), Menu Text Size (Normal/Large), Reduced Menu Motion, Apply, and Back. Settings have persistent version-1 global preferences. Master Volume applies through a single inspected audio owner; use AudioListener.volume only if no mixer/volume authority exists at implementation. Preview draft changes immediately, revert on canceled departure, and retain draft on write failure. No empty categories.

Canonical menu preference path: `Application.persistentDataPath/menu-settings-v1.json`. Fields: version 1, finite `masterVolume` in [0,1], a validated Normal/Large enum, and reduced-motion Boolean. Missing file uses defaults; corrupt data uses defaults with diagnostic feedback; unsupported future versions remain untouched and disable Apply with an explanation. Atomic temporary-file replacement mirrors radial storage. Pause behavior is a product policy, not an extra user-facing toggle in this slice. Restore previewed values on discarded departure, disable/unload, or failed initialization; committed preferences remain global. Inject a storage directory for tests rather than accessing the user's file.

The audit found no AudioMixer/AudioSource/AudioListener.volume owner in the inspected runtime or production scene text. Master Volume therefore needs an isolated AudioSource fixture to prove its effect; do not invent production sounds. Recheck this finding at Batch 0. If volume has no useful production consumer, omit this proposed row rather than delay the menu to build an audio system.

Exit confirmation: “Exit game?” and “Current session progress will not be saved.” Buttons: Keep Playing and Exit Game. Do not offer a save action or claim progress is preserved. Handle unsaved radial/settings drafts before opening this confirmation. Dialog focus starts on the safe action, stays inside the dialog, and restores its invoking row on cancel.

Minimal opacity/color transitions may use at most about 100 ms of unscaled time; reduced motion removes them. Selection and input become active immediately. Navigation is explicit, includes keyboard arrows and gamepad D-pad/stick, and preserves focus during value changes and device switches. Prompts come from binding authority; do not hardcode Xbox labels or show both device schemes simultaneously.

### Planned files

All paths below are under the repository root; each new Unity asset/script/folder receives its intentional `.meta` once.

| Existing file | Planned change |
| --- | --- |
| `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions` | Add ToggleMenu action and two bindings; preserve all existing IDs/bindings/meta. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs` | Bind/unbind menu event, append SystemMenu mode, route opener/Back arbitration without duplicated callbacks. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiSceneInstaller.cs` | Ensure menu installation through the existing scene/event authority. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.cs` | Suspend/resume beneath menu safely; suppress inventory toggle/cancel while menu owns them. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryCanvas.cs` | Add explicit view visibility/interactability suspension without forcing controller Close/OnDisable. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DRadialMenuController.cs` | Cancel wheel without activation on Start; setup completion returns to explicit invoking screen; reuse existing draft/storage owner. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DRadialCanvas.cs` | Replace recreated setup controls with stable setup view; retain working wheel. |
| `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DRadialSceneInstaller.cs` | Route existing inventory customization entry into the same menu-owned setup flow. |
| `Assets/_Project/Tests/Editor/TopDown3DInventoryUiTests.cs`, `TopDown3DRadialTests.cs` | Preserve existing regression cases; adapt explicit return-context coverage. |

New runtime UI files: `TopDown3DMenuController.cs` (pure transition rules may be a small companion `TopDown3DMenuState.cs`), `TopDown3DMenuView.cs`, `TopDown3DRadialSetupView.cs`, `TopDown3DMenuSettingsService.cs`, and `TopDown3DMenuSceneInstaller.cs` in the same `UI/` directory. Prefer one composite `Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab` with authored root/settings/dialog/setup panels; installer loads this one canonical asset, never a runtime-generated fallback. Missing asset reports a clear error and leaves gameplay usable.

New focused tests: `Assets/_Project/Tests/Editor/TopDown3DMenuTests.cs`. An Editor-only validator under `Assets/_Project/Scripts/Editor/Validation/TopDown3DMenuProductionValidator.cs` may inspect installation in memory without saving. No production scene, package, renderer, gameplay-setting, or archive changes are expected. Update this plan's evidence/status and menu direction after implementation; avoid duplicating controlling rules in multiple documents.

Create the new prefab through a narrowly scoped Editor-only authoring helper, `Assets/_Project/Scripts/Editor/UI/TopDown3DMenuPrefabAuthoring.cs`, when background-safe asset authoring is needed. It writes only the new canonical prefab and its initial metadata; it refuses to overwrite an existing prefab without a reviewed task-owned update. It does not open/rebuild/save gameplay scenes or change Build Settings. This authoring path is distinct from the validator and is never called a read-only check.

Installation calls the existing inventory/radial installer first, then resolves its EventSystem and constructs one inactive menu prefab instance, injects dependencies, and activates only after configuration. The prefab is a view, with no EventSystem or scene references. Already configured menu/inventory/radial controllers are returned without Configure/Build again; repeated scene-load hooks must not reset drafts, double-subscribe, or add duplicate components. If the base scene dependencies or prefab are missing, do not enter SystemMenu or acquire pause ownership. Validators/tests use injected temporary radial storage and never read/write the live user's radial layout.

## 11 Implementation batches and gates

| Batch | Intent and ownership | Contract proof and rollback | Gate |
| --- | --- | --- | --- |
| 0 | Gottspan/Babineaux refresh status, HEAD, affected callers, package pins, save/audio owners, process command lines and lock; resolve pause choice. | Record exact owned paths; preserve material changes. Inspect input bindings/meta and prefab resource path. No Unity launch while GUI owns checkout. | Scope and authority clear; pause decided; safe validation route available before validation-dependent work. |
| 1 | Runtime lane adds ToggleMenu, input request arbitration, and screen-state rules. | Input asset IDs unchanged; Start press fires once; context-captured Escape cannot open and close in the same update. Start wins over radial release; held Submit cannot accept a newly opened dialog. Include UI-module routing tests. Roll back only owned code/asset pairs. | Input/state tests pass before attaching gameplay views. |
| 2 | UI lane authors composite GUI and installer. | Idempotent installation, closed startup, one EventSystem, explicit navigation, backdrop hit blocking, safe area and text reflow. Missing-prefab behavior verified. Capture root/settings/dialog/setup layouts. | Usable controls and acceptable compact visual hierarchy. |
| 3 | Runtime/UI lane integrates radial and inventory return contexts. | Start from wheel cancels without executing; Start above inventory restores selection on close. Setup from root returns root; inventory entry returns inventory. Apply/dirty Back/page deletion/storage failure preserve existing semantics. | Existing radial/inventory tests plus new transition/draft cases pass. |
| 4 | Runtime lane implements settings, approved pause policy, and exit request. | Global settings versioning and failure retention; canceled previews restore accepted values. Pause applies consistently through root-entered and inventory-entered setup; nested dialogs retain its lifetime. Exit calls injected request once after confirmation. | Working settings, honest exit text, actual frame-based pause proof if selected, no gameplay-save integration. |
| 5 | Babineaux/Gottspan validate candidate, perform bounded GUI inspection, align documentation; Gear Ball commits task-owned passing changes. | Focused tests, compile, asset/meta checks, safe validator, layout evidence. Standalone exit proof separated from Editor callback proof. | Technical completeness and visual/hardware proof accurately reported; no push. |

Batch 1–4 source/GUI details must not expand into a generalized settings framework. Stop if simplifying setup would require changing command semantics or invalidating player preferences. Existing dirty files are never rollback targets.

## 12 Verification matrix

| Claim | Required proof | Limit |
| --- | --- | --- |
| Start and keyboard open reliably | Simulated gamepad Start press/hold/release and keyboard Escape; one menu event; all original action IDs unchanged. | Does not prove physical-controller labeling/feel. |
| Back affects one layer | Root/child/dialog tests including same-frame Escape opener and UI/Radial Cancel; dirty drafts and double inputs. | State tests alone do not prove visible layout. |
| Existing controls preserved | `TopDown3DInventoryUiTests`, `TopDown3DRadialTests`, new `TopDown3DMenuTests`. | Tests are focused contracts, not gameplay smoke tests. |
| Pause works if selected | EditMode proves pause ownership/restoration, including prior zero/non-unit values, nested dialogs and both setup entry paths. A separate isolated frame-based test enters PlayMode using the installed Test Framework's EnterPlayMode/ExitPlayMode yield instructions, advances configured vital/harvester fixtures and a Rigidbody probe before/during/after pause, and proves UI input still operates. | Pure EditMode advancement calls cannot prove Unity's paused frame/physics behavior. Do not infer effects/audio stop from timeScale alone. Unity [timeScale documentation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-timeScale.html) describes its limits. |
| Preferences persist | Temporary-directory tests for valid/invalid/versioned JSON, write failure, Apply/Cancel, radial v1 continuity. | Never overwrite the user's live preference file in tests. |
| Correct scene wiring | Menu validator plus production routing checks; one installer/event authority and no generator/save-service addition. | Existing radial validator installs transient objects in memory; it is not purely read-only though it does not save project content. |
| GUI readable | Root/setup/settings/dialog captures at 1920×1080, 1280×720, a 4:3 layout and large text; inspect four/eight sectors, four pages, More Options, long labels, keyboard text focus, and modal tab/navigation confinement. Measure important text contrast at least 4.5:1 against its final backing. | Requires render-capable capture; `-nographics` alone cannot prove appearance. |
| Exit correct | Injected callback test proves one confirmed request; standalone Player verification proves actual closure. | Application.Quit is ignored in Editor. A build/exit check must not be represented as gameplay acceptance. |
| Comfortable interaction | User controller review: open, assign, Apply, Back, resume, device switch. | User-owned acceptance; no agent-created gameplay smoke tests. |

Windows focused test command, only after verifying the GUI does not own the checkout:

```powershell
$menuEditor = Join-Path $env:LOCALAPPDATA 'Unity/Editors/6000.4.0f1/Editor/Unity.exe'
$menuProject = (Get-Location).Path
$menuTestArgs = @('-projectPath', ('"' + $menuProject + '"'), '-batchmode', '-nographics', '-runTests', '-testPlatform', 'EditMode', '-testFilter', 'BooterBigArm.Tests.TopDown3DMenuTests;BooterBigArm.Tests.TopDown3DInventoryUiTests;BooterBigArm.Tests.TopDown3DRadialTests', '-testResults', ('"' + (Join-Path $menuProject 'Logs/menu-tests.xml') + '"'), '-logFile', ('"' + (Join-Path $menuProject 'Logs/menu-tests.log') + '"'))
$menuTestProcess = Start-Process -FilePath $menuEditor -ArgumentList $menuTestArgs -WindowStyle Hidden -PassThru
$menuTestProcess.WaitForExit()
```

Ensure ignored Logs exists before launching; inspect exit code and XML counts/failures, not only log text. Omit `-quit` from the test run. Proposed new validator command uses the same project/editor guard with `-batchmode -nographics -quit -executeMethod BooterBigArm.Editor.TopDown3DMenuProductionValidator.ValidateFromCli`; inspect its implementation first to confirm no persistent writes. If the GUI owns the checkout and no safe existing background bridge can run focused checks, report the validation handoff without focusing Unity or running a competing editor.

Pause integration lives in `Assets/_Project/Tests/Editor/TopDown3DMenuPauseIntegrationTests.cs`, in the existing Editor test assembly. Run it separately with `-testFilter BooterBigArm.Tests.TopDown3DMenuPauseIntegrationTests` and distinct `Logs/menu-pause-tests.xml` and log paths. Its EnterPlayMode fixture must use a clean disposable test scene; never enter PlayMode with the user's unsaved or production scene. Capture/restore timeScale, AudioListener state, input settings, and temporary resources in teardown even on failure. This is a bounded component/integration test, not a gameplay smoke test. If a clean isolated scene cannot be supplied safely, report missing pause integration proof rather than silently enter the live scene.

## 13 Risk and failure containment

Escape/Start overlap is contained by one input request arbiter and held-button neutral guards; router subscribers must not compete. Radial setup owns no gameplay commands. Inventory suspension must suppress transfer actions while covered, preserve focus, and recheck cargo range on return. Opening the menu should cancel in-progress gathering safely without consuming resources; inspect action-controller cancellation and placement ownership before choosing interruption timing.

The live action-controller mode-change path already cancels gathering, placement preview, and incomplete harvester placement/pickup. Preserve it and test Start interruption before and after transaction commit; committed rewards/placements remain committed and are never rolled back by menu entry. Add those component assertions to the focused menu tests without changing gameplay timing or input semantics.

UI tick and navigation repeat must remain responsive if paused. Focus/device loss never applies drafts or exits; keep the menu open and preserve a valid keyboard/controller path. Restore cursor/map/pause ownership on teardown without resurrecting destroyed scene references. Preference write failure leaves editing active with a short readable error. Missing assets fail clearly without a second UI implementation.

Review every modified `.meta` and action ID; never regenerate an existing input asset or rebuild Greater Wasteland to install this menu. Retained macOS proof is not Windows validation.

## 14 Completion and stop condition

Technical implementation completes only when the full menu/GUI flow exists, focused checks pass against the final task-owned candidate, settings/radial preferences retain their contract, and exit/paused-state proof is reported at its actual level. Visual and physical-controller acceptance remain explicitly pending until reviewed. No test, build, or screenshot was produced by planning.

Stop after this menu slice. Do not add title/load/map/crafting screens, gameplay saves, device models, or extra configuration because the architecture could support them.

## 15 Readiness and next move

The plan selects routine technical choices and defines GUI layout, source boundaries, batches, and proof. It is ready for review; simulation behavior awaits the pause answer and the proposed settings/exit/GUI defaults require acceptance through implementation authorization. The next move is approval of this bounded slice, followed by Batch 0. No feature source or Unity asset is changed in this planning task.
