# Compact inventory and character-viewer review

October 15, 2026. Browser-only interface iteration requested by the author. Supplied ARC Raiders and Starfield screenshots informed density, fixed equipment addresses and the secondary character panel. No reference-game images were copied into the tool.

## Final captures

- [1280 × 720 embedded visualizer](inventory-1280.jpg): six Booter gear slots, four supply pockets, character rotation controls and item inspector.
- [1920 × 1080 layout](inventory-1920.jpg): standalone rendering of the same game-inventory module.
- [Rear gear inspection](character-back.jpg): existing worn cloak highlighted at 162° after selecting its actual equipment slot.

The character is the project's existing Unity Standard Humanoid prototype, exported in a neutral standing pose from a temporary Blender scene. The cloak uses its existing mesh. Neutral shading and the other gear proxies remain placeholder presentation, not final Booter art. Source/licence, binary geometry and content hashes are recorded in [the preview metadata](../../../../Tools/InventoryVisualizer/dist/preview/character.json). Export touches neither original FBXs nor Unity scenes, metadata or Blender sources.

## Iterations and evidence

Visual inspection of the first candidate exposed an extended arm, off-center gear and clipped rotation controls. The final export uses arm-branch skin weights and shoulder/elbow joint positions, retains a centered 1.8m inspection height, and shortens the viewer panel. A later narrow-screen review exposed inherited navigation ordering; the top tab row now stays above the content. These failed candidates are not acceptance evidence.

Final browser checks established:

- All six equipment and four supply addresses remain visible together at the desktop sizes. The embedded 1280 view's supply row ended above the sticky footer.
- Rotate buttons turn in 30° steps; six presses produce a 162° back view. Horizontal drag changed 342° to 34°. Selecting another slot retains rotation and highlights the matching fitted gear.
- A 520px layout has no document horizontal overflow, while the tab bar and map can scroll within their own regions.
- Switching to the original item workspace removes the compact-host class and restores its chrome. No catalogue/review/workbook command is issued by the game prototype.
- The synthetic browser fixture still passes shared bumper/trigger tab cycling and wrap, offscreen D-pad focus reveal, right-stick free scrolling and return to navigation. This is not physical-controller acceptance.
- Browser warning/error capture was empty in the final reviewed page.

`node --test Tools/InventoryVisualizer/test_character_preview.mjs Tools/InventoryVisualizer/test_gamepad.mjs Tools/InventoryVisualizer/test_game_inventory.mjs` passes **21/21** checks. The preview checks cover finite geometry, complete triangle ranges, lowered-arm/centering/height bounds and exact source hashes. JavaScript syntax checks pass for the changed modules and shared entry point.

Greater Wasteland, its fixed terrain, gameplay ownership, snapshot versions, packages and save data are unchanged. The viewer owns no world objects, generation identity, streaming cells or gameplay deltas. No Unity launch, gameplay smoke test, Player build, final-art acceptance, physical-controller trial or external publication is claimed.
