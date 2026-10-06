# Repository and Blender transfer organization audit — 2026-10-06

Audited `main` at `06cd06d22a71ce63ce49205b516399f891fbde3f`, before this organizational pass. The initial checkout was clean except for the user-supplied transfer folder. Work stayed in the background. No application was activated, no transferred authoring script was run, and no gameplay, Unity scene, imported asset, package, or project-setting change was made.

## Transfer accounting

The incoming folder contained **252 files, 756,017,456 bytes**. SHA-256 comparisons cover every file, including ignored Blender backups and Python caches. The [transfer receipt](../../Agents/Blender/archives/2026-10-06-transfer/README.md) and [per-file manifest](../../Agents/Blender/archives/2026-10-06-transfer/transfer-manifest.json) name every destination.

| Result | Files | Handling |
| --- | ---: | --- |
| Exact duplicates already present | 210 | Retained the existing canonical copies |
| Distinct `.blend1` previous saves | 2 | Preserved unchanged beside their current sources under dated `.blend` names |
| LFS pointer for `BadwaterGameSlice.blend` | 1 | Verified its OID and size against the existing full 144,116,547-byte scene; retained the original pointer as a reference |
| Older terrain builder and scene notes | 2 | Archived exact baseline snapshots; kept the current seam repair versions active |
| Python 3.13 bytecode | 37 | Archived unchanged as inert `.pyc.bin` data |

All eight primary Blender scene files were already tracked. There was no missing primary large scene to replace. The two previous saves are distinct content and were absent from the checkout. Keeping them beside their corresponding source directories preserves relative resource paths. They do not replace either authoring source or resume the paused badlands work.

One bytecode module, `game_slice_grid.cpython-313.pyc`, has no matching editable `.py` source in the transfer or checkout. Its exact bytes are preserved; source recovery is unverified. Thirty-six other cached modules have corresponding source files.

The three apparent version conflicts were traced to the restored `4f2fe41` baseline: the old scene notes, old builder, and Git LFS pointer. No older code was copied over current tools. Existing Blender sources retain their original bytes. All ten `.blend` sources, including the two recovered saves, now use the same Git LFS storage rule; six previously stored as ordinary Git binaries are converted without rewriting history.

Deletion of the verified redundant incoming copies was rejected by automatic approval review with the reason “blocked by policy.” The complete original transfer was moved intact to ignored `Logs/TransferHolding/wetransfer_blender_2026-10-06_2121/`. The repo root is organized, and those duplicate holding files are excluded from Git. They remain on disk; this pass does not claim disk-space reclamation.

## Repository-wide inventory

The baseline inventory hashed **5,854 tracked files**, totaling **1,598,297,895 materialized working-tree bytes**. The [machine-readable audit](./organization-audit-2026-10-06.json) records area counts, all existing duplicate groups, and structural findings. Local `.git`, Unity caches, outputs, environments, and ignored diagnostic state were excluded from source-content hashing. Package-cache metadata was read only to resolve Unity GUID references.

| Area | Baseline files | Ownership result |
| --- | ---: | --- |
| `Assets/` | 3,957 | Active Unity content and isolated legacy references remain in their existing lanes |
| `Docs/` | 574 | Plans, agent packages, studies, and evidence remain at their canonical owners |
| `Engine/` | 1,276 | Preserved reference implementation and evidence retained |
| `Unreal/` | 11 | Preserved reference direction retained |
| `ProjectSettings/` | 26 | Pinned configuration unchanged |
| `Packages/` | 2 | Manifest and lockfile unchanged |
| `.vscode/`, `Tools/`, root files | 8 | Existing editor configuration, tooling, and routing retained |

The source tree already followed the intended host and asset boundaries. The changes add transfer provenance, recovered saves, consistent Blender storage, a read-only receipt verifier, and [an organization guide](../../REPOSITORY_ORGANIZATION.md). They do not perform a broad rename or merge of established folders.

A separate `Docs/DeathValley/` coverage-analysis area and development-build automation/tests appeared or changed during the audit, after the initial checkpoint. They were inventoried as concurrent active work and left in place. The JSON records an observation snapshot and path exclusions. Those files remain outside this pass's edits and commit; their contents may continue changing under their existing owner.

## Duplicates and reference findings

The inventory found **159 existing groups of identical tracked file bytes**. These are retained because matching content is not enough to establish interchangeable roles:

- Unity animation pairs can use the same FBX bytes with different importer settings. The reviewed MoCap pivot pair has `mirror: 0` and `mirror: 1`, separate names, and distinct GUIDs.
- Source textures, documents, and review evidence copied into the preserved C++ reference retain their independent provenance and routes.
- Legacy and production UI icons, material variants, and evidence snapshots have separate references or ownership.
- Empty `.gitkeep` files and repeated licenses/check receipts are intentional organizational or historical records.

There are **2,043 unique Unity asset GUIDs**, **no duplicate asset GUIDs**, and **no missing/orphaned tracked asset/meta pairs**. All scanned YAML GUID references in the active 3D assets resolve against repo or installed package metadata.

Seven unresolved serialized GUIDs occur exclusively in the preserved `Legacy2D/Settings/Rendering/URP/Renderer2D.asset`, in old debug/probe/shader/falloff resource fields. They are recorded in the JSON report. This pass preserves the legacy renderer rather than changing historical rendering behavior. Binary-embedded references and runtime import behavior are outside this textual reference check.

All active root/`Docs/` Markdown file links resolve. **32 unavailable local links** remain in preserved Engine documentation: 16 point into ignored historical `Engine/out` outputs, and 16 are relative links retained inside copied Unity document snapshots. The existing reference-library README already explains that snapshots preserve original wording and links. No unavailable output was invented and no historical snapshot was rewritten.

## Verification

- Complete transfer receipt passes SHA-256 verification against all repository destinations and the intact local holding inventory.
- The materialized Badwater scene matches its transferred LFS OID and recorded size.
- Recovered saves have container signatures matching the retained Blender source family; no Blender reopen or visual-quality claim is made.
- The receipt verifier rejects a corrupted hash and a path that escapes the checkout.
- The Windows preflight now validates indexed LFS OIDs/sizes, including newly staged sources, and excludes literal pointer receipts without an LFS attribute. The previous HEAD-only check misreported the newly staged sources and attempted to read older large Git binaries as text; the repaired check bounds blobs before reading.
- Git LFS pointer/OID readback, integrity checks, Unity metadata pairing, active documentation links, and diff whitespace are verified before commit.
- Original `Assets/`, packages, project settings, and preserved implementations remain unchanged. No executable or Unity build was produced.

Recheck content coverage from the checkout root:

```powershell
.venv\Scripts\python.exe Docs/Agents/Gottspan/tools/verify-transfer-manifest.py Docs/Agents/Blender/archives/2026-10-06-transfer/transfer-manifest.json
```

Add `--incoming` to include the local ignored holding copy. That optional copy is not required after cloning the repository. The default command verifies all 252 original contents through their repository destinations.
