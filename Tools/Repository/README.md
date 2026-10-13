# Gottspan Tools

## `verify_publication_candidate.py`

Read-only gate for the complete unpublished candidate range: checks remote-tracking ancestry, excluded Blender/terrain-source path touches, ordinary Git blob limits, and exact SHA-256/size of each required local LFS object. Writes an object allowlist receipt; never uploads, changes attributes or prunes caches. Live remote, account allowance, pack estimation, LFS dry-run reconciliation and Unity proof remain separate gates.

```powershell
.venv\Scripts\python.exe Tools/Repository/verify_publication_candidate.py --output Logs/PublicationImplementation20261012/candidate.json
```

## `classify_terrain_storage.py`

Produces a file-level terrain storage inventory, manifest/protected-folder consumer map, Unity GUID/YAML reference audit and proposed storage classes. It verifies the saved production scene's TerrainData path set against the accepted final fourth-row manifest. It never moves assets, configures LFS, launches Unity or publishes sources. The compressed TSV contains metadata only; source payloads remain local. Binary dependency proof and fresh payload SHA-256 inventories are separate migration gates.

```powershell
.venv\Scripts\python.exe Tools/Repository/classify_terrain_storage.py --output Logs/TerrainStorageReaudit
```

Read [the terrain storage migration plan](../../Docs/Operations/Repository/TERRAIN_STORAGE_MIGRATION_PLAN_2026-10-12.md) before treating any proposed archive class as removal authority.

## `repo-health.sh`

Runs a read-only structural health check for:

- Required manager and Unity project files.
- Git status, diff whitespace, and accidentally tracked generated output.
- Unity asset and `.meta` pairing.
- Finder metadata under `Assets/`.
- Project-version discovery and matching installed Unity editor.

Run from anywhere:

```bash
bash Tools/Repository/repo-health.sh
```

A dirty worktree and ignored Finder metadata are warnings because they require ownership awareness but are not automatically defects. The tool never launches Unity, imports assets, edits files, stages changes, or proves gameplay behavior.

The Windows `Test-LocalWorkspace.ps1` counterpart also checks pinned packages, the local terrain Python environment, and LFS integrity. Its source check uses indexed pointer OIDs and sizes, including newly added/renormalized sources, and skips literal pointer receipts without an LFS attribute. It bounds pointer blobs before reading so an ordinary large Git binary is never dumped as text.

## `verify-transfer-manifest.py`

Read-only SHA-256 verification of every repository destination in a transfer receipt, including materialized Git LFS objects. Add `--incoming` before cleanup to verify the entire original inventory as well. Paths must stay inside the checkout; the tool never executes transferred code or removes files.

```powershell
.venv\Scripts\python.exe Tools/Repository/verify-transfer-manifest.py Docs/Agents/Blender/archives/2026-10-06-transfer/transfer-manifest.json
```
