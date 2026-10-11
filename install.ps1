# Sets ooga up on Windows, for you only (no administrator rights needed):
#   1. "ooga" works in any folder and any terminal, like "python" (adds this folder to your PATH)
#   2. .ooga files get the ooga icon, and double-clicking one runs it
#   3. the Ooga VS Code extension: colours, push-in, file icon and a Run button
#
# Run:   powershell -ExecutionPolicy Bypass -File .\install.ps1
# Undo:  powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
# Safe to run again (for example after moving the ooga folder).

param([switch]$NoVSCode)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'setup\windows.ps1')
Assert-Windows

foreach ($needed in 'ooga.cmd', 'ooga-run.cmd', 'bin\ooga.exe', 'icons\ooga.ico') {
    if (-not (Test-Path (Join-Path $root $needed))) {
        Write-Host "ooga no find $needed next to install.ps1. run install.ps1 from inside the ooga folder."
        exit 1
    }
}

Write-Host "setting up ooga from: $root"
Write-Host ""

# 1. PATH
$before = Get-UserPath
$after = Add-PathEntry $before $root
if ($after -ne $before) {
    Set-UserPath $after
    Send-EnvironmentChanged
    Write-Host "[1/3] ooga added to your PATH."
} else {
    Write-Host "[1/3] ooga was already on your PATH."
}
if (-not (Test-PathEntry $env:Path $root)) { $env:Path = "$env:Path;$root" }

# 2. .ooga files
$code = Find-VSCode
Register-OogaFiles $root ($code.Exe)
Write-Host "[2/3] .ooga files now have the ooga icon. double-click one to run it; right-click to edit it."

# 3. VS Code extension
if ($NoVSCode) {
    Write-Host "[3/3] VS Code extension skipped (-NoVSCode)."
} elseif (-not $code) {
    Write-Host "[3/3] VS Code not found, so the extension was skipped. install VS Code, then run install.ps1 again."
} else {
    & (Join-Path $root 'vscode-ooga\pack.ps1')
    if ($LASTEXITCODE -eq 0) { Write-Host "[3/3] Ooga extension installed in VS Code." }
    else { Write-Host "[3/3] the VS Code extension did not install (see above). everything else is done." }
}

Write-Host ""
Write-Host "ooga ready! open a NEW terminal (or restart VS Code), then try, from any folder:"
Write-Host "    ooga $root\examples\01_hello.ooga"
