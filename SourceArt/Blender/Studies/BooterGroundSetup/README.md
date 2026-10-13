# Booter ground setup

Created October 12, 2026, for a local Blender scene containing the Unity player model standing on a freely resizable ground plane.

Open the local `BooterGroundSetup.blend` in this folder. The ground is selected, and the viewport sidebar is open. Under **Item > Dimensions**, change **X** and **Y** in metres, or use **S**, **S X**, and **S Y**. Keep ground Z position at zero. Move `Booter - move character here` to move the character and its retained source parts together.

Booter's body comes from the production Unity `defaultmale_rig.fbx`, evaluated with the current `MCU_am_Stand_Idle_01` clip at 0.1 seconds and the production visual scale of 1.4. It is a frozen, editable standing mesh, not a Blender animation rig. UVs and material base colors are retained. Unity URP shading is approximated by Blender materials. Unposed cloak/backpack source meshes are preserved in a hidden collection; they require separate fitting and are excluded from the visible setup.

The initial plane is 10 by 10 metres. One Blender metre equals one Unity unit. The body is translated vertically so its lowest point is exactly at Z=0. The camera and lights support a preview without cluttering the viewport. `START HERE - Adjustable Ground` inside the file contains the same editing instructions.

## Creation and verification

[Unity snapshot script](../../../../Tools/Art/Blender/export_booter_standing.cs) runs through the installed Unity Pipeline `eval_file` command against this checkout. It uses a disposable preview scene, samples the idle clip, exports the mesh snapshot into ignored `Logs/BooterGroundSetup/booter_standing.json`, and destroys its temporary objects and animation graph. It does not open or save production scenes. It refuses to overwrite an existing snapshot.

[Blender creator](../../../../Tools/Art/Blender/create_booter_ground_project.py) runs with Blender `--background --factory-startup --python SCRIPT -- CHECKOUT_ROOT`. It refuses to overwrite the existing authoring file. Preserve the existing file and snapshot before deliberately creating a new version.

Fresh Windows proof used Blender 5.2.2 LTS and Unity 6000.4.0f1. The saved file reopened with auto-execution disabled. A read-only verification resized the plane to 25 by 7 metres, confirmed that the character transform stayed unchanged and its lowest point remained at zero, then exited without saving. The saved plane remains 10 by 10 metres. The preview render was visually reviewed; the body has 8,484 vertices and 15,016 triangles. The advisory inventory from `Tools/Art/Blender/inspect_blend.py`, creation report, Unity snapshot and preview PNG are in ignored `Logs/BooterGroundSetup/`.

This authoring-only setup does not participate in deterministic world identity, terrain streaming, generated-object identity, world placement constraints or persisted gameplay deltas. Unity assets, metadata and gameplay are untouched. The `.blend` and retained previous save remain local-only under the repository source-publication policy; only the small tools and this note belong in Git.

The author's rock sculpt was saved on October 12, 2026. The optimized limestone rock is now in this same `BooterGroundSetup.blend`, following the author's correction. Its export and texture set are documented in [LimestoneRock01](LimestoneRock01/README.md). The original sculpt remains hidden in the scene, and the untouched artist save is retained locally as `BooterGroundSetup_ArtistSource_2026-10-12.blend`.

The current rock material is the approved [white limestone with fine grit and a smooth worn surface](LimestoneRock01/ApprovedWhiteV2/README.md). Its source image, six packed maps and matching export are retained together, and the scene still uses the same optimized rock geometry.

The author-created wall `Cube` now uses the [repeating white limestone cut-brick texture](../LimestoneBrickWall/README.md). Its UVs account for the existing scale, use approximately 8.75-inch brick widths, continue around the vertical corners and give square footprints on the caps. The wall's geometry and transforms are retained, and `Cube` is selected in the saved original scene.
