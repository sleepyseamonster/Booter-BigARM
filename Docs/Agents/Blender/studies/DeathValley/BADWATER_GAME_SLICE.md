# Badwater four-slice terrain study

**Status (2026-10-05):** [BadwaterGameSlice.blend](./BadwaterGameSlice.blend) now contains the original southwest terrain slice and the three adjoining slices marked in the user's screenshot: northwest, northeast, and southeast. They form one measured **4.096 × 4.096 km** scene at the Badwater basin edge. The saved viewport frames the four-slice terrain; **Numpad 0** returns to the basin-eye camera. This is a Blender terrain and surface study, not yet a gameplay-ready Unity world.

## Geographic layout and geometry budget

The common EPSG:26911 bounds are `[520400,4006200,524496,4010296]`. The original southwest slice is `[520400,4006200,522448,4008248]`. The new slices are:

| Position | Bounds, EPSG:26911 | Terrain |
| --- | --- | --- |
| Northwest | `[520400,4008248,522448,4010296]` | Basin margin and rising slopes |
| Northeast | `[522448,4008248,524496,4010296]` | Mountain ridges and canyons |
| Southeast | `[522448,4006200,524496,4008248]` | Mountain ridges and basin-facing escarpments |

The scene contains **256 individually named 256 × 256 m chunks**: 252 shown at 2 m vertex spacing and the original four-chunk 512 × 512 m focus at 1 m spacing. It has **8,782,848 evaluated terrain triangles**, five packed images and one Blender review camera. The `.blend` is about 137 MB. These triangles fit in one saved Blender scene on the current 32 GB M1 Max machine, but all chunks remain loaded together; no distance-based streaming or performance guarantee for other machines is claimed. The [full-area preview](./previews/badwater_four_slices_map.png) shows all four geographic quarters.

## Measured elevation and seams

Two adjacent [USGS 3DEP 1 m DEM products](https://www.usgs.gov/3d-elevation-program/about-3dep-products-services) provide the heights: [x52y401](https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/1m/Projects/CA_FEMAR9Southeast_D24/TIFF/USGS_1M_11_x52y401_CA_FEMAR9Southeast_D24.tif) and [x52y402](https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/1m/Projects/CA_FEMAR9Southeast_D24/TIFF/USGS_1M_11_x52y402_CA_FEMAR9Southeast_D24.tif). Their individual [south](https://thor-f5.er.usgs.gov/ngtoc/metadata/waf/elevation/1_meter/geotiff/CA_FEMAR9Southeast_D24/USGS_1M_11_x52y401_CA_FEMAR9Southeast_D24.xml) and [north](https://thor-f5.er.usgs.gov/ngtoc/metadata/waf/elevation/1_meter/geotiff/CA_FEMAR9Southeast_D24/USGS_1M_11_x52y402_CA_FEMAR9Southeast_D24.xml) metadata describe a lidar-derived bare-earth surface in NAD83 / UTM 11N and NAVD88 metres. A heightfield cannot show overhangs, cliff undersides, loose rocks, or changing surfaces; 1 m sample spacing is not a survey-accuracy guarantee.

The prepared 4097² grid contains the native 1 m source samples across the full area. The 2 m display grid is strict decimation of that source. The original southwest 2049² area is **bit-identical** to the previous prepared terrain. At the selected source-tile join near northing 4,010,000 m, the overlapping source values differ by at most **0.0001221 m**; the outermost resampling row of the south product is deliberately excluded from that join. No procedural noise or artistic displacement alters mesh elevation.

`verify_four_slices.py` independently sampled the recorded source windows; the largest interpolation difference was **0.0000606 m**. All 256 exported RAW height-tile borders agree exactly, and the four 1 m focus exports agree with their 2 m counterparts at every shared sample. The global height encoding spans −100 to 1700 m, with a measured maximum 16-bit quantization error of **0.01374 m**. `verify_game_slice_scene.py` reopened the saved Blender scene and checked every visible vertex against the 1 m grid and every adjacent mesh edge against its neighbor: **zero mesh/source difference and no seam gaps**. These checks establish source and assembly consistency; they do not establish independent survey truth or Unity behavior.

## Surface and close-view status

The full-area material uses one aligned [USGS NAIP](https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer) color acquisition at 2 m pixels, with 99.11% valid coverage and a recorded [Landsat natural-color](https://landsat.arcgis.com/arcgis/rest/services/Landsat/MS/ImageServer) fallback for gaps. One full-area color image avoids a visible photographic join between the four slices. Generated slope/relief masks blend pale basin, alluvium, talus and rock with the existing [Poly Haven CC0 rocky ground scan](https://polyhaven.com/a/rocks_ground_09). The scan provides close color, roughness and normal detail; the material also adds fine shader grain. Material slot 1 retains the source-aligned geographic color for comparison.

These masks and shader bumps change **appearance only**. Aerial imagery contains baked shadows and traces of human activity, so it is a reference surface rather than a finished game material. No individual rocks, debris, overhang meshes or gameplay colliders were added in this expansion. The [eye-level preview](./previews/badwater_four_slices_eye.png) shows the current close-view limit.

## Blender review and Unity handoff

The scene opens in a wide perspective view of all four slices. `BasinEyeView_1p7m` remains at EPSG:26911 `(520600, 4006750)`, exactly 1.7 m above the recorded ground, facing the eastern slopes. Press **Numpad 0** in the 3D viewport to see through it. **View → Navigation → Walk Navigation** gives mouse-look and W/A/S/D movement. The camera is a Blender review viewpoint, not a Unity game camera.

The external manifest records absolute bounds and stable `rNN_cNN` identities for every 256 m chunk. It includes 129² little-endian unsigned 16-bit candidate height exports for all chunks and 257² exports for the four focus chunks. Unity importer orientation, vertical scaling, collision, LOD transitions and runtime streaming still need their own proof. The Blender edge triangulation bridges the 1 m focus and 2 m context, but Unity must reproduce that seam if these heights are imported there.

- Deterministic world identity: fixed source bounds, chunk IDs and this versioned data manifest identify the bounded geographic sample.
- Chunk load and unload: each tile is independently addressable; the Blender scene itself loads all tiles, and no Unity loader exists for these exports.
- Stable generated-object identity: terrain IDs are stable. Future rock and route instances need separate deterministic identities.
- Authored constraints: surface masks are visual hints; the production World Creator's rules remain authoritative until reviewed.
- Persisted deltas: this reproducible source terrain is separate from any future player-caused terrain or world change.

## Rebuild record

The [four-slice configuration](../../tools/death_valley/badwater_four_slices.json), [source preparation](../../tools/death_valley/prepare_four_slices.py), [scene builder](../../tools/death_valley/build_game_slice_scene.py), [rebuild wrapper](../../tools/death_valley/rebuild_game_slice.py), [data verifier](../../tools/death_valley/verify_four_slices.py), [scene verifier](../../tools/death_valley/verify_game_slice_scene.py) and [preview renderer](../../tools/death_valley/render_four_slices_preview.py) are versioned in the Blender tools folder. The source windows, both metadata files, NAIP and Landsat responses, hashes, prepared arrays, imagery composite, masks, RAW candidate exports and verification reports are retained outside Git at `/Users/worldbuilder/Desktop/Death Valley Terrain Data/four_slices_v1/`. The previous single-slice data remains at `/Users/worldbuilder/Desktop/Death Valley Terrain Data/game_slice_v1/` for independent comparison.
