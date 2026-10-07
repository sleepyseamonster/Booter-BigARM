# Local workspace and resume guide

This is the Windows setup and session entry point for the checkout restored on 2026-10-06. Root [AGENTS.md](../AGENTS.md) and the existing agent packages remain authoritative.

## Repository and branch

- Origin: `https://github.com/sleepyseamonster/Booter-BigARM.git`.
- Working branch: `main`, tracking `origin/main`. The user's repository link selected this branch; keep work here unless the user changes that instruction.
- Initial restored revision: `4f2fe41c8e6074f99b6e55fb92a32e41f6fe9a54` (`Record verified safe publication sequence`).
- This checkout initially has shallow history. Fetch older `main` history only when a task needs it; do not infer missing history means missing implementation.
- Checkout location on this machine: `C:\Users\kbkno\OneDrive\Desktop\arc & dust Unity`. Derive paths from the repo root in reusable tools.
- This folder is registered with Unity Hub through its CLI `projects add`, with the pinned editor version. Hub lives at `D:\Unity\Unity Hub`; no editor was found in the initial installed-editor registry or drive search. The authorized setup installs the editor under `%LOCALAPPDATA%\Unity\Editors` so no administrator-only install directory is needed.
- The previous macOS checkout, backups, editor caches, unsaved state, and excluded local files are not transferred by Git. Old absolute paths in retained evidence identify that earlier machine.

## Resume context

Read the [October 6 publication receipt](./Evidence/Publication/2026-10-06-safe-publication.md) before interpreting older snapshot documents. It is the latest restored checkpoint and records 43/43 focused terrain tests, 8/8 focused authored-asset tests, and a passing prototype validator on the previous machine. These are retained evidence, not fresh Windows test results.

The [Badwater baseline](./Evidence/Badwater/gameplay-terrain-baseline-2026-10-06.md) records source audit and an isolated macOS Development Player build. Player profiling, traversal, and visual acceptance remain pending. The Badwater implementation goal was paused at publication; setup does not resume that work automatically. No macOS Player build or unpublished recovery artifact is assumed present here. Use the [Badwater plan](./Design/Gameplay/BADWATER_GAMEPLAY_TERRAIN_PLAN.md) for the next authorized terrain task.

[PROJECT_STATUS.md](./PROJECT_STATUS.md) was reconciled September 21 and contains older foundation evidence. For a feature task, inspect live files and its newer evidence before relying on that snapshot. The active production area is the repository-root Unity TopDown3D project; preserve `Engine/`, `Unreal/`, and `Assets/_Project/Legacy2D/` as references.

## Existing local agent packages

These repository folders contain the restored instructions, SOPs, tools, templates, and retained memory. They are role packages used by this chat; their presence does not start agents or install a Codex plugin.

| Role | Entry point | Responsibility |
| --- | --- | --- |
| Gottspan | [README](./Agents/Gottspan/README.md) | Repo coordination, routing, integration, and evidence |
| Babineaux | [README](./Agents/Babineaux/README.md) | Unity bridge, import, editor automation, and validation |
| Gear Ball | [README](./Agents/GearBall/README.md) | Scoped Git commits and authorized publication |
| Lorekeeper | [README](./Agents/Lorekeeper/README.md) | Narrative retrieval and game-canon boundaries |
| Blender | [README](./Agents/Blender/README.md) | 3D sources, modeling, and asset handoff |

Start each session with root instructions, this guide, the relevant role's default load, `git status --short --branch`, and the feature's controlling documents. Keep retained memory bounded; record fresh evidence separately from previous-machine results. Commit only verified task-owned changes. Pushes require current user authority.

## Packages and prerequisites

- Unity is pinned by [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt) to `6000.4.0f1`.
- [manifest.json](../Packages/manifest.json) and [packages-lock.json](../Packages/packages-lock.json) restore the project's package definitions, including URP `17.4.0`, Input System `1.19.0`, and Test Framework `1.6.0`. Unity resolves these into ignored `Library/PackageCache` during import; copying the manifests alone does not install the package cache.
- Git LFS materializes the two Blender sources declared in [`.gitattributes`](../.gitattributes). Use `git lfs pull origin main` and `git lfs fsck`; preserve both sources.
- Terrain Python dependencies are pinned in [requirements.txt](./Agents/Blender/tools/death_valley/requirements.txt). The local `.venv` uses Python 3.12 and stays ignored. Restore with `.\.venv\Scripts\python.exe -m pip install -r Docs/Agents/Blender/tools/death_valley/requirements.txt`, then run `.\.venv\Scripts\python.exe -m pip check`. Recreate it with an available Python 3.12 executable using `-m venv .venv` if necessary.
- Blender authoring requires a separately installed Blender. Prior source-readability evidence used Blender 5.2.2 LTS; verify the installed version and API before invoking saved scripts.
- macOS `.sh` launch helpers and hardcoded `/Applications` commands are retained for their original platform. Use Windows executable paths here. No new navigation, rendering, or asset-format package is required for this setup.

