# Repository organization implementation evidence

Owner: Gottspan. Status: implemented and validated on 2026-10-06 on main. Authority: author's organizational plan and verified file-move authorization. Unity remains the root project; all processes ran in the background.

| Area | Current location | Evidence |
| --- | --- | --- |
| Production gameplay | Assets/_Project/Scenes/Production/GreaterWasteland.unity | Scene GUID preserved; sole enabled build scene; production/template/terrain/readback validators pass |
| Rebuild template | Assets/_Project/Scenes/TopDown3D/GameplaySetup.unity | Nine wired gameplay roots; no terrain, generator or save service |
| Generated reference | Assets/_Project/Scenes/Reference/GeneratedWorld/TopDown3DPrototype.unity | GUID/bytes unchanged; disabled; retained authoring and validation consumers repaired |
| Historical Unity | Archive/Unity/HistoricalProject | 786 frozen inputs hash verified; native dependency closure; separate import/compile; eight historical tests passed |
| Proprietary/Unreal | Archive/ProprietaryEngine and Archive/Unreal | 1,287 inputs accounted for; 12 roadmap tests; 43 surface assets and 186 bindings verified |
| Source art | SourceArt/Blender/Studies | 207 source/tool files mapped; ten Blender files reopened before/after with unchanged hashes, zero missing unpacked resources and autoexec disabled |
| Source/tool provenance | Tools/Art/Blender; Tools/Repository; Archive/Transfers/Blender | Anchor root discovery and relocation aliases; five path tests; all 252 original transfer files still hash verified |
| Terrain source data | SourceData/Terrain/DeathValley/WestCandidate2026-10-06 | Six unchanged bounded recovered inputs and original proof; manifest hashes; TIFF/NPZ LFS pointers verified |
| Documentation | Docs/Design, Engineering, Operations, Research, Evidence and Archive | Root canon/status anchors retained; topical and historical navigation repaired |

Final structural evidence: zero duplicate asset GUIDs and zero unresolved GUID references across 2,262 serialized files; all 44 production Resources files unchanged against the pre-organization baseline. No resource was removed by name or guessed redundancy. LFS fsck passes. Windows repository/package/meta/Python preflight reports zero failures. Terrain offline tests: 24 passed, including recovery of the moved Blender source through relocation resolution. Earlier affected production checks: 11 focused EditMode tests passed. Final isolated Unity 6000.4.0f1 CLI exits zero; all 960 directed neighbor links and 480 terrain edges match exactly.

Evidence remains in local Logs: org04-tests.xml, org06-production-tests.xml, historical-restore-tests.xml, org07c-validation-repaired.log, audit-reference-report.json, org08-preflight.log, and BlenderRelocationPreflight/Postflight. Pinned Packages and source settings were retained; no executable build or gameplay smoke test was run. The isolated project's existing caught PackageCache path diagnostic is recorded in its log; successful compile/validator results do not claim a completely silent log.

## Deliberate retention and missing inputs

The generated-world prototype remains imported and disabled because current reference-specific authoring tools and structural tests require it. Its external archival would require a separately verified tool/assembly extraction. Production build gates no longer depend on that reference, and no generator was added to Greater Wasteland.

Docs/DeathValley remains at its established location while its owner edits the coherent terrain research package. The owner repaired affected live source paths through relocation resolution, preserving the original recovery proof. Concurrent terrain-map and radial-gameplay changes are excluded from organization staging and acceptance.

The full historical regional GIS dataset is unavailable; bounded recovered windows do not reconstruct it. Lorekeeper's external checkout remains absent and disconnected. The incoming Blender folder remains intact in ignored Logs/TransferHolding; exact duplicates were consolidated into existing sources, unique saves and inert original content preserved with receipts. No speculative asset deduplication occurred.

## Resumption and reversal

Read root AGENTS, Docs/LOCAL_WORKSPACE and the execution plan. Use current paths above and Tools/Repository/Test-LocalWorkspace.ps1. Never batch-launch against a GUI-owned project. Relocations JSON files account for actual moves, baseline hashes, preserved content versions and consumer changes. Reverse only an affected receipt and its declared edits; restore historical Unity into a separate root, never production Assets. Do not reset shared work or infer push authorization from local commits.
