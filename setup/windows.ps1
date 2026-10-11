# Helpers for install.ps1 and uninstall.ps1. Everything here only touches the current Windows user:
# no administrator rights are needed, and other users of the computer are not changed.

# ---------- plain text helpers (no Windows needed, so they can be checked anywhere) ----------

# Is this folder already in a PATH list? (Windows ignores case and a trailing \)
function Test-PathEntry([string]$PathList, [string]$Folder) {
    $want = $Folder.TrimEnd('\')
    foreach ($part in ($PathList -split ';')) {
        if ($part.TrimEnd('\') -ieq $want) { return $true }
    }
    return $false
}

# The PATH list with the folder added at the end (unchanged if it is already there).
function Add-PathEntry([string]$PathList, [string]$Folder) {
    if (Test-PathEntry $PathList $Folder) { return $PathList }
    $parts = @($PathList -split ';' | Where-Object { $_ -ne '' })
    return (@($parts) + $Folder) -join ';'
}

# The PATH list with every copy of the folder taken out.
function Remove-PathEntry([string]$PathList, [string]$Folder) {
    $want = $Folder.TrimEnd('\')
    $parts = @($PathList -split ';' | Where-Object { $_ -ne '' -and $_.TrimEnd('\') -ine $want })
    return $parts -join ';'
}

# What Windows runs when a .ooga file is double-clicked: ooga-run.cmd with the file, in a console window.
function Get-OpenCommand([string]$ComSpec, [string]$Runner) {
    return "`"$ComSpec`" /c `"`"$Runner`" `"%1`"`""
}

# ---------- Windows-only helpers ----------

function Assert-Windows {
    if ([System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
        Write-Host "this script is for Windows. on Linux or macOS, run ooga with ./ooga.sh"
        exit 1
    }
}

# The user's own PATH, exactly as stored (entries like %USERPROFILE%\x stay as they are).
function Get-UserPath {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment')
    try { return [string]$key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) }
    finally { $key.Close() }
}

function Set-UserPath([string]$PathList) {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
    try { $key.SetValue('Path', $PathList, [Microsoft.Win32.RegistryValueKind]::ExpandString) }
    finally { $key.Close() }
}

Add-Type -Namespace OogaSetup -Name Native -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
public static extern System.IntPtr SendMessageTimeout(System.IntPtr hWnd, uint msg, System.UIntPtr wParam, string lParam, uint flags, uint timeout, out System.UIntPtr result);
[System.Runtime.InteropServices.DllImport("shell32.dll")]
public static extern void SHChangeNotify(int eventId, uint flags, System.IntPtr item1, System.IntPtr item2);
'@ -ErrorAction SilentlyContinue

# Tell Windows the PATH changed, so new terminals see it without logging out.
function Send-EnvironmentChanged {
    $result = [UIntPtr]::Zero
    [OogaSetup.Native]::SendMessageTimeout([IntPtr]0xffff, 0x1A, [UIntPtr]::Zero, 'Environment', 2, 5000, [ref]$result) | Out-Null
}

# Tell File Explorer the .ooga icon and double-click changed.
function Send-AssociationsChanged {
    [OogaSetup.Native]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
}

$ClassesRoot = 'HKCU:\Software\Classes'
$ProgId = 'Ooga.Script'

function Set-Default([string]$KeyPath, [string]$Value) {
    if (-not (Test-Path $KeyPath)) { New-Item -Path $KeyPath -Force | Out-Null }
    Set-Item -Path $KeyPath -Value $Value
}

# .ooga files: the ooga icon, double-click runs them, right-click can still edit them.
function Register-OogaFiles([string]$Root, [string]$CodeExe) {
    $icon = Join-Path $Root 'icons\ooga.ico'
    $runner = Join-Path $Root 'ooga-run.cmd'
    Set-Default "$ClassesRoot\.ooga" $ProgId
    New-ItemProperty -Path "$ClassesRoot\.ooga" -Name 'PerceivedType' -Value 'text' -Force | Out-Null
    Set-Default "$ClassesRoot\$ProgId" 'Ooga script'
    Set-Default "$ClassesRoot\$ProgId\DefaultIcon" "`"$icon`""
    Set-Default "$ClassesRoot\$ProgId\shell" 'open'
    Set-Default "$ClassesRoot\$ProgId\shell\open" 'Run with Ooga'
    Set-Default "$ClassesRoot\$ProgId\shell\open\command" (Get-OpenCommand $env:ComSpec $runner)
    Set-Default "$ClassesRoot\$ProgId\shell\edit" 'Edit in Notepad'
    Set-Default "$ClassesRoot\$ProgId\shell\edit\command" "`"$env:WINDIR\System32\notepad.exe`" `"%1`""
    if ($CodeExe) {
        Set-Default "$ClassesRoot\$ProgId\shell\vscode" 'Edit in VS Code'
        Set-Default "$ClassesRoot\$ProgId\shell\vscode\command" "`"$CodeExe`" `"%1`""
    }
    Clear-OldChoice
    Send-AssociationsChanged
}

# If someone once picked another program for .ooga in Windows' "Open with" box, Windows keeps using
# that program and ignores the lines above. Forget that old pick (only for .ooga files).
function Clear-OldChoice {
    $choice = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.ooga\UserChoice'
    if (Test-Path $choice) {
        try { Remove-Item $choice -Recurse -Force }
        catch { Write-Host "      (Windows kept an old 'Open with' choice for .ooga. if double-click opens another program, see the README.)" }
    }
}

function Unregister-OogaFiles {
    $ext = "$ClassesRoot\.ooga"
    if ((Test-Path $ext) -and ((Get-Item $ext).GetValue('') -eq $ProgId)) { Remove-Item $ext -Recurse -Force }
    if (Test-Path "$ClassesRoot\$ProgId") { Remove-Item "$ClassesRoot\$ProgId" -Recurse -Force }
    Send-AssociationsChanged
}

# VS Code's program and its command-line tool, if VS Code is installed (for this user, for everyone, or on PATH).
function Find-VSCode {
    foreach ($folder in @((Join-Path $env:LOCALAPPDATA 'Programs\Microsoft VS Code'), (Join-Path $env:ProgramFiles 'Microsoft VS Code'))) {
        if ($folder -and (Test-Path (Join-Path $folder 'Code.exe'))) {
            return @{ Exe = (Join-Path $folder 'Code.exe'); Cli = (Join-Path $folder 'bin\code.cmd') }
        }
    }
    $cli = Get-Command code -ErrorAction SilentlyContinue
    if ($cli) { return @{ Exe = $null; Cli = $cli.Source } }
    return $null
}
