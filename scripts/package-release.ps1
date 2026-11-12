# Builds the release zip into dist\EmilyIsAway3Access.zip.
# Usage (from the repo root):
#   powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
# The zip mirrors the game folder, so extracting it into the game folder installs the mod
# (BepInEx 5 x86 itself is installed separately, by the installer or by hand):
#   BepInEx/plugins/EmilyIsAway3Access/      the mod + Prism
#   EmilyIsAway3Access-README.md, EmilyIsAway3Access-CHANGELOG.md

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# SkipDeploy: a release build doesn't touch the local game install.
# DevTools=false: never ship the private test driver, even on a machine that has it.
$out = Join-Path $root "src\EmilyIsAway3Access\bin\ReleasePackage"
dotnet build src\EmilyIsAway3Access -c Release -p:SkipDeploy=true -p:DevTools=false -o $out
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$mod = "BepInEx/plugins/EmilyIsAway3Access"
# Source path (relative to the repo root) -> path inside the zip.
$files = [ordered]@{
    "src\EmilyIsAway3Access\bin\ReleasePackage\EmilyIsAway3Access.dll" = "$mod/EmilyIsAway3Access.dll"
    "prism\prism.dll"                                                  = "$mod/prism.dll"
    "prism\LICENSE"                                                    = "$mod/Prism/LICENSE"
    "prism\NOTICE"                                                     = "$mod/Prism/NOTICE"
    "README.md"                                                        = "EmilyIsAway3Access-README.md"
    "CHANGELOG.md"                                                     = "EmilyIsAway3Access-CHANGELOG.md"
}
$folders = [ordered]@{ "prism\LICENSES" = "$mod/Prism/LICENSES" }

$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$zip = Join-Path $dist "EmilyIsAway3Access.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }

# Write entries with forward slashes (Windows PowerShell's ZipFile writes backslashes).
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($key in $files.Keys) {
        $full = Join-Path $root $key
        if (-not (Test-Path $full)) { throw "Missing release file: $full" }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $full, $files[$key]) | Out-Null
    }
    foreach ($key in $folders.Keys) {
        $full = Join-Path $root $key
        if (-not (Test-Path $full)) { throw "Missing release folder: $full" }
        foreach ($item in Get-ChildItem $full -Recurse -File) {
            $name = $folders[$key] + "/" + $item.FullName.Substring($full.Length + 1).Replace([char]92, [char]47)
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $item.FullName, $name) | Out-Null
        }
    }
} finally {
    $archive.Dispose()
}
Remove-Item $out -Recurse -Force
Write-Host "Built: $zip"
