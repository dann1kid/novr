# Install dist/NOVR.zip into the Steam Nuclear Option BepInEx folder.
# Default path is the current Steam install with BepInEx already present.
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option',
    [string]$ZipPath = ''
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    $ZipPath = Join-Path $root 'dist\NOVR.zip'
}

if (-not (Test-Path -LiteralPath $ZipPath)) {
    throw "NOVR.zip not found at $ZipPath. Build first: dotnet build NOVR.Build\NOVR.Build.csproj -c Release"
}

$managed = Join-Path $GameDir 'NuclearOption_Data\Managed'
$bepInEx = Join-Path $GameDir 'BepInEx'
if (-not (Test-Path -LiteralPath $managed)) {
    throw "Nuclear Option was not found at $GameDir (missing NuclearOption_Data\Managed)."
}
if (-not (Test-Path -LiteralPath $bepInEx)) {
    throw "BepInEx is not installed at $bepInEx. Install BepInEx 5.x first."
}

Write-Host "Installing NOVR into $bepInEx"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ZipPath))
try {
    foreach ($entry in $archive.Entries) {
        if ([string]::IsNullOrWhiteSpace($entry.Name) -and $entry.FullName.EndsWith('/')) {
            continue
        }
        $destination = Join-Path $bepInEx ($entry.FullName -replace '/', '\')
        $destinationDir = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $destinationDir)) {
            New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
        }
        if (-not $entry.FullName.EndsWith('/')) {
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
        }
    }
}
finally {
    $archive.Dispose()
}

$plugin = Join-Path $bepInEx 'plugins\NOVR\NOVR.dll'
$patcher = Join-Path $bepInEx 'patchers\NOVR\NOVR.Patcher.dll'
$version = Join-Path $bepInEx 'plugins\NOVR\version.txt'
if (-not (Test-Path -LiteralPath $plugin) -or -not (Test-Path -LiteralPath $patcher)) {
    throw "Install finished but NOVR.dll or NOVR.Patcher.dll is missing under $bepInEx."
}

Write-Host "Installed:"
Write-Host "  $plugin"
Write-Host "  $patcher"
if (Test-Path -LiteralPath $version) {
    Write-Host ("  version.txt = " + (Get-Content -LiteralPath $version -Raw).Trim())
}
Write-Host "Close Nuclear Option before launching from Steam."
