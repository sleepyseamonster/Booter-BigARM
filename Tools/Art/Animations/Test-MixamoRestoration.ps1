param([string]$FixtureParent = 'D:\BooterBigArmValidation\AnimationRestoreFixtures')
$ErrorActionPreference = 'Stop'
$restore = Join-Path $PSScriptRoot 'Restore-MixamoAssets.ps1'
$fixture = Join-Path $FixtureParent ([Guid]::NewGuid().ToString('N'))
$repo = Join-Path $fixture 'Repo'; $downloads = Join-Path $fixture 'Downloads'; $private = Join-Path $fixture 'Private'
$checks = 0
function Require([bool]$Condition, [string]$Reason) { if (-not $Condition) { throw $Reason }; $script:checks++ }
function Reject([scriptblock]$Action, [string]$Reason) {
    $rejected=$false; try { & $Action | Out-Null } catch { $rejected=$true }
    Require $rejected $Reason
}
try {
    [IO.Directory]::CreateDirectory($repo) | Out-Null; [IO.Directory]::CreateDirectory($downloads) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $repo 'Assets')) | Out-Null
    & git -C $repo init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated Git fixture.' }
    [IO.File]::WriteAllText((Join-Path $repo '.gitignore'),"/Assets/_Project/Art/Animations/Mixamo/**/*.fbx`n/Assets/_Project/Art/Animations/Mixamo/**/*.fbx.meta`n")
    $source=Join-Path $downloads 'one.fbx'; $alias=Join-Path $downloads 'two.fbx'
    [IO.File]::WriteAllBytes($source,[byte[]](1,2,3,4)); [IO.File]::Copy($source,$alias)
    $hash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    $relative='Assets/_Project/Art/Animations/Mixamo/Fixture/One.fbx'
    $plan=[ordered]@{schema_version=2;assets=@([ordered]@{asset_id='fixture.one';destination=$relative;source_sha256=$hash;source_bytes=4;sources=@(@{path='one.fbx'},@{path='two.fbx'})});target_rig_candidates=@()}
    [IO.File]::WriteAllText((Join-Path $repo 'plan.json'),($plan | ConvertTo-Json -Depth 10))
    $parameters=@{RepositoryRoot=$repo;DownloadsRoot=$downloads;PrivateRoot=$private;PlanPath='plan.json';Apply=$true}
    & $restore @parameters -Mode StageSources | Out-Null
    Require ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $alias).Hash) 'Source aliases changed.'
    Reject { & $restore @parameters -Mode Restore } 'Unsealed restoration must require explicit first import.'
    Reject { & $restore @parameters -Mode Restore -AllowFirstImport -FirstImportPaths 'Assets/foreign.fbx' } 'Unplanned first-import selections must be rejected.'
    Require (-not (Test-Path -LiteralPath (Join-Path $repo $relative))) 'Rejected selection wrote a file.'
    & $restore @parameters -Mode Restore -AllowFirstImport -FirstImportPaths $relative | Out-Null
    $payload=Join-Path $repo $relative; $metadata=$payload+'.meta'; $guid=[Guid]::NewGuid().ToString('N')
    [IO.File]::WriteAllText($metadata,"fileFormatVersion: 2`r`nguid: $guid`r`n")
    & $restore @parameters -Mode Capture | Out-Null
    Reject { & $restore @parameters -Mode Restore -AllowFirstImport -FirstImportPaths $relative } 'A sealed bundle cannot use the first-import path.'
    Remove-Item -LiteralPath $payload,$metadata
    & $restore @parameters -Mode Restore | Out-Null
    Require ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToLowerInvariant() -eq $hash) 'Restored bytes differ.'
    Require ([IO.File]::ReadAllText($metadata).Contains($guid)) 'Restored GUID differs.'
    Remove-Item -LiteralPath $payload,$metadata
    $collision=Join-Path $repo 'Assets/collision.asset.meta'
    [IO.File]::WriteAllText($collision,"guid: $guid`n")
    Reject { & $restore @parameters -Mode Restore } 'Duplicate existing GUID must block hydration.'
    Require (-not (Test-Path -LiteralPath $payload)) 'Failed preflight wrote a payload.'
    Remove-Item -LiteralPath $collision
    [IO.File]::WriteAllText($metadata,"guid: $([Guid]::NewGuid().ToString('N'))`n")
    $previous=[IO.File]::ReadAllText($metadata)
    Reject { & $restore @parameters -Mode Restore } 'Differing metadata must be preserved.'
    Require ([IO.File]::ReadAllText($metadata) -ceq $previous) 'Differing metadata was overwritten.'
    $plan.assets[0].sources=@(@{path='../outside.fbx'})
    [IO.File]::WriteAllText((Join-Path $repo 'plan.json'),($plan | ConvertTo-Json -Depth 10))
    Reject { & $restore @parameters -Mode StageSources } 'Source traversal must be rejected.'
    [pscustomobject]@{result='PASS';checks=$checks;scope='isolated file/Git fixtures only; no Unity import'} | ConvertTo-Json -Compress
} finally {
    $absolute=[IO.Path]::GetFullPath($fixture)
    $parent=[IO.Path]::GetFullPath($FixtureParent).TrimEnd('\')+'\'
    if (-not $absolute.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup escaped its parent.' }
    if (Test-Path -LiteralPath $absolute) { Remove-Item -LiteralPath $absolute -Recurse -Force }
}
