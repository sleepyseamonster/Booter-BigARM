# Gottspan Tools

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
