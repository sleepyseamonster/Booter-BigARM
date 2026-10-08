# Western terrain pair implementation

The user's two marked slices now extend Greater Wasteland directly west of its accepted West and Northwest sections. Each addition has 256 authored 256 m chunks. Production contains a 48 by 32 grid with 1,536 chunks across 12.288 by 8.192 km. Blender authoring, reopen verification, native Unity integration and focused checks are complete.

| Section | EPSG 26911 bounds in metres | Chunks |
| --- | --- | --- |
| New western section `west_outer` | `[512208, 4006200, 516304, 4010296]` | 256 |
| New northwestern section `northwest_outer` | `[512208, 4010296, 516304, 4014392]` | 256 |
| Complete target footprint | `[512208, 4006200, 524496, 4014392]` | 1,536 |

```text
New northwest | Northwest | North
New west      | West      | Original
```

The fixed Unity origin remains `[522448, 4008248]`. Geographic keys retain the `epsg26911/e{east}/n{north}/size256` contract. Terrain is authored and always loaded. Streaming and unload/reload are not implemented; no generated objects or persisted runtime deltas are introduced. The existing 1,024 chunk identities, geometry and authored gameplay objects are protected. New borders adopt their existing neighbour's exact height readback, with changes confined to the new geometry.

## Blender sources and provenance

Accepted editable sources are in `SourceArt/Blender/Studies/DeathValley/WestPair2026-10-08/`: `west_outer02.blend`, `northwest_outer02.blend`, and `combined02.blend`. Blender 5.2.2 LTS built, saved and reopened both complete sections and the 1,536-chunk combined review against the restored terrain. All measured vertex and mesh edge errors are zero; normals face upward and geographic imagery is packed. The individual sources have 2 m interior control spacing and complete 1 m borders. The combined review uses 4 m interiors with complete 1 m borders. Earlier sources and preparation attempts remain local diagnostic history.

Immutable acquisition and preparation are in `SourceData/Terrain/DeathValley/WestPair2026-10-08/`. The two accepted official USGS 1 m bare-earth products `x51y401` and `x51y402` cover the additions, with NAVD88 heights in metres. New windows and metadata were acquired from the recorded official products. Their prepared elevation range is approximately −85.146 to −81.239 m, inside the existing −100 to 1,700 m encoding range. NAIP RGB is resampled to 2 m for geographic color, matched to the retained macro-color source, and feathered only inside the additions.

The user approved removing all sculpt edits on October 8. One existing asset, `TerrainData/north/r12_c05.asset`, contained a height edit of up to 18.046 m relative to its verified source. Its sculpted bytes were backed up under `D:/Arc & Dust/Backups/SculptEdits20261008` before restoring the exact committed asset; metadata and GUIDs remain intact. `Baseline03` then captured the saved 1,024-terrain scene in isolation and sealed 10,290 protected file hashes and 1,984 exact retained seams. `Prepared02` and the accepted Blender sources use that restored baseline. The two new sections' geometry is byte-identical to its earlier preparation.

Earlier live captures had included unsaved sculpt heights before the terrain asset was serialized. Comparing those captures did not prove equivalence with the earlier saved asset. Native tests caught this error, and the alternative-hash path was removed. The importer requires exact protected bytes and full retained height readback. Distinct original and discarded-sculpt payloads remain diagnostic regression fixtures; the discarded shape is rejected. Other camera, menu, material and rendering work remains separately owned.

Prepared border adjustment is at most 0.027459 m, below the derived 0.042058 m encoding precision budget. Raw elevation samples remain immutable. Unity export reads the reopened Blender mesh proofs and pins retained normalized borders to their exact native values. The exported full footprint has 2,992 exact shared edge pairs.

The native launcher is `Tools/Art/Blender/death_valley/run_expansion_blender.py` with `--script Tools/Art/Blender/death_valley/build_west_pair_scene.py`. Acquisition, preparation and guarded export use `acquire_west_pair.py`, `prepare_west_pair.py` and `export_west_pair_unity.py`. All outputs require fresh paths. Canonical sources and validation work stay on D:.

## Unity validation and cost

Unity is pinned to 6000.4.0f1 with the existing URP package versions. The isolated candidate is `D:/BooterBigArmValidation/TerrainWestPair20261008-01`. Its builder creates only new native TerrainData, TerrainLayers, geographic textures and two reusable section prefabs. Candidate scene `Assets/_Project/Scenes/Reference/TerrainWestPairCandidate.unity` stays outside enabled Build Settings. Greater Wasteland remains the sole production scene.

All 1,024 restored full grids and asset identities remain exact. All 2,992 shared edges and reciprocal neighbours match, 160 outer neighbour slots are null, and 38,400 direct TerrainCollider ray samples passed. Maximum collider error is 0.000030518 m; new terrain readback error is at most 0.027468 m, inside the established encoding budget. Production scene preservation retains 5,667 prior native YAML documents exactly; the sole changed original document is the terrain parent with two appended children. There are 2,564 new native documents. The developer map shows all six sections without recentering the world origin.

Focused Unity EditMode tests passed 50/50 in the isolated integrated scene, including the sculpt rejection fixture. Offline checks passed 47/47. The coverage audit reports 1,536 linked native terrains and exact normalized source edges; terrain doctor checked 3,087 records with zero issues. The production scene SHA-256 is `717e2714a04092d64fc6440587e17daee615468e83d88e882e1de3506dfb2ddd`; export manifest SHA-256 is `95ab40e90d184573cca17d166fe75ad612a7ab3a6447c926d6f167a1afd783d9`. New assets and their original metadata were transferred with byte hashes. The detailed receipt is `D:/Arc & Dust/Transfers/WestPair20261008/production_transfer.json`; current proof and images are in `Docs/Evidence/DeathValley/WestPair2026-10-08/`.

All accepted Blender files, acquisition, saved baseline and prepared resources are stored on D:. Earlier local attempts are excluded from the accepted source batch. The previously missing metadata for the retained validation JSON is included so the protected file inventory remains reproducible. Packages and existing terrain metadata retain their versions and GUIDs. Separate view-distance work is preserved in owner commit `61413def`; unrelated BigARM material and QualitySettings changes remain outside this terrain commit.

The additions increase terrain count and area by 50%. The initial 1,024-terrain Editor reading was about 1.87 GiB allocated and 2.65 GiB reserved, on a 64 GiB machine with roughly 106 GiB free on D:. The live 1,536-terrain Editor reports about 1.86 GiB allocated and 2.64 GiB reserved before validation; these are separate Editor states, not a controlled memory benchmark. The scene is loaded and clean, and live readback passes all native gates. These readings establish authoring headroom, not Player frame rate. Existing instancing and pixel error 8 are retained. No actor/input or Play-mode smoke test is authorized; gameplay traversal and Player performance remain user-owned acceptance. Unity opened with `SW_SHOWNOACTIVATE` to preserve the foreground app.
