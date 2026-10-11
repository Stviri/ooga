# Checks the plain-text helpers in setup/windows.ps1 (they need no Windows, so they run anywhere).
# Run:  powershell -ExecutionPolicy Bypass -File .\tests\setup\windows-setup.tests.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot)
. (Join-Path $root 'setup\windows.ps1')

$failed = 0
function Check([string]$Name, $Got, $Want) {
    if ($Got -ceq $Want) { Write-Host "ok     $Name" }
    else { Write-Host "WRONG  $Name`n         want: $Want`n         got:  $Got"; $script:failed++ }
}

$ooga = 'C:\Users\Joni Smith\Desktop\ooga'
Check 'adds folder at the end'         (Add-PathEntry 'C:\Windows;%USERPROFILE%\bin' $ooga) "C:\Windows;%USERPROFILE%\bin;$ooga"
Check 'adds to an empty PATH'          (Add-PathEntry '' $ooga) $ooga
Check 'skips empty pieces'             (Add-PathEntry 'C:\a;;C:\b;' $ooga) "C:\a;C:\b;$ooga"
Check 'no second copy'                 (Add-PathEntry "C:\a;$ooga" $ooga) "C:\a;$ooga"
Check 'no copy when case differs'      (Add-PathEntry 'C:\a;c:\users\joni smith\desktop\OOGA' $ooga) 'C:\a;c:\users\joni smith\desktop\OOGA'
Check 'no copy with trailing \'        (Add-PathEntry "C:\a;$ooga\" $ooga) "C:\a;$ooga\"
Check 'keeps %VARIABLES% as written'   (Add-PathEntry '%USERPROFILE%\x' $ooga) "%USERPROFILE%\x;$ooga"
Check 'finds it'                       (Test-PathEntry "C:\a;$ooga" $ooga) $true
Check 'does not find a longer name'    (Test-PathEntry "C:\a;$ooga-old" $ooga) $false
Check 'removes it'                     (Remove-PathEntry "C:\a;$ooga;C:\b" $ooga) 'C:\a;C:\b'
Check 'removes every copy'             (Remove-PathEntry "$ooga;C:\a;$ooga\" $ooga) 'C:\a'
Check 'remove leaves others alone'     (Remove-PathEntry 'C:\a;C:\b' $ooga) 'C:\a;C:\b'
Check 'add then remove = start'        (Remove-PathEntry (Add-PathEntry 'C:\a;C:\b' $ooga) $ooga) 'C:\a;C:\b'
Check 'double-click command'           (Get-OpenCommand 'C:\WINDOWS\system32\cmd.exe' "$ooga\ooga-run.cmd") `
                                       ('"C:\WINDOWS\system32\cmd.exe" /c ""C:\Users\Joni Smith\Desktop\ooga\ooga-run.cmd" "%1""')

$runtimes = @('Microsoft.AspNetCore.App 8.0.10 [C:\x]', 'Microsoft.NETCore.App 8.0.10 [C:\x]')
Check 'no desktop runtime'             (Test-DesktopRuntime $runtimes) $false
Check 'desktop runtime 8'              (Test-DesktopRuntime ($runtimes + 'Microsoft.WindowsDesktop.App 8.0.10 [C:\x]')) $true
Check 'desktop runtime 10 is fine too' (Test-DesktopRuntime @('Microsoft.WindowsDesktop.App 10.0.1 [C:\x]')) $true
Check 'desktop runtime 6 is too old'   (Test-DesktopRuntime @('Microsoft.WindowsDesktop.App 6.0.35 [C:\x]')) $false

Write-Host ""
if ($failed -eq 0) { Write-Host "all setup checks ok." } else { Write-Host "$failed setup checks WRONG."; exit 1 }
