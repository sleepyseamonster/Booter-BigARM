# Prototype UI Plan

This document defines the current UI decision for the prototype and the later GUI work that will replace it.

## Current State

- The prototype uses immediate-mode HUD overlays via `OnGUI`.
- The current overlays are intentionally lightweight and are good enough for the prototype loop.
- No Canvas-based HUD is required yet.
- No special GUI layer setup is required for the current overlays.

## What Stays For Now

- Survival status HUD.
- Inventory HUD.
- Harvest prompt HUD.
- Debug overlay for prototype diagnostics.

## Why This Is Acceptable Now

- The current priority is gameplay seam work, not presentation polish.
- The prototype HUD only needs to communicate critical state, not final layout quality.
- Keeping the HUD simple avoids overbuilding UI before the loop is stable.

## Later GUI Pass

- Replace `OnGUI` overlays with a proper Screen Space Overlay Canvas.
- Move to explicit UI objects, layout groups, and reusable prefab-based widgets.
- Add a UI layer and EventSystem only when the project actually needs richer GUI behavior.
- Preserve the current information hierarchy: survival, inventory, harvest, debug.

## Implementation Rule

- Do not spend time converting the prototype HUDs to Canvas/UI until the gameplay loop needs it.
- Treat the current overlays as temporary but valid.

