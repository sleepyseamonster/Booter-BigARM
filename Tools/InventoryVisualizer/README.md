# Inventory interface study

Local browser prototype for fast iteration on Arc & Dust's Unity menu and inventory presentation. Run from this checkout:

```powershell
& .\Tools\InventoryVisualizer\Start.ps1
```

Open **http://127.0.0.1:8087**. Stop with Ctrl+C in the server terminal. Optional launcher arguments: `-Port 8088` or `-Python 'absolute/path/to/python.exe'`. The launcher prefers the installed Codex Python runtime; another Python requires `openpyxl`. No npm installation, build, Unity launch, or package change is needed. The server binds only to loopback. Source edits appear after browser refresh.

## Current proposal

- Compact entry menu, category rail, item grid/list, and persistent inspection panel.
- Reads the `Items` sheet of [the master workbook](../../Docs/Design/Items/MASTER_ITEM_DATABASE.xlsx) on page load; refresh to read workbook changes. No exported roster to maintain. Cached formula values must already have been calculated in the workbook.
- Search spans source fields, including aliases, uses and source references. Search results may include related definitions as well as an exact name. Filters combine category, material tier, source design status and artwork state. Tier is not rarity. Missing mass, stack and rarity values remain undefined.
- Booter view sketches the six equipment spaces and four-pocket experiment from [the inventory brainstorm](../../Docs/Design/Gameplay/CAMP_LOOP_INVENTORY_REDESIGN_BRAINSTORM_2026-10-14.md). Its filtered library suggests broad equipment/supply categories, not approved slot compatibility. Legger view is a cargo browsing study, not actual carried contents. No equip, transfer, mass, capacity or physical-access simulation is implemented.
- Appearance controls compare worked iron, red oxide and mineral chalk; adjust tile size, text size and subtle generated surface grain. Preferences persist in this browser.
- Artwork studio is secondary. Category silhouettes are explicitly labelled placeholders; no item artwork has been generated. Optional PNG/JPEG/WebP attachment, notes, art status, queue, draft prompt copy, and export/import support later visual iteration. Inventory hides those editing controls.

The quiet metal/mineral treatment is a proposal informed by [Sable's refined B–C study](../../Docs/Agents/Sable/work/2026-10-15-material-dioramas/refined_BC/README.md). The menu reads the existing workyard E review JPEG directly through a dedicated endpoint; it does not modify or duplicate the texture agent's files. The backdrop is an offline Blender render, not a Unity capture or new texture.

Artwork records are keyed by exact master item ID in IndexedDB; appearance preferences use localStorage. Browser profile, origin and port determine this storage, and clearing browser data removes it. Export workspace downloads a portable JSON backup including images and notes. Import merges recognized item IDs, replacing matching art records; unrelated records remain. Unknown IDs are skipped. Appearance is included in exports as a reference, but import currently restores only artwork records. Uploaded files are never written to the repository or master workbook. Attachment limit is 10 MB per image; workspace import limit is 100 MB. Avoid very large source images for icon studies.

## Scope and handoff

This is browser presentation tooling under `Tools`, outside Unity's import boundary. It follows the accepted [menu direction](../../Docs/Design/Gameplay/MENU_SYSTEM_DIRECTION.md) as context; its appearance and screen composition remain proposals. Controller integration, Unity UI implementation, actual inventory transactions and creative acceptance are separate work. Browser keyboard support covers Tab focus, arrow movement within the grid, `/` search and Escape back to menu. This does not establish Unity or gamepad parity.

Deterministic world identity, terrain/chunk streaming, generated-world objects and persisted gameplay deltas are not applicable: the tool creates no world objects, reads no live save, and issues no Unity commands. Existing master item IDs identify browser art records only. Greater Wasteland's authored constraints and always-loaded terrain remain untouched.

## Verification, October 15, 2026

- JavaScript syntax and Python syntax checks pass; HTTP page, stylesheet, module, catalog and reference-image endpoints return successfully.
- Workbook adapter reads 827 unique IDs across 17 categories, preserving source field values. Workbook SHA-256: `0f487b3be371d543d595ca80c75b0555670ef0d4de50e347e6e7709a019a997f` at implementation.
- Background browser checks covered menu/inventory navigation, category/tier intersection (67 mineral resources; 66 at T0), search, empty results, reset, item selection, Booter's six spaces and appearance persistence through reload. At 520px the document had no horizontal overflow. No captured browser warnings/errors during those checks.
- No gameplay smoke tests, Unity tests, new art generation or production changes. Image attachment and workspace round-trip have not been exercised in browser QA in this UI-focused slice.
