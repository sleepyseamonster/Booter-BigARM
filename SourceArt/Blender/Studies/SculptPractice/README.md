# Minimal sculpt practice

Open `MinimalSculpt.blend` through `Open-Sculpt.ps1` (right-click the script,
Run with PowerShell). Blender starts minimized; select it from the taskbar.
The launcher uses standard Blender shortcuts and enables pen navigation for
this session only. Existing preferences and startup files are not overwritten.

One smooth sphere, Sculpt mode, maximized viewport, clay matcap, no camera,
lights, timeline, sidebar, toolbar or persistent brush shelf. The top brush
controls remain available. Draw uses pressure for strength and a steady radius.
X symmetry starts on; toggle X in the top bar for asymmetric sculpting.
The sphere has 163,842 vertices / 327,680 triangles; Dyntopo is off.

## Keep these nearby

| Action | Shortcut |
| --- | --- |
| Sculpt | Pen tip drag |
| Smooth | Shift + pen drag |
| Reverse brush / carve | Ctrl + pen drag |
| Brush size | F, move pen, tap to confirm |
| Brush strength | Shift + F, move pen, tap to confirm |
| Choose brush | Shift + Space; choose Draw, Clay Strips, Grab or Crease |
| Orbit | Alt + pen drag |
| Pan | Alt + Shift + pen drag |
| Zoom | Alt + Ctrl + pen drag |
| Undo / redo | Ctrl + Z / Ctrl + Shift + Z |
| Save / save a new copy | Ctrl + S / Ctrl + Shift + S |

Only two menus are needed: the brush thumbnail at the top (or Shift + Space)
for brushes, and File for saving. T reveals extra tools; N reveals the sidebar;
Ctrl + Space restores the surrounding editors. Hover over the viewport for
viewport shortcuts. Pen side buttons can be mapped to middle mouse (orbit)
and right mouse (context menu) in your tablet driver if preferred.

Tablet pressure requires a functioning manufacturer driver. The launcher uses
Blender's Automatic tablet API. If pressure does not respond, check Edit >
Preferences > Input > Tablet API and match Windows Ink or Wintab to the driver.
That hardware response still needs a real pen test.

## Scope and verification

Created with Blender 5.2.2 LTS on 2026-10-08. This is a freeform practice source,
not a production model or approved game design. No export or Unity import is
performed. Deterministic world identity, chunk streaming/unload, generated-object
identity, authored gameplay constraints and persisted runtime deltas are all
inapplicable: this file contains only an independent sculpt sphere.

Source generation: `Tools/Art/Blender/create_minimal_sculpt.py`.
The file is reopened in background to verify geometry, Sculpt mode, brush,
pressure configuration, and saved viewport state. Tablet feel is user-owned.

Shortcut references: installed Blender 5.2 default keymap and the official
[brush manual](https://docs.blender.org/manual/en/5.2/sculpt_paint/brush/introduction.html).
