param(
    [string]$CandidateRoot = 'D:\BooterBigArmValidation\AnimationPlayground20261013',
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$PrivateManifest = 'D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13\ImportCheckpoints\PHY1LowMotionQualified\bundle-manifest.json',
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$candidate = [IO.Path]::GetFullPath($CandidateRoot).TrimEnd('\')
$repository = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
if ($candidate -eq $repository -or (Test-Path -LiteralPath (Join-Path $candidate '.git')) -or
    -not (Test-Path -LiteralPath (Join-Path $candidate '.animation-playground-validation-project'))) {
    throw 'Use the marked, non-Git isolated candidate.'
}
$running = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.Contains($candidate)
})
if ($running.Count) { throw 'Close the task-owned candidate Editor before reference repair.' }
function Within([string]$Base, [string]$Relative) {
    $path = [IO.Path]::GetFullPath((Join-Path $Base $Relative))
    if (-not $path.StartsWith($Base + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Path escapes its verified root.' }
    $path
}
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant() }
function GuidOf([string]$Path) {
    $match = [regex]::Match([IO.File]::ReadAllText($Path), '(?m)^guid: ([a-f0-9]{32})')
    if (-not $match.Success) { throw "Missing metadata GUID: $Path" }
    $match.Groups[1].Value
}
$recovery = Within $candidate 'AssetAliasRecovery20261014'
if (Test-Path -LiteralPath $recovery) { throw 'Preserve the existing recovery receipt; do not replay the move batch.' }
$seal = Get-Content -LiteralPath $PrivateManifest -Raw | ConvertFrom-Json
$canonical = @{}
foreach ($file in $seal.files) {
    $source = Within $repository $file.path
    if ((Hash $source) -ne $file.sha256 -or (Hash ($source + '.meta')) -ne $file.meta_sha256) {
        throw "The author's private seal has changed: $($file.path)"
    }
    $canonical[$file.path + '.meta'] = $source + '.meta'
}
$mixamo = Within $repository 'Assets/_Project/Art/Animations/Mixamo'
foreach ($metadata in @(Get-ChildItem -LiteralPath $mixamo -Recurse -File -Filter '*.meta') +
    @(Get-Item -LiteralPath ($mixamo + '.meta')) + @(Get-Item -LiteralPath ((Split-Path $mixamo -Parent) + '.meta'))) {
    $relative = [IO.Path]::GetRelativePath($repository, $metadata.FullName).Replace('\', '/')
    $canonical[$relative] = $metadata.FullName
}
$byGuid = @{}
foreach ($relative in $canonical.Keys) {
    $guid = GuidOf $canonical[$relative]
    if ($byGuid.ContainsKey($guid)) { throw 'The canonical source itself has duplicate GUIDs.' }
    $byGuid[$guid] = $relative
}
$aliases = @()
foreach ($metadata in Get-ChildItem -LiteralPath (Within $candidate 'Assets') -Recurse -File -Filter '*.meta') {
    $guid = GuidOf $metadata.FullName
    if (-not $byGuid.ContainsKey($guid)) { continue }
    $relative = [IO.Path]::GetRelativePath($candidate, $metadata.FullName).Replace('\', '/')
    if ($relative -eq $byGuid[$guid]) { continue }
    $sourceMetadata = $canonical[$byGuid[$guid]]
    $sourceAsset = $sourceMetadata.Substring(0, $sourceMetadata.Length - 5)
    $aliasAsset = $metadata.FullName.Substring(0, $metadata.FullName.Length - 5)
    $folder = Test-Path -LiteralPath $sourceAsset -PathType Container
    if (-not $folder -and ((Hash $aliasAsset) -ne (Hash $sourceAsset) -or (Hash $metadata.FullName) -ne (Hash $sourceMetadata))) {
        throw "An alias contains distinct content; preserve and investigate: $relative"
    }
    $aliases += [pscustomobject]@{ canonical = $byGuid[$guid]; alias = $relative; folderMetadataOnly = $folder; guid = $guid }
}
$restores = @()
foreach ($relative in $canonical.Keys) {
    $target = Within $candidate $relative
    if (-not (Test-Path -LiteralPath $target)) { throw "Expected candidate metadata is missing: $relative" }
    if ((Hash $target) -ne (Hash $canonical[$relative])) { $restores += $relative }
}
if ($Apply) {
    [IO.Directory]::CreateDirectory($recovery) | Out-Null
    foreach ($alias in $aliases) {
        $items = @($alias.alias)
        if (-not $alias.folderMetadataOnly) { $items = @($alias.alias.Substring(0, $alias.alias.Length - 5)) + $items }
        foreach ($relative in $items) {
            $source = Within $candidate $relative
            if (-not $source.StartsWith((Within $candidate 'Assets') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Alias left Assets.' }
            $destination = Within $recovery ('Aliases/' + $relative)
            [IO.Directory]::CreateDirectory((Split-Path $destination -Parent)) | Out-Null
            $expected = Hash $source
            Move-Item -LiteralPath $source -Destination $destination
            if ((Hash $destination) -ne $expected) { throw 'Quarantine transfer mismatch.' }
        }
    }
    foreach ($relative in $restores) {
        $target = Within $candidate $relative
        $backup = Within $recovery ('CanonicalBefore/' + $relative)
        [IO.Directory]::CreateDirectory((Split-Path $backup -Parent)) | Out-Null
        if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination $backup }
        Copy-Item -LiteralPath $canonical[$relative] -Destination $target
        if ((Hash $target) -ne (Hash $canonical[$relative])) { throw 'Canonical metadata restoration mismatch.' }
    }
    $receipt = [ordered]@{ state = 'aliases-quarantined-canonical-metadata-restored-awaiting-unity-reimport';
        authorRootUnchanged = $true; aliases = $aliases; restoredMetadata = $restores }
    $json = ($receipt | ConvertTo-Json -Depth 6).Replace(([string][char]13 + [char]10), [string][char]10)
    [IO.File]::WriteAllText((Join-Path $recovery 'receipt.json'), ($json + [char]10), [Text.UTF8Encoding]::new($false))
}
[pscustomobject]@{ aliases = $aliases.Count; metadataRestores = $restores.Count; applied = [bool]$Apply; recovery = $recovery } | ConvertTo-Json
