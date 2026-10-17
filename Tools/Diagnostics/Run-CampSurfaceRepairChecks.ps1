param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$EvidenceDirectory,
    [string]$StartAt = ""
)
$ErrorActionPreference = 'Stop'
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
if ((Test-Path -LiteralPath (Join-Path $resolvedProject '.git')) -or !(Test-Path -LiteralPath (Join-Path $resolvedProject '.animation-playground-validation-project'))) { throw 'Focused checks require the marked offline candidate.' }
if (Test-Path -LiteralPath $EvidenceDirectory) { throw 'Preserve the existing evidence directory.' }
New-Item -ItemType Directory -Path $EvidenceDirectory | Out-Null
$cli = Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
$filters = @(
    'AnimationPlaygroundSurface', 'TopDown3DPlayerSlopeTests', 'AnimationPlaygroundPlayerIntegrationTests',
    'AnimationPlaygroundContinuationTests', 'AnimationPlaygroundForwardLocomotionTests',
    'AnimationPlaygroundMovingTurn', 'AnimationPlaygroundForwardReviewTests', 'AnimationPlaygroundPresentationTests',
    'AnimationPlaygroundDodge', 'AnimationPlaygroundJump', 'AnimationPlaygroundAirborneTests',
    'AnimationPlaygroundExpandedGaitTests', 'AnimationPlaygroundDirectionalTests', 'AnimationPlaygroundNativeGroundTests',
    'AnimationPlaygroundForwardPoseEnvelopeTests', 'AnimationPlaygroundInputTests', 'AnimationPlaygroundMotorTests',
    'AnimationPlaygroundCrouchControllerTests', 'AnimationPlaygroundLowMotionControllerTests', 'AnimationPlaygroundResetTests'
)
if ($StartAt) {
    $index = [Array]::IndexOf($filters,$StartAt)
    if ($index -lt 0) { throw "Unknown focused starting suite." }
    $filters = $filters[$index..($filters.Length-1)]
}
foreach ($filter in $filters) {
    $raw = & $cli command run_tests --mode editor --filter $filter --filter_type testName --async_tests true --timeout 45 --project-path $resolvedProject --json
    $dispatch = ($raw -join [Environment]::NewLine) | ConvertFrom-Json
    if (!$dispatch.success -or !$dispatch.data.result.success -or $dispatch.data.result.result -ne 'running') { throw "Test dispatch failed: $filter" }
    $statusPath = Join-Path $resolvedProject 'Temp/pipeline_test_status.json'
    $deadline = [DateTime]::UtcNow.AddMinutes(5)
    while (!(Test-Path -LiteralPath $statusPath)) {
        if ([DateTime]::UtcNow -gt $deadline) { throw "Focused test timeout: $filter" }
        Start-Sleep -Milliseconds 500
    }
    $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    Copy-Item -LiteralPath $statusPath -Destination (Join-Path $EvidenceDirectory ($filter + '.json'))
    [pscustomobject]@{suite=$filter;total=$status.summary.total;passed=$status.summary.passed;failed=$status.summary.failed;skipped=$status.summary.skipped;duration=$status.duration} | ConvertTo-Json -Compress
    if ($status.status -ne 'completed' -or $status.summary.total -le 0 -or $status.summary.failed -ne 0 -or $status.summary.skipped -ne 0 -or $status.summary.inconclusive -ne 0) { throw "Focused checks failed: $filter" }
}
