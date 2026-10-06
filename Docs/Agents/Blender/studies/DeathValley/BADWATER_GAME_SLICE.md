# Badwater game-scale terrain slice

**Status (2026-10-05):** First measured, chunked Blender terrain and surface study. [BadwaterGameSlice.blend](./BadwaterGameSlice.blend) is open in the single Blender window as a freely navigable 2.048 × 2.048 km source scene at the Badwater basin edge, about 1–3 km south of the Badwater marker. It is **not yet a gameplay-ready Unity world**: no runtime streaming, collision, traversal, rock kit, or player test is claimed.

## Why this slice

The broad regional viewer answers where the mountains and basin are, but its 10–40 m close terrain cannot answer what a player sees beside a wash or cliff. This bounded slice includes a pale basin margin at the west, fans and channels in the middle, and steep exposed eastern rock. It is a representative height and material test before spending the same density over a much larger area. The prior regional scene is preserved separately.

| Layer | Extent or count | Elevation source | Role |
| --- | --- | --- | --- |
| Study bounds | EPSG:26911 `[520400,4006200,522448,4008248]` | [USGS 1 m tile x52y401](https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/1m/Projects/CA_FEMAR9Southeast_D24/TIFF/USGS_1M_11_x52y401_CA_FEMAR9Southeast_D24.tif) | Fixed geographic identity and local Blender origin |
| 64 terrain chunks | 256 × 256 m each | One aligned 2049 × 2049 native-source grid | Independently addressable, shared-edge tiles |
| 60 outer chunks | 2 m visible spacing; 129 × 129 height samples per export | Every second 1 m source sample | Context at lower geometry cost |
| Four focus chunks | 1 m visible spacing; 257 × 257 height samples per export | Same 1 m source grid | Basin-to-rock transition in rows 4–5, columns 1–2 |

The scene has **2,491,392 evaluated terrain triangles**, five packed images and no camera. The focus is about 512 × 512 m. Blender loads all 64 chunks; named collections and per-object IDs make selective inspection possible, but the scene does not itself stream by viewer distance.

## Topography accuracy and seams

The source is the specific [USGS 3DEP one-metre DEM](https://www.usgs.gov/3d-elevation-program/about-3dep-products-services) identified above. Its [tile metadata](https://thor-f5.er.usgs.gov/ngtoc/metadata/waf/elevation/1_meter/geotiff/CA_FEMAR9Southeast_D24/USGS_1M_11_x52y401_CA_FEMAR9Southeast_D24.xml) describes a lidar-derived **bare-earth** surface, EPSG:26911/NAD83 horizontal positioning, elevation in metres and NAVD88 vertical reference. The metadata does not give a numeric vertical accuracy for this tile; do not treat 1 m sample spacing as a 1 m accuracy guarantee or expect overhangs, small stones, changing surfaces, and cliff undersides from this heightfield.

The full 1 m source window, exact exported source clip, prepared arrays, TIFF response and SHA-256 hashes are stored outside the Unity import tree at `/Users/worldbuilder/Desktop/Death Valley Terrain Data/game_slice_v1/`. The 2 m grid is a strict decimation of the 1 m grid, **not an enlarged 10 m DEM**. At 230,400 overlap samples, the 1 m source differs from the interpolated earlier 10 m patch by 1.82 m RMS (3.75 m at the 95th percentile), showing additional measured relief. No noise or artistic displacement is added to the mesh heights.

`verify_game_slice.py` checked 256 independent bilinear reads against the recorded source clip; maximum regrid difference was 0.0000293 m. The 2049² source grid has no missing vertices and ranges from −84.50 to 952.01 m. All 64 quantized height-tile edges match exactly; four 1 m exports agree with their 2 m versions at every shared sample. A single shared encoding range of −100 to 1000 m bounds 16-bit quantization error to about 0.0084 m. `verify_game_slice_scene.py` reopened the saved `.blend`: all visible mesh vertices exactly matched the prepared 1 m source values, including midpoint vertices added to neighboring 2 m tile edges, and all mesh borders matched. This is geometric/source consistency proof, not survey validation or Unity runtime proof.

## Surface and texture status

The visible art material combines source aligned [USGS NAIP](https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer) macro color (98.61% valid in this crop), a recorded Landsat natural-color fallback for gaps, generated slope/relief masks for pale basin, alluvium, talus and rock, and the existing [Poly Haven CC0 rocky ground scan](https://polyhaven.com/a/rocks_ground_09) for reusable close color, roughness and normal detail. Material slot 1 retains pure geographic color for comparison. The generated masks and shader bump **change appearance only**, never DEM height. The image has baked aerial shadows and traces of human activity, so this is an editable visual direction and not a shipping game material. The masks are art hints, not geologic, drainage or traversal truth. No individual rocks, ledges or debris were built in this pass.

## Game-world handoff contract

The external `manifest.json` records absolute EPSG:26911 bounds and an `rNN_cNN` identity for every 256 m chunk. It contains 129² little-endian unsigned 16-bit height exports for all chunks and 257² exports for the four high-detail chunks, with one global height range and north-to-south row order. Unity accepts 129 and 257 heightmap dimensions, but importer orientation, rendered LOD seams, colliders and vertical scaling **must be verified there** before these exports are called runtime assets. The Blender transition edge triangulation is explicit; a Unity terrain adapter must achieve the same seam result or build an equivalent transition mesh.

- Deterministic world identity: use fixed source bounds, chunk row/column and a versioned data manifest. This is a bounded geographic sample, not the effectively infinite world's coordinate authority.
- Chunk load and unload: each tile has an ID and source height export. No loader exists yet; the owning Unity world-query and representation system must decide when to display each LOD.
- Stable generated-object identity: terrain chunk IDs are stable. Future rocks/routes need their own seed, version and absolute-coordinate IDs; none are placed here.
- Authored constraints: masks are provisional visual layers. The World Creator's geology, routes and traversal constraints remain authoritative until a reviewed mapping is approved.
- Persisted deltas: the static source terrain is reproducible; any runtime excavation or player change belongs in the existing world delta system rather than a modified source tile.

The next production gate is a bounded Unity import and player-scale visual/collision test, followed by hand-authored cliff pieces and a small rock/debris kit where the heightfield cannot carry the silhouette. That work should use the active World Creator path and avoid the current unrelated dirty Unity edits. This Blender artifact supplies the measured slice and chunk contract for that gate.

## Rebuild and review

The versioned [slice configuration](../../tools/death_valley/badwater_game_slice.json), [source preparation](../../tools/death_valley/prepare_game_slice.py), [scene builder](../../tools/death_valley/build_game_slice_scene.py), [rebuild wrapper](../../tools/death_valley/rebuild_game_slice.py), and [verification scripts](../../tools/death_valley/verify_game_slice.py) live in the Blender tools folder. The external data directory retains `verification.json`, `scene_verification.json`, the native source clip, imagery response, prepared grids, masks and RAW candidate exports. [Wide](./previews/badwater_slice_wide.png), [transition](./previews/badwater_slice_transition.png), and [game-scale oblique](./previews/badwater_slice_game_view.png) renders were made with temporary cameras; no review camera was saved in the scene.
