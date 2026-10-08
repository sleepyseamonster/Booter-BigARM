# Unity CLI connection — 2026-10-08

## Scope and installation

The user authorized connecting the new Unity CLI to the production checkout. Setup targeted `D:\Arc & Dust\Project` on `main`, tracking `origin/main`. The separately running terrain candidate batch under `D:\BooterBigArmValidation\TerrainExpansion20261007-02` was left untouched.

- CLI: `1.0.0-beta.12`, `%LOCALAPPDATA%\Unity\bin\unity.exe`, also available on PATH.
- Editor: pinned `6000.4.0f1` (`8cf496087c8f`).
- Package: `unity pipeline install --project-path 'D:\Arc & Dust\Project' --package-version 0.8.0-exp.1 --non-interactive --json` succeeded.
- Manifest adds only `com.unity.pipeline` `0.8.0-exp.1`. Lockfile adds Pipeline and `com.unity.nuget.newtonsoft-json` `3.2.2`; Mono.Cecil's depth changes from 2 to 1 at the same `1.11.6` version. Existing resolved package versions remain unchanged, including Test Framework `1.6.0`.

## Current-machine proof

After confirming no Editor owned the production project and that its existing lock could be opened exclusively, setup launched the pinned Editor through `Start-Process -WindowStyle Hidden` with `-batchmode -nographics`, an explicit project path, and an ignored log. No window activation or keyboard interaction was used.

- `unity status --project-path 'D:\Arc & Dust\Project' --json` reported ready, PID `29716`, port `7800`, version `6000.4.0f1`.
- Command discovery succeeded against that target, with full parameter schemas.
- Read-only `eval` returned `D:/Arc & Dust/Project/Assets`; the active scene path was empty. No production scene was opened or saved by setup.
- `console_status` reported `compilationFailed=false`, `compiling=false`, zero errors and 59 warnings. Captured warnings are project obsolete API usages, unused fields, and unreachable code; no Pipeline warning appears in that captured set. Startup also logs immutable-package metadata/assembly warnings and ignored invalid retained test assemblies; these did not prevent compilation or the focused test run.
- `command run_tests --mode editor --filter TopDown3DVolumetricDustTests --filter_type testName` returned 11 total, 11 passed, zero failed/skipped/inconclusive, in 0.75 seconds. This verifies the connected test runner and focused existing contracts; no gameplay smoke test was run.
- Licensing entitlement resolution and completed project import are recorded in the D: session log. This supersedes the post-migration licensing/import uncertainty for this run.
- OS listener inspection confirmed `127.0.0.1:7800` belongs to PID `29716`.
- Package JSON parses, Pipeline manifest/lock pins agree, and all pre-existing resolved package versions were compared with `HEAD` and preserved. Task-owned whitespace checks and documentation file links pass.
- The full local preflight passed package pins, metadata pairing, Git/LFS integrity and materialized hashes, Python dependency checks and imports. Its sole failure is pre-existing trailing whitespace in the unrelated dirty `Greybox_BigARM.mat` at lines 24 and 135; that material was preserved. The task-owned candidate has no whitespace errors.

Local diagnostic files are ignored under `Logs/`: `unity-cli-connection-20261008.log`, `unity-cli-test-schema-20261008.json`, `unity-cli-warnings-20261008.json`, `unity-cli-focused-tests-20261008.json`, and `unity-cli-preflight-20261008.log`.

## Operating boundary

The setup session was left running in background for immediate CLI use. PID and port are observations, not permanent configuration; discover them again with explicit project targeting. Reuse the connection and do not start a second Editor against the project. Before an interactive launch, check unsaved state and close only the task-owned background session safely.

Pipeline automatically reconnects after Editor startup/domain reload. Codex uses PowerShell directly, so a separate MCP server/client registration is unnecessary. Runtime gameplay, procedural generation, streaming, generated-object identity, authored terrain, persistence, and production scene routing were not changed. World-system integration concerns are not applicable to this Editor connection setup because it adds no gameplay system or saved runtime state. Visual/gameplay acceptance and Player builds were not performed.

Unrelated material and terrain-authoring work remains separately owned. No push, branch switch, release, or external publication was authorized or performed.
