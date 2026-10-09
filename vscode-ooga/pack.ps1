# Packs this folder into ooga-language.vsix and installs it into VS Code.
# Run again after changing the colors.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$here = $PSScriptRoot
$vsix = Join-Path $here "ooga-language.vsix"
$version = (Get-Content (Join-Path $here "package.json") -Raw | ConvertFrom-Json).version

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011" xmlns:d="http://schemas.microsoft.com/developer/vsx-schema-design/2011">
  <Metadata>
    <Identity Language="en-US" Id="ooga-language" Version="$version" Publisher="local" />
    <DisplayName>Ooga</DisplayName>
    <Description xml:space="preserve">Colors for the ooga language</Description>
    <Categories>Programming Languages</Categories>
    <Properties>
      <Property Id="Microsoft.VisualStudio.Code.Engine" Value="^1.60.0" />
    </Properties>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Code" />
  </Installation>
  <Dependencies />
  <Assets>
    <Asset Type="Microsoft.VisualStudio.Code.Manifest" Path="extension/package.json" Addressable="true" />
  </Assets>
</PackageManifest>
"@

$contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension=".json" ContentType="application/json" />
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
    foreach ($f in "package.json", "language-configuration.json", "syntaxes/ooga.tmLanguage.json") {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $here $f), "extension/$f") | Out-Null
    }
}
finally {
    $zip.Dispose()
}

$code = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code\bin\code.cmd"
& $code --install-extension $vsix --force