## Windows preflight and Unity restoration

Run the read-only preflight from PowerShell:

```powershell
& .\Docs\Agents\Gottspan\tools\Test-LocalWorkspace.ps1
```

For a custom Unity installation, pass `-UnityEditor 'D:\Unity\6000.4.0f1\Editor\Unity.exe'` or set `BOOTER_UNITY_EDITOR`. The preflight checks branch/origin, required paths, agent packages, direct package pins, LFS objects, and ignored local caches. It does not launch Unity or change package versions.

Once the exact editor is installed, Babineaux can restore packages and import in background batchmode. First inspect current Git state, verify the executable version, and confirm no editor owns this project (process inspection plus `Temp/UnityLockfile`). Keep logs under ignored `Logs/`. Use the following from the repo root only after those checks:

```powershell
$editor = Join-Path $env:LOCALAPPDATA 'Unity\Editors\6000.4.0f1\Editor\Unity.exe'
$project = (Get-Location).Path
New-Item -ItemType Directory -Path Logs -Force | Out-Null
$log = Join-Path $project 'Logs\local-import.log'
$arguments = @('-projectPath', ('"' + $project + '"'), '-batchmode', '-nographics', '-quit', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$null = $process.Handle
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity import failed; inspect $log" }
git status --short --branch
```

Import can serialize project assets. Review any tracked changes and preserve unrelated work. Import success proves neither focused test execution nor interactive acceptance. Use the canonical validator and focused checks from [UNITY_AUTOMATION.md](./Engineering/UNITY_AUTOMATION.md) for a subsequent implementation task; do not run gameplay smoke tests or rebuild scenes during setup.

## Windows setup evidence — 2026-10-06

- Unity Hub registered this exact checkout. Unity `6000.4.0f1_8cf496087c8f` was installed under `%LOCALAPPDATA%\Unity\Editors`; the CLI's `editors verify 6000.4.0f1` passed.
- Unity restored 64 entries under `Library/PackageCache`. Both tracked package manifests retain their original pins. Initial package API-migration errors and two shader messages cleared after cold import and a validator retry; no package version or shader source change was needed.
- `BooterBigArm.Editor.TopDown3DPrototypeValidator.ValidateFromCli` exited 0 on Windows. Its volumetric shader failure now includes compiler severity, platform, line, and message instead of only a count.
- `BooterBigArm.Tests.TopDown3DVolumetricDustTests` ran in EditMode: 11 passed, 0 failed, 0 skipped. This is focused structural/contract proof; it does not accept rendering appearance, gameplay feel, or Badwater performance.
- `Test-LocalWorkspace.ps1` reported zero failures. Direct package pins, Unity asset/metadata pairs, both materialized LFS SHA-256 hashes, Python imports, `pip check`, and whitespace checks passed. Relative documentation links and PowerShell syntax also passed.
- Local logs and test XML are under ignored `Logs/`: `windows-import-validation.log`, `windows-validator-diagnostics.log`, `windows-volumetric-tests.log`, `windows-volumetric-tests.xml`, and `local-workspace-preflight.log`. They are current-machine diagnostics rather than published evidence.
- The first failed batchmode run left an unlocked `Temp/UnityLockfile` and an idle compiler server. After confirming the editor had exited and the lock could be opened exclusively, the task removed that stale lock and stopped its own compiler server. Windows launch examples now wait on the editor process itself with `WaitForExit()` instead of waiting indefinitely for all descendant compiler servers.
- No scene, shader, runtime gameplay source, package manifest, or Unity metadata was changed during setup. After the user identified themselves as `sleepyseamonster`, Git author identity was configured for this repository using the name and GitHub noreply address on the restored publication commit. No push was performed.
