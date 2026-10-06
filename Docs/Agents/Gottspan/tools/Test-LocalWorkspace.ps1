param([string]$UnityEditor = $env:BOOTER_UNITY_EDITOR)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$failures = 0
function Report([bool]$Success, [string]$Message) {
    if ($Success) { Write-Output "PASS  $Message" }
    else { Write-Output "FAIL  $Message"; $script:failures++ }
}

Push-Location -LiteralPath $repoRoot
try {
    $branch = git branch --show-current
    Report ($LASTEXITCODE -eq 0 -and $branch -eq 'main') 'Working branch is main'
    $remote = git remote get-url origin
    Report ($remote -match '^https://github\.com/sleepyseamonster/Booter-BigARM(?:\.git)?$') 'Origin is Booter-BigARM'
    foreach ($path in @(
        'AGENTS.md', 'Docs/LOCAL_WORKSPACE.md', 'Docs/PROJECT_STATUS.md',
        'Packages/manifest.json', 'Packages/packages-lock.json',
        'ProjectSettings/ProjectVersion.txt',
        'Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity',
        'Assets/_Project/Scripts/Runtime/TopDown3D/BooterBigArm.TopDown3D.Runtime.asmdef',
        'Assets/_Project/Scripts/Editor/BooterBigArm.Editor.asmdef'
    )) { Report (Test-Path -LiteralPath $path) "Required file: $path" }
    foreach ($role in @('Gottspan', 'Babineaux', 'GearBall', 'Lorekeeper', 'Blender')) {
        Report (Test-Path "Docs/Agents/$role/README.md") "Agent package: $role"
    }

    $metaProblems = @()
    foreach ($asset in Get-ChildItem Assets -Recurse -Force) {
        if ($asset.Name -in @('.gitkeep', '.DS_Store')) { continue }
        if ($asset.Extension -eq '.meta') {
            $paired = $asset.FullName.Substring(0, $asset.FullName.Length - 5)
        } else { $paired = $asset.FullName + '.meta' }
        if (-not (Test-Path -LiteralPath $paired)) { $metaProblems += $asset.FullName }
    }
    $metaProblems | ForEach-Object { Write-Output "      Unpaired Unity path: $_" }
    Report ($metaProblems.Count -eq 0) 'Unity assets and .meta files are paired'

    $manifest = Get-Content Packages/manifest.json -Raw | ConvertFrom-Json
    $lock = Get-Content Packages/packages-lock.json -Raw | ConvertFrom-Json
    foreach ($package in $manifest.dependencies.PSObject.Properties) {
        $locked = $lock.dependencies.PSObject.Properties[$package.Name]
        Report ($null -ne $locked -and $locked.Value.version -eq $package.Value) "Pinned package: $($package.Name) $($package.Value)"
    }

    git lfs version
    Report ($LASTEXITCODE -eq 0) 'Git LFS is available'
    git lfs fsck
    Report ($LASTEXITCODE -eq 0) 'Current Git LFS objects pass integrity checks'
    $lfsPaths = @(git lfs ls-files --name-only)
    foreach ($path in $lfsPaths) {
        # Literal pointer receipts are archival text, not LFS-managed assets.
        $filter = git check-attr filter -- $path
        if ($filter -notmatch ': filter: lfs$') { continue }
        $valid = $false
        if (Test-Path -LiteralPath $path) {
            # Check the index candidate, including new and renormalized sources.
            # Bound the blob before reading so an old large Git binary is not emitted.
            $blobSize = git cat-file -s ":$path"
            if ($LASTEXITCODE -eq 0 -and [long]$blobSize -le 1024) {
                $pointer = @(git show ":$path")
                $oidLine = $pointer | Where-Object { $_ -match '^oid sha256:[0-9a-f]{64}$' }
                $sizeLine = $pointer | Where-Object { $_ -match '^size [0-9]+$' }
                if ($oidLine -and $sizeLine) {
                    $expected = ($oidLine -split ':', 2)[1]
                    $expectedSize = [long](($sizeLine -split ' ', 2)[1])
                    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
                    $valid = $actual -eq $expected -and (Get-Item -LiteralPath $path).Length -eq $expectedSize
                }
            }
        }
        Report $valid "LFS source matches indexed object: $path"
    }

    git diff --check
    Report ($LASTEXITCODE -eq 0) 'Working diff has no whitespace errors'
    git diff --cached --check
    Report ($LASTEXITCODE -eq 0) 'Staged diff has no whitespace errors'
    $generated = @(git ls-files 'Library/**' 'Temp/**' 'Logs/**' 'UserSettings/**' '.venv/**')
    Report ($generated.Count -eq 0) 'Local caches and Python environment are untracked'

    $venvPython = Join-Path $repoRoot '.venv/Scripts/python.exe'
    if (Test-Path -LiteralPath $venvPython) {
        & $venvPython -m pip check
        Report ($LASTEXITCODE -eq 0) 'Python environment dependency check passes'
        & $venvPython -c 'import numpy, PIL, pyproj, shapefile, rasterio; print("Terrain dependency imports pass")'
        Report ($LASTEXITCODE -eq 0) 'Terrain Python dependencies import successfully'
    } else { Write-Warning 'Terrain Python environment is absent; restore it using Docs/LOCAL_WORKSPACE.md.' }

    $versionLine = Get-Content ProjectSettings/ProjectVersion.txt | Where-Object { $_ -match '^m_EditorVersion:' }
    $version = ($versionLine -split ':', 2)[1].Trim()
    if (-not $UnityEditor) {
        $UnityEditor = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$version/Editor/Unity.exe"
        $localEditor = Join-Path $env:LOCALAPPDATA "Unity/Editors/$version/Editor/Unity.exe"
        if (-not (Test-Path -LiteralPath $UnityEditor) -and (Test-Path -LiteralPath $localEditor)) {
            $UnityEditor = $localEditor
        }
    }
    if (Test-Path -LiteralPath $UnityEditor) { Write-Output "INFO  Editor candidate for Unity ${version}: $UnityEditor (verify version before launch)" }
    else { Write-Warning "Unity $version was not found. Set BOOTER_UNITY_EDITOR for a custom install; Unity import/package restoration remains pending." }
    if (Test-Path Temp/UnityLockfile) { Write-Warning 'Unity lockfile exists; do not start a competing batchmode editor.' }
    if (-not (git config user.name) -or -not (git config user.email)) {
        Write-Warning 'Git author identity is missing; configure the user-provided name and email locally before committing.'
    }
    Write-Output "Summary: $failures failure(s). This check does not launch Unity or prove compilation/gameplay."
} finally { Pop-Location }
exit $failures
