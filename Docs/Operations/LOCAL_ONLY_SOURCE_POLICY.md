# Local-only source data and GitHub publication

Owner: Gottspan and Gear Ball. Authority: explicit author direction on October 9, 2026.

Blender authoring files (`.blend` and numbered backups) and terrain source archives under `SourceData/Terrain/` stay local. Do not stage, commit, push or add Git LFS rules for them. Large Blender files, GIS acquisitions, raw captures, prepared arrays, source imagery and compressed terrain archives are not GitHub publication content.

Keep these local sources intact. Removing Git tracking does not authorize physical deletion, LFS pruning, or loss of the author's local recovery history. Fresh clones do not contain those source archives; source authoring or rebuilding requires a separately provisioned local copy.

The native Unity terrain assets, their metadata and source resources already required under `Assets/_Project/` remain normal project content. They are required for Greater Wasteland to open, validate and run. Scene/code changes, documentation, small verification receipts and approved optimized runtime assets may be published.

Before publication, inspect the complete unpublished commit range as well as the final tree. A later deletion does not prevent an earlier unpublished addition from uploading. Exclude local-only source paths from that range, preserving the original unpublished history locally. Do not rewrite already-published remote history or force-push without a separate explicit author instruction.

The [Blender inventory](../Evidence/Repository/BLENDER_LOCAL_ONLY_2026-10-09.json) and [terrain-source inventory](../Evidence/Repository/TERRAIN_SOURCES_LOCAL_ONLY_2026-10-09.json) record locally retained files. Previously published historical Blender objects and unreferenced payloads transferred by the interrupted push are not claimed to have been purged from GitHub storage. The interrupted push did not advance remote main.
