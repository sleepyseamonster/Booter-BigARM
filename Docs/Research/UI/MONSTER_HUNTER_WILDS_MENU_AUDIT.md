# Monster Hunter Wilds menu research and Arc and Dust audit

Research date: 2026-10-07. Status: provisional reference and proposed direction. This does not approve a menu redesign, new gameplay, or implementation.

The user's later [menu system direction](../../Design/Gameplay/MENU_SYSTEM_DIRECTION.md) controls scope: a simple supporting layer for radial setup, settings, and exiting. The broader menu family below is reference material, not a build target.

Monster Hunter Wilds is a useful reference for a substantial preparation interface that still belongs to a physical world. Arc & Dust should borrow its layered information, consistent selection feedback, and separation between quick actions and detailed management. The immediate engineering priority is shared screen navigation and input ownership; visual polish should grow on that foundation.

## Reference evidence

Capcom's [official manual](https://manual.capcom.com/mhwilds/) and its [English content data](https://manual.capcom.com/mhwilds/locale/data/en.json) document the title menu, inventory, crafting, contextual HUD, and shortcut controls. The JSON was retrieved directly from Capcom during this audit because the web text reader could not access the manual. Manual screenshots may differ from the final product; these references do not establish behavior in every current patch.

The visual observations below come from direct inspection of Capcom's [crafting list screenshot](https://manual.capcom.com/mhwilds/locale/en/page/103_3_1.jpg) and [item pouch screenshot](https://manual.capcom.com/mhwilds/locale/en/page/101_3_1.jpg). They are design interpretation of those screens, rather than measured timings or an exhaustive hands-on audit.

| Pattern | Observed or documented behavior | Proposed application |
| --- | --- | --- |
| Material and world continuity | The screenshots use translucent brown surfaces, fine decorative borders, warm text, and visible world imagery behind information. | A weathered field-instrument language: soot and iron surfaces, bone text, restrained amber focus, original etched geometry. Keep texture out of text areas. |
| Clear current location | The crafting screen shows a breadcrumb above its content. | Show screen title and parent location on deep management screens; return to the actual previous screen. |
| Browse and inspect | Crafting places a list on the left, required materials on the right, and selected-item information below. | Keep inventory or recipe browsing distinct from details and consequences. Update details on selection without executing actions. |
| Strong focus | A bright horizontal highlight, marker, and icon emphasis identify the selected crafting row. | Use a consistent outline or bar plus text/icon emphasis. Distinguish focus, selected transfer source, equipped state, and unavailable state. |
| Categories and filters | The crafting screenshot includes icon categories, filter labels, and page navigation. | Give tabs readable names as well as icons; introduce filters when content volume warrants them. Favor scrolling and stable selection over arbitrary page breaks. |
| Fast and deliberate layers | The manual separates item use, shortcuts, inventory inspection, and crafting. | Retain the quick radial; reserve full screens for preparation and comparison. |
| Contextual HUD | The Screen Layout topic describes health/stamina expanding in relevant situations, an always-expanded option, and contextual control guidance. | Keep critical survival warnings available; allow compact and expanded HUD modes. Do not hide attrition information just because the player is outside combat. |

