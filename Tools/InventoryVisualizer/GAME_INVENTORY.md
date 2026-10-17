# Game inventory study

Open **Game inventory** in the visualizer's top navigation, or visit `http://127.0.0.1:8087/#game`. The existing launcher and server serve the new files without a restart. `game.html` also works as a standalone study.

This tab is separate from the catalogue/review/artwork workspace. Its iframe, JavaScript module, model and stylesheet are independent; the shared entry points integrate navigation, lazy loading and scoped host chrome. It reads `/api/catalog` and never calls a mutation endpoint. No master item, review choice, artwork record, Unity asset or gameplay save is edited.

## Unified crafting — October 17, 2026

The author combined Camp and Crafting into one **Crafting** top-level tab. Open **Game inventory → Crafting**, choose **Crafting** in the Game menu, or interact with the camp item. Direct entry is `http://127.0.0.1:8087/game.html#crafting`; the previous `#camp` entry remains compatible.

Crafting now uses the former camp hub and opens directly to **Workstations**. Its sidebar retains Overview, Item box, Booter equipment, Legger cargo, Tracked projects and Item reference. The separate two-recipe crafting screen is removed; every item/reference/project recipe link selects the matching station in this single interface. The title, service breadcrumb, Game-menu entry and close action use Crafting. Camp Loop remains the sample location. Escape/East backs from a service to Overview, then closes Crafting to the prior top-level page. There are seven top-level destinations; controller bumpers/triggers cycle that row.

The inventory model, recipes, access checks, sample save and Unity files are unchanged. Module syntax and all 22 existing camp/inventory/controller checks pass. Browser verification passed for a single Crafting tab, default workstation view, a battery recipe link after previously selecting Dust refinery, Game-menu entry, item-box/workstation navigation, back/close and both old/new direct URLs. [Current Crafting screenshot](../../Docs/Evidence/UI/CampMenu20261017/crafting-unified.png). Earlier sections and screenshots retain the previous layout's research and verification history.

## Inventory sub-tabs — October 17, 2026

The author requested a single **Inventory** top-level destination with **Booter** and **Legger** loadout sub-tabs. Each carrier retains its selected item; the Inventory page remembers its active carrier across other game pages and camp service visits for the current page session. This navigation state adds no saved ownership or schema changes. Left/Right and Home/End switch the focused sub-tab; controller users can select either sub-tab through ordinary focus/South. Bumpers and triggers continue cycling only the top row. Camp retains its existing direct equipment/cargo services.

Browser verification passed for both carrier views, arrow/Home/End navigation and returning to the selected Legger loadout after visiting Booter equipment in camp. Existing 22 camp/inventory/controller checks and module syntax checks pass. [Current inventory screenshot](../../Docs/Evidence/UI/CampMenu20261017/inventory-subtabs.png). Earlier screenshots below preserve the previous top-row layout.

## Pop-up camp menu — October 17, 2026

This original camp hub now appears under **Crafting**, as described above. Its previous `http://127.0.0.1:8087/game.html#camp` entry remains compatible.

This implements the author's narrowed browser/menu-only request. The Camp Loop Unity scene and runtime are unchanged. The browser remains the design workspace for camp and eventual town menus; a complete town economy, vendors and additional production recipes are not implemented here.

The persistent camp service menu contains Overview, Item box, Booter equipment, Legger cargo, Workstations, Tracked projects and Item reference. Inventory, inspection, transfers, protection, crafting and tracking use the existing sample ownership and transaction functions. Opening a menu never changes proximity or supplies resources. Escape/East backs from a service to camp overview, then leaves camp for the prior game tab. Escape outside camp opens the existing Game menu. Modal dialogs retain their existing close behavior. Bumpers/triggers cycle the seven top tabs; camp service buttons are separate from that cycle.

