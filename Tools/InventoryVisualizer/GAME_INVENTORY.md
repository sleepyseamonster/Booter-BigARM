# Game inventory study

Open **Game inventory** in the visualizer's top navigation, or visit `http://127.0.0.1:8087/#game`. The existing launcher and server serve the new files without a restart. `game.html` also works as a standalone study.

This tab is separate from the catalogue/review/artwork workspace. Its iframe, JavaScript module, model and stylesheet are independent; the shared entry points only add navigation and lazy loading. It reads `/api/catalog` and never calls a mutation endpoint. No master item, review choice, artwork record, Unity asset or gameplay save is edited.

## Included interactions

- Booter's six designated equipment spaces and four supply pockets; resources and filled dust canisters are rejected. The living algae attachment cannot be removed.
- Legger's twelve cargo mounts and 92 hard mass limit, including contained dust; the Camp Loop chest's 24 spaces. These are study defaults, not new production balance.
- Selected possession details, owner transfers, stack merging, searchable/filterable/sortable manifests, independent favourites and protection. Sorting changes the view only. Protection excludes transfers and recipe consumption in this study.
- Explicit Legger/chest/station proximity controls. Distant belongings remain inspectable; transfers and crafting check access again.
- Two explicit Camp Loop operations: four chest ore make an empty battery; ten dust in one accessible canister plus an empty battery make a charged battery. Outputs require a free Booter pocket. Failed operations preserve all inputs. Refining retains the canister identity and leftover contents.
- Tracked additional output quantities, combined direct requirements, accessible versus known stock, shortages, and progress reduced on successful crafting. No automatic intermediate crafting or resource reservation.
- Read-only reference search over the full current catalogue, known locations and links to the two supported operations. Up to 100 search matches are shown; narrow the search to find more. Other design routes are not executable.
- Game menu, shortcut configuration, larger text and reduced motion, explicit two-step sample reset, and return to the existing workspace. Keyboard navigation uses native buttons and controls; Escape opens the game menu and closes dialogs, `/` focuses search, `I` opens inventory. No gamepad integration is claimed.

Sample ownership uses exact master IDs, but is fictional browser state. Repeated bulk units use counts; equipment uses individual study IDs. Glyphs are placeholders. Algae inspection creates no reserve or survival effect. The refinery applies an atomic direct conversion for interface exploration; physical Unity sockets, equipment deployment, timed collection and survival actions are not simulated. Booter equipment replacement requires storing the fitted piece before fitting a spare; atomic swap UI and world actions are future extensions.

## Persistence and scope

Possessions, projects, access toggles and shortcuts use localStorage key `arc-dust-game-inventory-v1`; accessibility preferences use `arc-dust-game-settings-v1`. They are independent of item-review protection, the artwork IndexedDB and existing appearance preferences. State is scoped to browser profile and origin/port. A malformed stored save is preserved and automatic saving is blocked until explicit reset. Storage failure leaves the session usable with a notice. Multi-tab conflict coordination and portable save export are not implemented.

Catalogue data is fetched on study load; reload the study to pick up workbook changes. Missing definitions retain their stored IDs rather than deleting possessions. This tab is not a production inventory migration or a complete recipe database.

No terrain, world objects, chunk streaming, generated-object identity or production runtime deltas are involved. The study's stable instance IDs identify local sample ownership only. Greater Wasteland remains fixed and always loaded; Unity inventory/snapshot versions are unchanged.

## Verification

Run `node --test Tools/InventoryVisualizer/test_game_inventory.mjs` for quantity conservation, access/protection rejection, carrier rules, identity, full-output rollback, contained-dust conversion/mass and malformed save handling. This verifies the browser model, not Unity gameplay. Browser interaction and responsive layout checks are separate.

October 15, 2026: all seven model checks pass. JavaScript syntax checks pass for both the new module and shared entry module. Background browser review verified the new tab, chest access blocking, tracked-project completion, reload persistence, protection, a six-ore transfer merging into the existing chest stack, reference search, shortcut setup and return to the existing item workspace. At a 520px viewport, neither the parent page nor the embedded study had horizontal overflow. Browser warning/error capture was empty. No Unity launch, gameplay smoke test, gamepad trial, production save migration or workbook mutation was performed by this task.
