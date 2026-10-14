param(
    [ValidateSet('StageSources', 'Restore', 'Capture')][string]$Mode = 'StageSources',
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path,
    [string]$DownloadsRoot = (Join-Path $env:USERPROFILE 'Downloads'),
    [string]$PrivateRoot = 'D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13',
    [string]$PlanPath = 'Docs/Design/Gameplay/AnimationPlayground20261013/asset-import-plan.json',
    [string[]]$AdditionalPrivatePaths = @(),
    [switch]$AllowFirstImport,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'

function Resolve-Within([string]$Root, [string]$Relative) {
    if ([IO.Path]::IsPathRooted($Relative)) { throw "Expected a relative path: $Relative" }
    $absoluteRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $absolute = [IO.Path]::GetFullPath((Join-Path $Root $Relative))
    if (-not $absolute.StartsWith($absoluteRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes its root: $Relative"
    }
    return $absolute
}
function Hash-File([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Assert-File([string]$Path, [string]$Hash, [long]$Bytes) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -ne $Bytes -or (Hash-File $Path) -cne $Hash) {
        throw "Source size/hash mismatch: $Path"
    }
}
function Copy-Verified([string]$Source, [string]$Destination, [string]$ExpectedHash) {
    if (Test-Path -LiteralPath $Destination) {
        if ((Hash-File $Destination) -cne $ExpectedHash) { throw "Destination differs; preserved: $Destination" }
        return
    }
    if (-not $Apply) { return }
    [IO.Directory]::CreateDirectory((Split-Path $Destination -Parent)) | Out-Null
    $temporary = $Destination + '.transfer-' + [Guid]::NewGuid().ToString('N')
    try {
        [IO.File]::Copy($Source, $temporary, $false)
        if ((Hash-File $temporary) -cne $ExpectedHash) { throw "Copy verification failed: $Destination" }
        [IO.File]::Move($temporary, $Destination)
    } finally {
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
    }
}
function Is-PrivateUnityPath([string]$Relative) {
    $relative = $Relative.Replace('\', '/')
    return $relative -match '^Assets/_Project/Art/Animations/Mixamo/.+\.(fbx|anim)$' -or
        $relative -match '^Assets/_Project/Art/Characters/AnimationPlayground/.+\.fbx$' -or
        $relative -match '^Assets/_Project/Settings/AnimationPlayground/PrivateBindings/[^/]+\.asset$'
}
function Assert-Ignored([string]$Relative) {
    & git -C $RepositoryRoot check-ignore --quiet --no-index -- $Relative
    if ($LASTEXITCODE -ne 0) { throw "Private dependency is not ignored: $Relative" }
    $tracked = @(& git -C $RepositoryRoot ls-files -- $Relative)
    if ($tracked.Count -ne 0) { throw "Private dependency is already tracked: $Relative" }
}
function Read-Guid([string]$Metadata) {
    $match = [regex]::Match([IO.File]::ReadAllText($Metadata), '(?m)^guid: ([0-9a-f]{32})\r?$')
    if (-not $match.Success) { throw "Invalid Unity metadata GUID: $Metadata" }
    return $match.Groups[1].Value
}
function Assert-GuidOwnership([string]$Guid, [string]$Relative) {
    if (-not $guidIndex.ContainsKey($Guid)) { return }
    foreach ($owner in $guidIndex[$Guid]) {
        if ($owner -cne $Relative.Replace('\', '/')) { throw "GUID is already owned by another asset: $owner" }
    }
}
function Write-Json([string]$Path, $Value) {
    if (-not $Apply) { return }
    [IO.Directory]::CreateDirectory((Split-Path $Path -Parent)) | Out-Null
    $temporary = $Path + '.new-' + [Guid]::NewGuid().ToString('N')
    [IO.File]::WriteAllText($temporary, (($Value | ConvertTo-Json -Depth 16).Replace("`r`n", "`n") + "`n"), [Text.UTF8Encoding]::new($false))
    [IO.File]::Move($temporary, $Path, $true)
}

$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$PrivateRoot = [IO.Path]::GetFullPath($PrivateRoot)
if ($PrivateRoot.StartsWith($RepositoryRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Private recovery bundle must be outside the repository.'
}
$plan = Get-Content -Raw -LiteralPath (Resolve-Within $RepositoryRoot $PlanPath) | ConvertFrom-Json
if ($plan.schema_version -ne 2) { throw 'Unsupported animation transfer schema.' }
$items = [Collections.Generic.List[object]]::new()
$identities = @{}
foreach ($asset in $plan.assets) {
    if ($identities.ContainsKey($asset.destination)) { throw 'Duplicate destination in transfer plan.' }
    $identities[$asset.destination] = $true
    $items.Add([pscustomobject]@{ id = $asset.asset_id; destination = $asset.destination; hash = $asset.source_sha256; bytes = $asset.source_bytes; sources = $asset.sources; isUnity = $asset.destination.StartsWith('Assets/') })
}
foreach ($rig in $plan.target_rig_candidates | Where-Object { -not $_.reuses_canonical_source_destination }) {
    $rigSource = Resolve-Within $DownloadsRoot $rig.source_file
    if ($Mode -eq 'StageSources' -and (Hash-File $rigSource) -cne $rig.source_sha256) { throw 'Target rig source hash differs.' }
    $items.Add([pscustomobject]@{ id = 'rig.' + $rig.bone_count; destination = $rig.planned_qualification_destination; hash = $rig.source_sha256; bytes = $null; sources = @([pscustomobject]@{path=$rig.source_file}); isUnity=$true })
}
$bundleManifest = Resolve-Within $PrivateRoot 'bundle-manifest.json'
$operations = [Collections.Generic.List[object]]::new()
$guidIndex = @{}
if ($Mode -ne 'StageSources') {
    foreach ($metadata in Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'Assets') -Filter '*.meta' -Recurse -File) {
        $guid = Read-Guid $metadata.FullName
        $relative = $metadata.FullName.Substring($RepositoryRoot.Length + 1, $metadata.FullName.Length - $RepositoryRoot.Length - 6).Replace('\', '/')
        if (-not $guidIndex.ContainsKey($guid)) { $guidIndex[$guid] = [Collections.Generic.List[string]]::new() }
        $guidIndex[$guid].Add($relative)
    }
}

if ($Mode -eq 'StageSources') {
    foreach ($item in $items) {
        $first = $null
        foreach ($alias in $item.sources) {
            $source = Resolve-Within $DownloadsRoot $alias.path
            $bytes = if ($null -eq $item.bytes) { (Get-Item -LiteralPath $source).Length } else { $item.bytes }
            Assert-File $source $item.hash $bytes
            if ($null -eq $first) { $first = $source }
        }
        $archived = Resolve-Within $PrivateRoot ('Originals/' + $item.destination)
        Copy-Verified $first $archived $item.hash
        $operations.Add([pscustomobject]@{id=$item.id;path=$item.destination;sha256=$item.hash;bytes=(Get-Item -LiteralPath $first).Length;aliases=$item.sources.Count})
    }
    Write-Json (Resolve-Within $PrivateRoot 'source-receipt.json') ([ordered]@{schema=1;plan_hash=(Hash-File (Resolve-Within $RepositoryRoot $PlanPath));sources=$operations;source_aliases=(@($operations | ForEach-Object {$_.aliases}) | Measure-Object -Sum).Sum;originals_preserved=$true})
} elseif ($Mode -eq 'Restore') {
    if (Test-Path -LiteralPath $bundleManifest) {
        $sealed = Get-Content -Raw -LiteralPath $bundleManifest | ConvertFrom-Json
        if ($sealed.schema -ne 1) { throw 'Unsupported recovery bundle schema.' }
        $incomingGuids = @{}; $incomingPaths = @{}
        # Preflight every pair before writing anything. GUID replacement is never a restoration operation.
        foreach ($file in $sealed.files) {
            if (-not (Is-PrivateUnityPath $file.path)) { throw 'Bundle contains an unauthorized Unity path.' }
            if ($incomingPaths.ContainsKey($file.path)) { throw 'Bundle contains a duplicate destination.' }
            $incomingPaths[$file.path] = $true
            Assert-Ignored $file.path; Assert-Ignored ($file.path + '.meta')
            Assert-File (Resolve-Within $PrivateRoot ('Files/' + $file.path)) $file.sha256 $file.bytes
            Assert-File (Resolve-Within $PrivateRoot ('Files/' + $file.path + '.meta')) $file.meta_sha256 $file.meta_bytes
            $actualGuid = Read-Guid (Resolve-Within $PrivateRoot ('Files/' + $file.path + '.meta'))
            if ($actualGuid -cne $file.guid) { throw 'Sealed metadata GUID differs from its receipt.' }
            if ($incomingGuids.ContainsKey($actualGuid)) { throw 'Bundle contains duplicate Unity GUIDs.' }
            $incomingGuids[$actualGuid] = $true
            Assert-GuidOwnership $actualGuid $file.path
            foreach ($pair in @(@($file.path,$file.sha256), @($file.path+'.meta',$file.meta_sha256))) {
                $existing = Resolve-Within $RepositoryRoot $pair[0]
                if ((Test-Path -LiteralPath $existing) -and (Hash-File $existing) -cne $pair[1]) { throw "Existing dependency differs; preserved: $existing" }
            }
        }
        foreach ($file in $sealed.files) {
            # Caller pauses AssetDatabase refresh while restoring a sealed bundle.
            Copy-Verified (Resolve-Within $PrivateRoot ('Files/' + $file.path + '.meta')) (Resolve-Within $RepositoryRoot ($file.path + '.meta')) $file.meta_sha256
            Copy-Verified (Resolve-Within $PrivateRoot ('Files/' + $file.path)) (Resolve-Within $RepositoryRoot $file.path) $file.sha256
            $operations.Add($file)
        }
    } else {
        if (-not $AllowFirstImport) { throw 'No sealed metadata bundle exists. First import requires -AllowFirstImport.' }
        foreach ($item in $items | Where-Object isUnity) {
            Assert-Ignored $item.destination; Assert-Ignored ($item.destination + '.meta')
            $original = Resolve-Within $PrivateRoot ('Originals/' + $item.destination)
            Assert-File $original $item.hash (Get-Item -LiteralPath $original).Length
            $destination = Resolve-Within $RepositoryRoot $item.destination
            if (Test-Path -LiteralPath ($destination + '.meta')) { throw 'First import cannot adopt existing metadata; use sealed restoration.' }
            if ((Test-Path -LiteralPath $destination) -and (Hash-File $destination) -cne $item.hash) { throw 'First import destination differs.' }
        }
        foreach ($item in $items | Where-Object isUnity) {
            Copy-Verified (Resolve-Within $PrivateRoot ('Originals/' + $item.destination)) (Resolve-Within $RepositoryRoot $item.destination) $item.hash
            $operations.Add([pscustomobject]@{path=$item.destination;sha256=$item.hash;metadata='pending-first-import'})
        }
    }
} else {
    $paths = @($items | Where-Object isUnity | ForEach-Object destination) + $AdditionalPrivatePaths
    $guidOwners = @{}
    foreach ($relative in $paths | Sort-Object -Unique) {
        if (-not (Is-PrivateUnityPath $relative)) { throw 'Capture contains an unauthorized private path.' }
        Assert-Ignored $relative; Assert-Ignored ($relative + '.meta')
        $payload = Resolve-Within $RepositoryRoot $relative
        $metadata = $payload + '.meta'
        if (-not (Test-Path -LiteralPath $payload) -or -not (Test-Path -LiteralPath $metadata)) { throw "Capture requires a complete imported pair: $relative" }
        $guid = Read-Guid $metadata
        if ($guidOwners.ContainsKey($guid)) { throw 'Duplicate dependency GUID.' }
        Assert-GuidOwnership $guid $relative
        $guidOwners[$guid] = $relative
        $hash = Hash-File $payload; $metaHash = Hash-File $metadata
        Copy-Verified $payload (Resolve-Within $PrivateRoot ('Files/' + $relative)) $hash
        Copy-Verified $metadata (Resolve-Within $PrivateRoot ('Files/' + $relative + '.meta')) $metaHash
        $operations.Add([pscustomobject]@{path=$relative;sha256=$hash;bytes=(Get-Item -LiteralPath $payload).Length;guid=$guid;meta_sha256=$metaHash;meta_bytes=(Get-Item -LiteralPath $metadata).Length})
    }
    Write-Json $bundleManifest ([ordered]@{schema=1;files=$operations;pairing='private-payload-and-metadata';captured_utc=[DateTime]::UtcNow.ToString('o')})
}
[pscustomobject]@{ mode=$Mode;applied=[bool]$Apply;operations=$operations.Count;private_root=$PrivateRoot;source_plan=$PlanPath } | ConvertTo-Json -Compress
