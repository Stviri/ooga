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

Write-Host ""
if ($failed -eq 0) { Write-Host "all setup checks ok." } else { Write-Host "$failed setup checks WRONG."; exit 1 }
