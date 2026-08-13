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
"/Users/worldbuilder/Desktop/Booter & BigARM/Docs/Agents/Gottspan/tools/repo-health.sh"
```

A dirty worktree and ignored Finder metadata are warnings because they require ownership awareness but are not automatically defects. The tool never launches Unity, imports assets, edits files, stages changes, or proves gameplay behavior.
