# Repository organization

The repository root is the active Unity project. Use [AGENTS.md](../AGENTS.md) for authority and [PROJECT_STRUCTURE.md](./Engineering/PROJECT_STRUCTURE.md) for Unity asset placement.

| Location | Responsibility |
| --- | --- |
| `Assets/_Project/` | Imported game assets, production code, scenes, settings, and tests; preserve each asset's `.meta` and GUID |
| `Packages/`, `ProjectSettings/` | Pinned Unity dependencies and project configuration |
| `Docs/` | Canonical plans, standards, research, evidence, and agent packages |
| `SourceArt/Blender/Studies/` | Provisional editable Blender studies and their source resources |
| `Docs/Agents/Blender/archives/` | Dated transfer receipts, inert historical tools, and recovered data |
| `Tools/` | Project-wide tooling in its existing ownership lanes |
| `Archive/ProprietaryEngine/`, `Archive/Unreal/` | Segregated inactive hosts, with their source, tools, and historical evidence preserved |
| `Archive/Unity/HistoricalProject/` | Frozen standalone historical Unity restore project; excluded from production import |
| `SourceData/Terrain/` | Retained bounded GIS inputs, hashes and original provenance; missing full datasets stated explicitly |
| `Tools/Repository/`, `Tools/Art/Blender/` | Shared repository validation and source-art tools |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, `.venv/` | Ignored local caches, outputs, diagnostics, and environments |

All Blender `.blend` sources below SourceArt/Blender use Git LFS. Dated `*_PreviousSave_YYYY-MM-DD.blend` files are archival saves, not replacements for the working source. They stay beside that source to preserve relative resource paths. `*.blend1` files are normally ignored; a unique recovered save is deliberately named and tracked as a `.blend` instead of silently discarding it.

For incoming folders, inventory every file, compare SHA-256 hashes against the repository, verify LFS pointers against their materialized objects, and preserve distinct versions before removing any duplicate. Keep a source-to-destination receipt. Do not overwrite current tools with older transferred copies.

Matching asset bytes alone do not establish interchangeable Unity assets: `.meta` GUIDs, import settings, references, and independent historical evidence may differ. Preserve referenced duplicates unless their full reference contract can be consolidated safely. Never bulk-deduplicate Unity, legacy, or preserved reference trees by file hash alone.

The [October 6 organization audit](./Evidence/Repository/organization-audit-2026-10-06.md) and [Blender transfer receipt](./Agents/Blender/archives/2026-10-06-transfer/README.md) record the current audit and retained source versions.

## Selected Professional Layout

The [second-pass audit and migration plan](./Operations/Repository/LAYOUT_PLAN_2026-10-06.md) selects a single active Unity core at the repo root, an `Archive/` area for proprietary-engine/Unreal/legacy Unity work, separate `SourceArt/` and source-data manifests, topic-owned documentation, and project-wide tooling. The [execution plan](./Operations/Repository/REORGANIZATION_EXECUTION_PLAN.md) records actual verified batches: inactive hosts, source art/tools, topical and historical documents, and the historical Unity implementation are now segregated and verified. The generated reference remains disabled and the active terrain document package retains its current owner/location. The dated audit map is a proposal snapshot, not a substitute for relocation receipts.

Lorekeeper's external lore source is currently disconnected, as confirmed by the author. The agent package and local game canon remain available. External reconnection is a later task; no lore repository is mirrored here.
