# Compact menu implementation evidence

Date: 2026-10-07. Implements the [audited menu plan](../../Design/Gameplay/MENU_SYSTEM_IMPLEMENTATION_PLAN.md) in the active Unity TopDown3D lane. The user authorized implementation and explicitly selected a running world while the menu is open.

## Result

Start/Menu/Options opens the compact Resume, Radial Setup, Settings, Exit Game menu; Escape opens it from gameplay. Start/Escape/Back have context-specific routing and cannot accidentally accept an exit or discard decision. Player controls are suppressed while the world clock and physics continue. No timeScale writer or pause service was added.

The authored `Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab` supplies the GUI: iron panels, warm text, amber focus outline, safe-area layout, Normal/Large text, responsive radial setup, scrolling advanced options, and retained footer controls. Menu motion can be reduced to remove the short unscaled fade. Full action names remain in the assignment/detail area; compact ring labels avoid cramped eight-sector text.

Radial setup reuses the existing command and preference owner. It supports four/eight directions, up to four pages, renaming, default page, activation style, and wheel scale. Apply keeps `radial-layout-v1.json`; dirty Back, deletion, and sector reduction require explicit choices. Deleting an earlier page preserves the default by stable page ID. Setup returns to its invoking menu or inventory.

Inventory suspension clears only an uncommitted transfer selection and preserves player/cargo focus. Resume rechecks live cargo range. System inventory/radial openers are blocked while the menu owns input. Disable/unload releases references without re-enabling gameplay.

Settings persist separately to `menu-settings-v1.json`; drafts and failed writes remain recoverable, and future-version files are preserved. Master Volume was omitted under the plan's conditional rule because the production source/scene has no useful audio consumer. No production sounds were invented. Exit confirms that current session progress will not be saved and calls Application.Quit only after explicit confirmation; it does not add a gameplay save service.

## Current Windows proof

The original checkout was owned by the user's Unity Editor. Validation used a disposable file copy under ignored `Logs/MenuValidationProject`; no Git branch/worktree operation or foreground activation was performed. Assets, pinned Packages, and ProjectSettings were copied; final task-owned source/input/prefab bytes were compared against that copy.

- Pinned Unity 6000.4.0f1 compiled the implementation and authored the prefab in the isolated copy. The prefab/meta pair was copied back byte-for-byte; existing GUIDs were preserved.
- Focused candidate tests: **27 passed, 0 failed** across `TopDown3DMenuTests`, `TopDown3DMenuUiIntegrationTests`, `TopDown3DInventoryUiTests`, and `TopDown3DRadialTests`. The integration fixture uses an empty disposable scene, real UI Submit/Back, a running clock and a Rigidbody probe. It does not load gameplay terrain or perform a gameplay smoke test.
- Menu production validator passed: Greater Wasteland routing, configured closed installation, repeated-install identity, one EventSystem, canonical prefab and menu action. It modifies transient scene objects only and does not save scenes.
- Twenty-four GUI captures cover root, settings, setup, and confirmation at 1920×1080, 1280×720, and 1024×768, each with Normal/Large text. Representative full-size captures and the complete contact sheet were inspected. Setup uses scrolling in stacked layouts. Captures are GUI-only previews against a neutral background, not gameplay screenshots or user visual acceptance.
- Important text token contrast against panel, neutral button, and selected slot surfaces measures 14.19:1, 10.59:1, and 6.95:1 respectively. These checks cover stable opaque surfaces, not every transient fade or disabled state.
- Every original input action/binding ID and definition, plus control schemes, remains unchanged. Only ToggleMenu and its bindings were added. The installed Input System names the control `<Gamepad>/start`; `startButton` is its C# property. A test asserts that the action resolves to the actual controller control.
- Scoped whitespace, metadata pairing, and documentation-link checks pass. No package, renderer, production scene, terrain, world identity, or gameplay-save format changed.

Unity's generated whitespace after empty YAML keys and new-file line endings were normalized after authoring; serialized values, references, and GUIDs were unchanged. The normalized source and validation-copy assets remain byte-identical.

Local diagnostics: `Logs/menu-tests.xml`, `Logs/menu-tests-closeout.log`, `Logs/menu-gui-sealed.log`, `Logs/MenuValidationProject/Logs/MenuCaptures/`, and `Logs/menu-layout-contact-sheet.png`. Earlier authoring/test failures were corrected; those logs are diagnostics, not passing proof. Existing terrain-tool warnings are outside this menu task.

## Remaining acceptance and limits

Physical-controller comfort and final visual approval remain user-owned. The exit callback and confirmation are tested; a standalone Player was not built or closed during this task. The GUI captures do not prove production scene appearance, every scroll position, or localized text expansion. The menu installs through the existing scene bootstrap on the next gameplay scene load/Play session; the user's active Editor session was not controlled or restarted.

Unrelated material, terrain authoring, source-data, and tooling changes in the shared workspace remain owned by their original writers and are excluded from this commit. No push or external publication occurred.