Capcom describes controller shortcuts selected with the right stick while holding the bumper, with stick release activating the shortcut. Keyboard shortcuts use a separate numbered-key layout. The [PlayStation starter guide](https://www.playstation.com/en-ie/games/monster-hunter-wilds/monster-hunter-wilds-starters-guide/) documents up to eight customizable radial menus with twelve entries each. These capacities are reference facts, not targets for Arc & Dust.

The manual also distinguishes settings shared across saves from character progress, limits some settings to the title screen or gameplay, and describes keyboard configuration separately from limited controller reassignment. Arc & Dust can improve discoverability by using the same settings categories at both entry points and explaining any unavailable option beside it.

## Strengths and tradeoffs

Wilds makes detailed preparation feel connected to the world through materials, restrained frames, and background continuity. Its list/detail composition supports comparison without constantly opening another screen. The quick-action layer lets experienced players use familiar actions without repeatedly traversing the full hierarchy.

The screenshots also expose tradeoffs. Translucent panels compete with bright scenery. Compact category icons and several quantity symbols demand learned vocabulary. Nested submenus can make common actions harder to find. A large shortcut capacity increases configuration and memory demands. These are design risks inferred from the references, not measured usability failures.

For Arc & Dust, use an opaque-enough backing behind important text, named categories, explicit quantity meanings, and visible unavailable reasons. Common actions should be direct; destructive or resource-spending actions should communicate their result before confirmation. Animation must never delay selection or command acceptance. Avoid importing Wilds' quest, multiplayer, camp-storage, or auto-crafting rules into this game's survival design.

## Current production UI audit

Audit scope: live files in the active TopDown3D runtime, input asset, package manifest, and relevant test sources. Current checkout is `main` tracking `origin/main`. This is a source audit; it does not establish current Unity rendering, physical-controller comfort, or passing test execution.

| Area | Live evidence | Finding |
| --- | --- | --- |
| UI technology | `Packages/manifest.json`: uGUI 2.0.0, Input System 1.19.0; runtime canvases use `UnityEngine.UI`. | An existing production foundation can be extended without changing packages. |
| Input separation | `TopDown3DInputRouter.cs`: Gameplay, Inventory, Radial, Disabled modes; action-map switching, cursor ownership, gameplay-intent clearing, prompt-device tracking. | Preserve this authority. The Inventory mode currently also serves radial customization; more screens need a general screen-context contract. |
| UI event routing | `UI/TopDown3DInventoryUiSceneInstaller.cs`: scene-scoped EventSystem checks and InputSystemUIInputModule wiring. | Useful existing guard. Future persistent UI plus additive scenes needs a single application-level owner rather than independently installing competing systems. |
| Inventory and cargo | `UI/TopDown3DInventoryUiController.cs`: details, remembered slot selection, move/merge/swap, cargo range checks, refresh on state changes. | Reuse domain services. Cargo must remain tied to the Legger's real physical presence. |
| Screen transitions | Inventory Close and OnDisable directly request Gameplay; radial customization directly closes inventory, enters Inventory mode, and reopens inventory on finish. | Adequate for the current small flow, but fragile when a confirmation dialog, settings screen, or pause overlay is stacked above it. A closing child must restore its parent rather than always resume gameplay. |
| Visual language | Inventory, slot, radial, and feedback views embed colors, font sizes, and layout values; several use builtin `LegacyRuntime.ttf`. | Palette already trends toward warm industrial colors. Shared tokens and reusable components are missing from the inspected views. |
| Scaling | Inventory handles `Screen.safeArea`, uses a 1920 by 1080 reference, fixed grid columns, and fixed panel dimensions; radial preferences include scale. | Safe-area handling is present. A complete text-scale and responsive-layout policy is still needed; increasing canvas scale alone does not prove readable text or prevent clipping. |
| Preferences | Radial controller uses `radial-layout-v1.json`, validation, drafts, temporary-file write and replace, and retained drafts on save failure. | Preserve these semantics and stable page IDs. General settings need their own versioned schema and migration policy. |
| Coverage | `TopDown3DInventoryUiTests.cs` and `TopDown3DRadialTests.cs` contain input, prompts, geometry, selection, preference, and handoff checks. | Existing tests are useful structural evidence. Shared screen stack, settings lifecycle, and responsive/localized presentation require future targeted coverage. |

No general title/pause/settings screen stack, rebinding persistence implementation, or UIDocument-based menu system was found in the inspected production runtime. This does not imply all older UI work is absent from archives. Archives remain outside the production lane.

The [existing radial research](./RADIAL_MENU_UI_CONTROLLER_RESEARCH.md), [system design](../../Design/Gameplay/RADIAL_MENU_SYSTEM_DESIGN.md), and [polish audit](../../Design/Gameplay/RADIAL_MENU_POLISH_AUDIT.md) remain the detailed radial references. Their browser-preview proof must not be promoted to proof of current Unity presentation. Live code controls implementation facts where an earlier proposal differs.

## Proposed menu family

| Surface | Purpose | Initial scope |
| --- | --- | --- |
| Title | Start or resume a session; accessibility and settings before play. | Continue/New/Load only when backed by actual session/save services; Settings, Credits, Quit as available. |
| Pause and system menu | Resume, settings, controls/help, return/quit with progress consequences. | A compact list with consistent back behavior. Pause policy needs a deliberate product decision. |
| Field kit | Inspect and manage Booter's inventory and reachable Legger cargo. | Refine the existing working screen before introducing new categories. |
| Quick radial | Access frequent field commands. | Preserve current assignments and cancellation semantics; do not expand capacity just to match Wilds. |
| Settings | Controls, interface/accessibility, audio, display/graphics. | Only expose settings with working owners; group consistently at title and in play. |
| Future preparation | Equipment, crafting, trade, journal, map. | Reserve navigation seams; build each screen only with its approved gameplay system. |

Proposed composition for deeper menus: screen title and breadcrumb at the top, named categories beneath, primary list or grid to the left, selected details and consequences to the right, and current-device actions in a consistent footer. Small confirmation dialogs sit above the originating screen and return focus to its initiating control.

The visual proposal is an industrial field ledger suited to [WORLD_BASIS](../../WORLD_BASIS.md): dark iron, warm readable text, subtle wear and etched framing, amber for focus, and distinct warning marks with text. The orange world makes solid backing and contrast important. Keep traversal HUD sparse; let opened management screens carry the detailed information. Final fonts, icons, texture, palette, sound, and motion require visual review and are not accepted canon.

## Proposed engineering foundation

Retain uGUI for the first slice. This follows the existing code and Unity 6.4's [UI-system comparison](https://docs.unity3d.com/6000.4/Documentation/Manual/UI-system-compare.html), which recommends uGUI for runtime and lists UI Toolkit as an alternative. Framework replacement would add migration work without resolving ownership or navigation on its own.

1. **Screen coordinator:** owns open/close/back, layered dialogs, restoration of the underlying screen, and focus history. Separate screen IDs from input contexts so Settings does not masquerade as Inventory.
2. **Input and simulation policy:** the coordinator requests input contexts from the existing router. One policy owner controls cursor and simulation pause. UI animations use unscaled time if pause is adopted. Cancel goes only to the top eligible layer and must not also close its parent.
3. **Reusable presentation:** shared theme, typography, spacing, panels, rows, tabs, item tiles, prompts, dialogs, and toast styles. Prefer authored reusable prefabs for stable screen structure; retain runtime population where content is dynamic.
4. **Navigation and prompts:** one EventSystem/InputSystemUIInputModule authority, deterministic directional navigation, stable selection identity, visible focus, pointer/controller switching without losing focus, and prompts derived from current bindings. Unity's [Input System 1.19 UI documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html) describes supported integration.
5. **Settings service:** separate global preferences from character/world saves. Use working-copy editing, Apply/Cancel/Restore Defaults, explicit failure feedback, and versioned migration. Risky display changes should have a timed revert. Preserve existing radial preferences until migration is explicitly designed.
6. **Domain commands:** views request operations from inventory, cargo, and other approved services. Services validate availability at execution time, including Legger distance changes while a screen remains open. Menus never become remote storage or teleport controls.

## Accessibility and resilience

Build text sizing and layout reflow together. Use [Xbox Accessibility Guideline 101](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101) for text and [Guideline 102](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102) for contrast; target at least 4.5:1 for ordinary important text. Measure final blended surfaces, including focus and unavailable states. Do not treat the earlier radial browser-preview measurements as certification of the game.

Provide high-contrast backing, redundant shape/text state cues, reduced motion, readable device prompts, controller-only operation, and alternatives to sustained holds where appropriate. Preserve already implemented radial interaction options. Plan font glyph coverage, localization expansion, right-to-left layout requirements if relevant, and safe fallback when a device disconnects.

Verify narrow/aspect-ratio layouts and large text, empty lists, long item names, full inventories, changing cargo access, dialogs above inventory, device switching, failed settings writes, corrupted preferences, scene transitions, repeated open/close, focus loss, and held-button release on screen changes. Large lists should update changed entries rather than rebuild the whole hierarchy on every navigation input.

## World and persistence boundaries

- **Deterministic world identity:** UI routes identify screens and domain entities; they do not change seeds or world identity. World/session IDs remain owned by world services.
- **Streaming and unload/reload:** Greater Wasteland is fixed and always loaded. Streaming implementation is out of scope. Keep future views able to invalidate an unavailable entity and clear subscriptions without retaining destroyed scene references.
- **Generated-object identity:** no generation work is proposed. Domain commands should use existing stable item/object IDs; list positions and Unity instance IDs must not become persistent identities.
- **Authored constraints:** maintain physical cargo access and existing placement validation. A menu cannot bypass distance, terrain, availability, or capacity checks.
- **Persisted runtime deltas:** inventory/cargo/action services own gameplay mutations. UI preferences remain separate. Do not import the generated prototype's save service into Greater Wasteland.

## Recommended first slice and review gates

Following the user's clarified direction, first review a compact supporting menu and simple radial setup flow. Material styling can borrow from these references without requiring a field-ledger interface. Review the existing Field Kit only when it serves a concrete inventory task; include readable text and controller focus.

After visual direction is selected, implement shared navigation around the existing Field Kit plus one settings screen and one confirmation dialog. Prove parent restoration, single cancel consumption, input suppression, focus recovery, and unavailable cargo behavior before expanding the menu family. Decide explicitly whether full menus pause the world; disabling player input alone is not a simulation pause.

Future validation should use focused state/input tests and controlled layout captures. Hands-on gameplay smoke testing remains user-owned. This research changed documentation only and did not launch Unity, modify gameplay, change packages, or run tests.
