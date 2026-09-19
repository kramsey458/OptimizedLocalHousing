param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$modDir = Join-Path $PSScriptRoot 'OptimizedLocalHousing'
$version = ([xml](Get-Content -LiteralPath (Join-Path $modDir 'OptimizedLocalHousing.csproj'))).Project.PropertyGroup.Version
if (-not $version) { throw 'Version not found in the project file' }
$manifestVersion = (Get-Content -LiteralPath (Join-Path $modDir 'manifest.json') -Raw | ConvertFrom-Json).Version
if ($manifestVersion -ne $version) { throw "manifest.json version $manifestVersion does not match project version $version" }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$target = Join-Path $OutputDirectory "OptimizedLocalHousing-v$version.zip"
if (Test-Path $target) { Remove-Item -LiteralPath $target }
$payload = [ordered]@{
    'OptimizedLocalHousing/version-1.1/Scripts/OptimizedLocalHousing.dll' = (Join-Path $modDir 'bin/Release/netstandard2.1/OptimizedLocalHousing.dll')
    'OptimizedLocalHousing/version-1.1/manifest.json' = (Join-Path $modDir 'manifest.json')
    'OptimizedLocalHousing/README.md' = (Join-Path $PSScriptRoot 'README.md')
    'OptimizedLocalHousing/LICENSE' = (Join-Path $PSScriptRoot 'LICENSE')
}
foreach ($file in $payload.Values) { if (-not (Test-Path -LiteralPath $file)) { throw "Missing $file (build in Release first)" } }
$zip = [System.IO.Compression.ZipFile]::Open($target, [System.IO.Compression.ZipArchiveMode]::Create)
try { foreach ($entry in $payload.GetEnumerator()) { [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null } }
finally { $zip.Dispose() }
# Read the archive back and compare every entry with its source file.
$zip = [System.IO.Compression.ZipFile]::OpenRead($target)
try {
    if ($zip.Entries.Count -ne $payload.Count) { throw 'Archive entry count mismatch' }
    foreach ($entry in $zip.Entries) {
        $stream = $entry.Open(); $hash = [System.Security.Cryptography.SHA256]::Create()
        try { $actual = [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-', '') } finally { $stream.Dispose(); $hash.Dispose() }
        if ($actual -ne (Get-FileHash -LiteralPath $payload[$entry.FullName] -Algorithm SHA256).Hash) { throw "Archive content mismatch: $($entry.FullName)" }
    }
} finally { $zip.Dispose() }
$sum = '{0}  {1}' -f (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path -Leaf $target)
$sum | Set-Content -LiteralPath (Join-Path $OutputDirectory "OptimizedLocalHousing-v$version-SHA256SUMS.txt")
Write-Output "Verified $($payload.Count) entries: $target"
Write-Output $sum
