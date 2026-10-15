param(
    [Parameter(Mandatory=$true)][string]$PlanPath,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$')][string]$RecoveryName = 'CurrentSceneHydration20261014'
)
$ErrorActionPreference = 'Stop'
$plan = Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
if ($plan.state -ne 'read-only-current-saved-scene-hydration-plan') { throw 'Unexpected hydration plan schema.' }
$repository = [IO.Path]::GetFullPath($plan.repository).TrimEnd('\')
$candidate = [IO.Path]::GetFullPath($plan.candidate).TrimEnd('\')
if ($repository -eq $candidate -or -not (Test-Path -LiteralPath (Join-Path $repository '.git')) -or
    (Test-Path -LiteralPath (Join-Path $candidate '.git')) -or
    -not (Test-Path -LiteralPath (Join-Path $candidate '.animation-playground-validation-project'))) { throw 'Invalid source or candidate.' }
if (@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.Contains($candidate)
}).Count) { throw 'The task-owned candidate Editor must finish exiting before transfer.' }
function Within([string]$Base,[string]$Relative) {
    $value = [IO.Path]::GetFullPath((Join-Path $Base $Relative))
    if (-not $value.StartsWith($Base+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Transfer path escaped its root.' }
    $value
}
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant() }
$recovery = Within $candidate $RecoveryName
if (Test-Path -LiteralPath $recovery) { throw 'Preserve the existing hydration recovery; do not replay.' }
foreach ($file in $plan.files) {
    if (-not $file.path.StartsWith('Assets/',[StringComparison]::Ordinal)) { throw 'Only Assets belong in this transfer.' }
    $source = Within $repository $file.path
    $target = Within $candidate $file.path
    if ((Hash $source) -ne $file.sha256) { throw "Source changed since plan: $($file.path)" }
    if ($null -ne $file.candidateBefore) {
        if (-not (Test-Path -LiteralPath $target) -or (Hash $target) -ne $file.candidateBefore) { throw "Candidate changed since plan: $($file.path)" }
    } elseif (Test-Path -LiteralPath $target) { throw "A candidate file appeared since plan: $($file.path)" }
}
[IO.Directory]::CreateDirectory($recovery) | Out-Null
Copy-Item -LiteralPath $PlanPath -Destination (Join-Path $recovery 'plan.json')
$copied = 0
foreach ($file in $plan.files) {
    if (-not $file.transfer) { continue }
    $source = Within $repository $file.path
    $target = Within $candidate $file.path
    if (Test-Path -LiteralPath $target) {
        $backup = Within $recovery ('Before/'+$file.path)
        [IO.Directory]::CreateDirectory((Split-Path $backup -Parent)) | Out-Null
        Copy-Item -LiteralPath $target -Destination $backup
        if ((Hash $backup) -ne $file.candidateBefore) { throw 'Recovery copy mismatch.' }
    }
    [IO.Directory]::CreateDirectory((Split-Path $target -Parent)) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
    if ((Hash $target) -ne $file.sha256) { throw 'Transfer verification mismatch.' }
    $copied++
}
foreach ($file in $plan.files) {
    if ((Hash (Within $candidate $file.path)) -ne $file.sha256) { throw 'Final candidate closure mismatch.' }
}
$receipt = [ordered]@{state='current-saved-scene-closure-copied-awaiting-unity-import';
    copiedFiles=$copied; verifiedFiles=$plan.files.Count; repositoryWritten=$false;
    planSha256=(Hash (Join-Path $recovery 'plan.json')); recovery=$recovery}
$json=($receipt|ConvertTo-Json -Depth 5).Replace([string][char]13,'')+[char]10
[IO.File]::WriteAllText((Join-Path $recovery 'receipt.json'),$json,[Text.UTF8Encoding]::new($false))
$json