Workstations includes every current catalogue definition in **Crafting stations** (11 at verification), plus a Cooking station menu address when its definition is absent. The cooking fallback follows [the author-directed station brief](../../Docs/Design/Items/CAMP_CRAFTING_STATION_PROPOSALS.json); it does not create an item, inventory instance or recipe. Workbench and Dust refinery execute the two existing sample operations. Ironstone refinery, Cooking and proposal facilities open inspectable details with unavailable recipe actions. No new costs, yields, construction, food effects, power sources or crafting routes are invented. The active station ID and recipe resolve together if a catalogue definition is missing, and overview readiness counts only recipes with available station menus.

Navigation and selected workstation are transient UI state. Existing save version, localStorage keys, instance IDs, access checks, protected-input rules, canister contents and transaction rollback remain unchanged. No world generation, terrain streaming, chunk unload/reload, placed-object identity or persisted Unity deltas are involved. The service records/rendering are isolated in `dist/game-camp.mjs`; `dist/game-camp.css` scopes their presentation. Town services can later reuse the same service/content boundary after their rules are defined.

### Research and audit

| Evidence | Design consequence |
| --- | --- |
| [Capcom's Gamescom overview](https://blog.playstation.com/2024/08/20/monster-hunter-wilds-all-the-news-from-gamescom-opening-night-live/) describes pop-up camps as field access to item/equipment management and quest preparation; its base camp also hosts smithing and purchasing. | Use one preparation hub, with room for broader town facilities later. Quest and shop systems are not introduced in this slice. |
| [PlayStation's Wilds handbook](https://www.playstation.com/en-us/games/monster-hunter-wilds/monster-hunter-wilds-starters-guide/) describes camp preparation, customizable radial menus, and crafting from stored supplies while in a tent. | Keep storage and crafting close together, with direct service navigation and visible stock/access requirements. |
| [The community Tent reference](https://www.monsterhunterwiki.org/wiki/MHWilds/Tent) inventories item transfer/crafting, equipment, companion, meal/rest and other tent groups. It is secondary evidence, not a verified current game capture. Capcom's linked online manual could not be fetched during this research. | Borrow the grouped-service structure while retaining Arc & Dust's terminology, palette and established systems. Do not copy Wilds' unrelated systems or assume exact current bindings. |
| Browser audit: `game.html` had eight peer tabs; `game.js` dispatched views centrally; `game-model.mjs` owned two atomic recipe routes and finite carrier rules. | Add Camp as the ninth tab and compose existing views inside its shell instead of maintaining a second inventory. |
| Browser audit: the controller adapter preserves logical focus by control addresses and includes all visible `nav [data-page]` controls in tab cycling. | Give camp controls distinct attributes/stable IDs, retain focus after rebuilds, and add East/back to the existing neutral/release-gated adapter. |

The original SVG camp illustration is an interface placeholder, not approved source art or a Unity scene capture. No Monster Hunter assets are bundled. Desktop stations use a list/details layout; at 520px and below, a labelled station selector keeps the selected details close to the control.

### Current verification

- `node --test Tools/InventoryVisualizer/test_camp_menu.mjs Tools/InventoryVisualizer/test_game_inventory.mjs Tools/InventoryVisualizer/test_gamepad.mjs`: **22 passed**. New checks cover missing-station fallback/readiness, proposal recipe exclusion, escaped catalogue text, protected/full-output UI blockers and East/back neutral gating. Existing transaction/identity/controller checks remain passing.
- Changed JavaScript modules pass syntax checks; Git whitespace checks pass.
- Background browser verification passed in standalone mode and the existing Game inventory iframe: all seven services, all twelve station details, camp-item/Game-menu entry, Escape back/exit, reference search, recipe links, tracking, battery crafting, dust charging, transfer, protection, out-of-reach blocking and reload persistence. Charging left three dust in the same canister. These actions affected only local sample possessions; access/text settings were restored after checking.
- All seven services had no document horizontal overflow at 520px. Mobile station selection and larger text were visually reviewed; desktop review used 1280 × 900. Console warnings/errors were empty. Physical controller acceptance remains unverified; mapping/timing checks are synthetic.
- Evidence: [camp overview](../../Docs/Evidence/UI/CampMenu20261017/camp-1280.png), [desktop workbench](../../Docs/Evidence/UI/CampMenu20261017/workbench-1280.png), [mobile workbench](../../Docs/Evidence/UI/CampMenu20261017/workbench-520.png). No Unity launch, import, build, gameplay smoke test or workbook mutation was performed.

## Compact inventory iteration — October 15, 2026

The current character panel uses a static, original SVG outline labelled **Empty character space**, following the author's subsequent direction. The 3D model and rotation controls are removed from the active page; no mesh or WebGL viewer is loaded. Gear slots and item inspection remain independent of this placeholder. The earlier viewer implementation and export sources below are retained for reference only.

Outline verification: the embedded browser view displays the placeholder with six equipment slots and four pockets, no canvas or rotation controls, and selecting Cloak updates the inspector. JavaScript syntax and all 18 existing inventory/controller checks pass.

The author's supplied ARC Raiders and Starfield screenshots set the hierarchy: compact fixed gear slots and visible items first, a standing character viewer second. No reference-game artwork is copied into this tool. The new compact stylesheet removes the oversized headings, serif treatment, verbose inventory explanations and large generic cards. Six named Booter equipment slots and four supply pockets appear together, with original SVG item silhouettes and a concise inspector. Legger cargo and the camp chest show finite grids of 12 and 24 spaces. The reference library uses a dense icon grid. Prototype settings retain the explanatory/developer controls.

The host navigation compacts only while Game inventory is active, removes the duplicate outer footer, and gives the embedded prototype the remaining viewport. Catalogue review, artwork and workbook editing retain their existing layout and storage. `/` opens Reference search from fixed-slot views.

The previous character panel used a local WebGL mesh viewer: horizontal pointer drag and focusable rotate/reset buttons turned the standing figure, slot selection highlighted its fitted gear, and approximate gear hit areas selected the matching equipped item. That retained implementation creates no world entities, gameplay identity, terrain, streaming, save migration or runtime deltas.

`export_character_preview.py` imports the existing Unity Standard Humanoid FBX and worn cloak into a hidden factory-startup Blender scene, bakes a neutral arms-down pose from the original arm-branch skin weights, and exports only `dist/preview/character.bin` and its provenance JSON. Source FBXs, metadata, Blender sources and Unity scenes are untouched. The existing character's [source and licence record](../../Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/README.md) remains controlling. The viewer's neutral material treatment and simple jacket/canister/grapple/tool proxies are presentation placeholders, not accepted final character/gear art. The cloak uses the project's existing mesh. Inspection hit areas are approximate, not exact mesh raycasts.

Re-export only through hidden/background Blender with `--factory-startup --disable-autoexec --python-exit-code 1 --python <this-folder>/export_character_preview.py -- <checkout-root>`; Blender 5.2.2 LTS was used. No Blender file is saved. `node --test Tools/InventoryVisualizer/test_character_preview.mjs` checks finite geometry, triangle ranges, standing/centering bounds and the unchanged source hashes. Run alongside the existing controller/inventory tests (21 checks total).

Historical viewer review found and corrected an extended-arm FBX pose, gear alignment, and clipped viewer controls. Its browser checks verified six equipment/four pocket addresses, button and drag rotation (342° → 34°), back-view cloak highlighting, and rotation retention during selection. These checks describe the previous viewer, not the current empty outline. Physical-controller feel and final character art remain separate acceptance.

## Included interactions

The sticky top menu has seven peer tabs: **Inventory**, **Crafting**, **Camp storage**, **Projects**, **Map**, **Reference**, and **Settings**. Inventory contains two loadout sub-tabs, **Booter** and **Legger**; Legger is no longer a separate top-level page. Inventory ownership is selected by these sub-tabs or the Camp service menu; the old left-hand owner switcher is removed. Each owner remembers its valid item selection during tab changes. The left panel summarizes the current carrier; crafting still has its station picker within the Crafting tab.

Top-menu follow-up, October 15, 2026: all eight pages were checked in the browser, including Legger ownership, map waypoint interaction, and Settings/radial/prototype controls. The synthetic browser fixture verified both bumpers and both triggers cycle the same top row and wrap at its ends, with the existing long-list navigation/right-stick scrolling checks still passing. The 18 automated inventory/controller checks pass, including clearance beneath the sticky menu. Hardware acceptance remains separate.

Map is an explicitly schematic mockup with Booter, Legger and camp markers, marker inspection, local waypoint/clear actions and zoom. It contains no real terrain, coordinates, distances, navigation or live tracking. Map selection, zoom and waypoints last for this page session only. Settings is a full top-level page with readability preferences, radial assignments and a collapsible Prototype controls section for access toggles/reset. Its controls use the existing independent browser preferences and sample state.

- Booter's six designated equipment spaces and four supply pockets; resources and filled dust canisters are rejected. The living algae attachment cannot be removed.
- Legger's twelve cargo mounts and 92 hard mass limit, including contained dust; the Camp Loop chest's 24 spaces. These are study defaults, not new production balance.
- Selected possession details, owner transfers, stack merging, searchable/filterable/sortable manifests, independent favourites and protection. Sorting changes the view only. Protection excludes transfers and recipe consumption in this study.
- Explicit Legger/chest/station proximity controls. Distant belongings remain inspectable; transfers and crafting check access again.
- Two explicit Camp Loop operations: four chest ore make an empty battery; ten dust in one accessible canister plus an empty battery make a charged battery. Outputs require a free Booter pocket. Failed operations preserve all inputs. Refining retains the canister identity and leftover contents.
- Tracked additional output quantities, combined direct requirements, accessible versus known stock, shortages, and progress reduced on successful crafting. No automatic intermediate crafting or resource reservation.
- Read-only reference search over the full current catalogue, known locations and links to the two supported operations. Up to 100 search matches are shown; narrow the search to find more. Other design routes are not executable.
- Game menu, shortcut configuration, larger text and reduced motion, explicit two-step sample reset, and return to the existing workspace. Keyboard navigation uses native buttons and controls; Escape opens the game menu and closes dialogs, `/` focuses search, `I` opens inventory.

## Controller controls

Use a browser-standard mapped gamepad and press a button while the visualizer has focus. The browser may require this before exposing the device. D-pad or left stick moves visible focus; South selects the focused control; East backs out. **LB/RB and LT/RT both cycle the same seven top-level tabs**, wrapping at either end. Neither pair changes tabs behind an open dialog. Workbench/Refinery selection uses ordinary focus and South within Crafting.

D-pad/left-stick navigation includes offscreen controls and scrolls the new focus into view with clearance below the sticky menu and above the sticky footer. The right stick scrolls vertically or horizontally without changing selection: a 0.22 dead zone removes drift, deflection scales speed up to 850 pixels/second, and frame time is capped at 50ms to prevent resume jumps. It scrolls the focused control's nearest scrollable pane, including the schematic map, or the study document; an open dialog confines scrolling to itself. Right-stick scrolling does not continually snap back to the focused item. Returning to navigation reveals focus again. Hidden/inactive study tabs and disconnected controllers cannot scroll the page.

South toggles edit mode on dropdowns and project quantity controls; Up/Down changes their value and South finishes. Text search still uses a keyboard. Native pointer and keyboard interaction remains available. Focus uses spatial navigation and scrolls into view, remains inside open dialogs, and recovers its logical address after a view rebuild. Stick threshold is 0.55; held directions repeat after 360ms, then every 125ms. South/bumpers/triggers fire once and require all bound controls to return to neutral. Triggers activate above 0.55.

Input is active only while this study is visible and the containing browser page has focus. Switching to the item-management workspace, hiding the browser page, disconnecting or changing controllers clears input and requires neutral before rearming. Nonstandard mappings are reported rather than guessed. No controls execute against Unity or the authoring workbook.

The implementation follows the [Gamepad standard mapping](https://w3c.github.io/gamepad/#remapping) and [browser API guidance](https://developer.mozilla.org/en-US/docs/Web/API/Gamepad_API/Using_the_Gamepad_API). Run `node --test Tools/InventoryVisualizer/test_gamepad.mjs` for mapping, dead zone, repeat, release, reconnect, inactive-state and spatial-navigation checks. Physical-controller acceptance remains separate from synthetic input checks.

Controller addition, October 15, 2026: seven synthetic controller checks and seven existing inventory checks pass (14 total). Both browser modules pass syntax checks; the updated study loads its controller status without browser warnings/errors. No physical controller was exposed during browser verification, so hardware mappings, stick feel and end-to-end controller acceptance remain unverified.

Scrolling follow-up: 18 automated controller/inventory checks pass, including offscreen navigation, footer clearance, proportional right-stick speed, frame timing and neutral rearming. Open `gamepad-scroll-check.html` on the local server for a disposable 40-item browser fixture using the real controller adapter and synthetic input frames. Its background browser run passed: navigation revealed item 23 above the footer; right stick scrolled 280px without changing focus or snapping back; the next navigation restored focus visibility. The fixture reads no catalogue or saved possessions and overrides controller/focus APIs only in its own document. Physical-controller feel remains unverified.

Sample ownership uses exact master IDs, but is fictional browser state. Repeated bulk units use counts; equipment uses individual study IDs. Glyphs are placeholders. Algae inspection creates no reserve or survival effect. The refinery applies an atomic direct conversion for interface exploration; physical Unity sockets, equipment deployment, timed collection and survival actions are not simulated. Booter equipment replacement requires storing the fitted piece before fitting a spare; atomic swap UI and world actions are future extensions.

## Persistence and scope

Possessions, projects, access toggles and shortcuts use localStorage key `arc-dust-game-inventory-v1`; accessibility preferences use `arc-dust-game-settings-v1`. They are independent of item-review protection, the artwork IndexedDB and existing appearance preferences. State is scoped to browser profile and origin/port. A malformed stored save is preserved and automatic saving is blocked until explicit reset. Storage failure leaves the session usable with a notice. Multi-tab conflict coordination and portable save export are not implemented.

Catalogue data is fetched on study load; reload the study to pick up workbook changes. Missing definitions retain their stored IDs rather than deleting possessions. This tab is not a production inventory migration or a complete recipe database.

No terrain, world objects, chunk streaming, generated-object identity or production runtime deltas are involved. The study's stable instance IDs identify local sample ownership only. Greater Wasteland remains fixed and always loaded; Unity inventory/snapshot versions are unchanged.

## Verification

Run `node --test Tools/InventoryVisualizer/test_game_inventory.mjs` for quantity conservation, access/protection rejection, carrier rules, identity, full-output rollback, contained-dust conversion/mass and malformed save handling. This verifies the browser model, not Unity gameplay. Browser interaction and responsive layout checks are separate.

October 15, 2026: all seven model checks pass. JavaScript syntax checks pass for both the new module and shared entry module. Background browser review verified the new tab, chest access blocking, tracked-project completion, reload persistence, protection, a six-ore transfer merging into the existing chest stack, reference search, shortcut setup and return to the existing item workspace. At a 520px viewport, neither the parent page nor the embedded study had horizontal overflow. Browser warning/error capture was empty. No Unity launch, gameplay smoke test, gamepad trial, production save migration or workbook mutation was performed by this task.

Palette correction, October 15, 2026: game inventory uses the regular inventory's charcoal surfaces, warm cream text and muted copper selection/focus accents. The palette stylesheet loads after the compact layout and applies across inventory, storage, crafting, map and menu views. Slot sizes, layout, character materials and sample-save behavior are unchanged.
