# Packs this folder into ooga-language.vsix and installs it into VS Code.
# Run again after changing the extension:
#     powershell -ExecutionPolicy Bypass -File .\vscode-ooga\pack.ps1
# -NoInstall only makes the .vsix file (used by checks).

param([switch]$NoInstall)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$here = $PSScriptRoot
$vsix = Join-Path $here "ooga-language.vsix"
$package = Get-Content (Join-Path $here "package.json") -Raw | ConvertFrom-Json
$version = $package.version
$engine = $package.engines.vscode

# Every file the extension needs, as it sits in this folder.
$files = @(
    "package.json",
    "extension.js",
    "language-configuration.json",
    "syntaxes/ooga.tmLanguage.json",
    "icons/ooga-128.png",
    "icons/ooga-file.png"
)

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011" xmlns:d="http://schemas.microsoft.com/developer/vsx-schema-design/2011">
  <Metadata>
    <Identity Language="en-US" Id="ooga-language" Version="$version" Publisher="local" />
    <DisplayName>Ooga</DisplayName>
    <Description xml:space="preserve">$($package.description)</Description>
    <Categories>Programming Languages</Categories>
    <Icon>extension/icons/ooga-128.png</Icon>
    <Properties>
      <Property Id="Microsoft.VisualStudio.Code.Engine" Value="$engine" />
    </Properties>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Code" />
  </Installation>
  <Dependencies />
  <Assets>
    <Asset Type="Microsoft.VisualStudio.Code.Manifest" Path="extension/package.json" Addressable="true" />
    <Asset Type="Microsoft.VisualStudio.Services.Icons.Default" Path="extension/icons/ooga-128.png" Addressable="true" />
  </Assets>
</PackageManifest>
"@

$contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension=".json" ContentType="application/json" />
  <Default Extension=".js" ContentType="application/javascript" />
  <Default Extension=".png" ContentType="image/png" />
  <Default Extension=".vsixmanifest" ContentType="text/xml" />
</Types>
"@

if (Test-Path $vsix) { Remove-Item $vsix }
$zip = [System.IO.Compression.ZipFile]::Open($vsix, "Create")
try {
    function Add-Text($name, $text) {
        $entry = $zip.CreateEntry($name)
        $w = New-Object System.IO.StreamWriter($entry.Open(), (New-Object System.Text.UTF8Encoding($false)))
        $w.Write($text)
        $w.Dispose()
    }
    Add-Text "extension.vsixmanifest" $manifest
    Add-Text "[Content_Types].xml" $contentTypes
    foreach ($f in $files) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $here $f), "extension/$f") | Out-Null
    }
}
finally {
    $zip.Dispose()
}
Write-Host "made $vsix (Ooga extension $version)"

if ($NoInstall) { return }

# Find VS Code: installed for this user, installed for everyone, or on PATH.
$code = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code\bin\code.cmd"),
    (Join-Path $env:ProgramFiles "Microsoft VS Code\bin\code.cmd")
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $code) {
    $onPath = Get-Command code -ErrorAction SilentlyContinue
    if ($onPath) { $code = $onPath.Source }
}
if (-not $code) {
    Write-Host ""
    Write-Host "ooga no find VS Code. Install it first, or install the extension by hand:"
    Write-Host "  in VS Code press Ctrl + Shift + P, type 'Install from VSIX', and pick:"
    Write-Host "  $vsix"
    exit 1
}

& $code --install-extension $vsix --force
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Ooga extension $version installed. Close and reopen VS Code to use it."
