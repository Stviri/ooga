# Undoes install.ps1: takes ooga off your PATH, gives .ooga files back to Windows,
# and removes the Ooga VS Code extension. Your ooga folder and scripts are not touched.
#
# Run:  powershell -ExecutionPolicy Bypass -File .\uninstall.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'setup\windows.ps1')
Assert-Windows

$before = Get-UserPath
$after = Remove-PathEntry $before $root
if ($after -ne $before) {
    Set-UserPath $after
    Send-EnvironmentChanged
    Write-Host "[1/3] ooga taken off your PATH."
} else {
    Write-Host "[1/3] ooga was not on your PATH."
}

Unregister-OogaFiles
Write-Host "[2/3] .ooga files no longer have the ooga icon or double-click run."

$code = Find-VSCode
if ($code -and $code.Cli -and (Test-Path $code.Cli)) {
    & $code.Cli --uninstall-extension local.ooga-language | Out-Host
    Write-Host "[3/3] Ooga extension removed from VS Code."
} else {
    Write-Host "[3/3] VS Code not found, nothing to remove there."
}

Write-Host ""
Write-Host "ooga gone from Windows. to set it up again: powershell -ExecutionPolicy Bypass -File .\install.ps1"
