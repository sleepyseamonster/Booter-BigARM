# Repeating white limestone cut-brick wall

October 12, 2026. The author requested a repeatable wall texture using the screenshot's staggered rough-cut brick arrangement and the approved gritty-yet-smooth white limestone surface. The brick has a square top footprint around 8–10 inches across, with a flatter rectangular wall face.

Use [LimestoneCutBrick_Albedo_Repeat.png](LimestoneCutBrick_Albedo_Repeat.png) as the wall color texture. It is a 2,048 by 2,048 sRGB PNG. Set both texture axes to **Repeat**, and preserve the horizontal orientation of the courses. The [3×3 repeat preview](LimestoneCutBrick_RepeatPreview_3x3.png) is an inspection image, not the tile to assign to a wall.

The tile contains five brick pitches across and ten courses down, in half-brick running bond. A nominal 9-inch width and depth gives the requested square top footprint; a nominal 4.5-inch height gives the flatter front face. These are texture-scale references, not modeled volume. At that scale, one UV repeat covers approximately 45 by 45 inches, or 1.143 by 1.143 metres. For a wall of width W and height H in metres, tile UVs by `W / 1.143` horizontally and `H / 1.143` vertically. Brick edges and dimensions vary slightly to preserve the roughly cut stone look.

## Source and repetition proof

The built-in image generation tool produced the masonry and limestone surface from the two references. [Provenance](provenance.json) contains both full prompts and their reference roles. The retained [generated source](Source/LimestoneCutBrick_Generated.png) is 1,254 by 1,254 pixels; the 2K deliverable resamples that source rather than adding measured detail.

The generated border initially included full mortar strips at top and bottom. The [finishing tool](../../../../Tools/Art/Blender/finish_limestone_wall_tile.py) uses a disposable background Blender material bake to sample between the joint centres, removing the doubled stripe when tiles stack. An eight-pixel border blend harmonizes wrapping stones, and shared outermost texels remove stochastic bake jitter. The interior masonry is retained. No Blender scene or project file is opened or saved.

[Verification](verification.json) records the exact source/output hashes, joint crop, physical scale, finite image values and zero opposite-border difference. The exported PNG was decoded independently: its left/right and top/bottom border RGB values match exactly. The 3×3 repeat preview was visually reviewed for mortar width, course continuity and wrapping brick shapes. These checks establish a usable repeating color tile, not Unity renderer or game performance proof.

This deliverable is one wall color texture. A normal/roughness/height map set, wall mesh and scene application were not requested and were not produced. The original player/rock scene and Unity project content are untouched. Deterministic world identity, streaming/unload/reload, generated-object identity, authored placement and persisted runtime deltas are not applicable to this independent texture creation; no world system is involved.
