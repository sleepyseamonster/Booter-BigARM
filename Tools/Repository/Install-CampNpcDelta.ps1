param([string]$PayloadDirectory = 'Logs/NpcIntegration')
$ErrorActionPreference = 'Stop'
$npcRepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$npcPayloadRoot = [IO.Path]::GetFullPath((Join-Path $npcRepoRoot $PayloadDirectory))
if (-not $npcPayloadRoot.StartsWith((Join-Path $npcRepoRoot 'Logs') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Use a prepared payload under this repository Logs directory.'
}
$npcScenePath = Join-Path $npcRepoRoot 'Assets/_Project/Scenes/Reference/CampLoopTesting.unity'
$npcReceipt = Get-Content -LiteralPath (Join-Path $npcPayloadRoot 'scene-delta.json') -Raw | ConvertFrom-Json
$npcPayload = [IO.File]::ReadAllBytes((Join-Path $npcPayloadRoot 'CampLoopTesting.npc-candidate.unity'))
function Get-NpcHash([byte[]]$Bytes) { [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant() }
if ((Get-NpcHash $npcPayload) -ne $npcReceipt.result_sha256 -or $npcReceipt.added_documents -ne 4) { throw 'Invalid prepared NPC payload.' }
$npcEditors = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.IndexOf($npcRepoRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0
})
if ($npcEditors.Count -gt 0) { throw 'A Unity process references the author checkout; use an explicit owner handoff before writing.' }
# Scene-writer ownership must already be granted by the coordinating chats. The
# lock/hash protect against races; they do not substitute for that handoff.
$npcStream = [IO.File]::Open($npcScenePath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $npcBefore = [byte[]]::new($npcStream.Length)
    $npcRead = 0
    while ($npcRead -lt $npcBefore.Length) {
        $npcCount = $npcStream.Read($npcBefore, $npcRead, $npcBefore.Length - $npcRead)
        if ($npcCount -eq 0) { throw 'Could not capture complete current scene.' }
        $npcRead += $npcCount
    }
    if ((Get-NpcHash $npcBefore) -ne $npcReceipt.source_sha256) { throw 'Scene changed after payload preparation; preserve it and prepare again.' }
    [IO.File]::WriteAllBytes((Join-Path $npcPayloadRoot 'author-scene-before.unity'), $npcBefore)
    try {
        $npcStream.Position = 0
        $npcStream.Write($npcPayload, 0, $npcPayload.Length)
        $npcStream.SetLength($npcPayload.Length)
        $npcStream.Flush($true)
    }
    catch {
        $npcStream.Position = 0
        $npcStream.Write($npcBefore, 0, $npcBefore.Length)
        $npcStream.SetLength($npcBefore.Length)
        $npcStream.Flush($true)
        throw
    }
}
finally { $npcStream.Dispose() }
if ((Get-FileHash -LiteralPath $npcScenePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $npcReceipt.result_sha256) {
    throw 'Scene readback differs; preserve the baseline and diagnose before retrying.'
}
Write-Output 'NPC additive scene installed: four new records, one root entry; all previous documents preserved.'
