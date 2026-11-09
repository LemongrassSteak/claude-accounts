$ErrorActionPreference = 'Stop'
# Per-user installation; administrator privileges are not needed.
$sourceExe = Join-Path $PSScriptRoot 'ClaudeAccounts.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw 'Extract the release ZIP before installing.' }
$destination = Join-Path $env:LOCALAPPDATA 'Programs\ClaudeAccounts'
$installedExe = Join-Path $destination 'ClaudeAccounts.exe'
$running = Get-Process -Name ClaudeAccounts -ErrorAction SilentlyContinue
if ($running) { throw 'Quit Claude Accounts from its tray menu before installing or updating.' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
if ((Resolve-Path $PSScriptRoot).Path -ne $destination) {
    foreach ($name in @('ClaudeAccounts.exe', 'LICENSE', 'README.md', 'THIRD-PARTY.md', 'Uninstall.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $destination -Force
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'licenses') -Destination $destination -Recurse -Force
}
$shell = New-Object -ComObject WScript.Shell
$startMenu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Claude Accounts'
New-Item -ItemType Directory -Path $startMenu -Force | Out-Null
foreach ($link in @((Join-Path $startMenu 'Claude Accounts.lnk'), (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Claude Accounts.lnk'))) {
    $shortcut = $shell.CreateShortcut($link)
    $shortcut.TargetPath = $installedExe
    $shortcut.WorkingDirectory = $destination
    $shortcut.Description = 'Independent account profile launcher for Claude Desktop'
    $shortcut.IconLocation = "$installedExe,0"
    $shortcut.Save()
}
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeAccounts'
New-Item -Path $uninstallKey -Force | Out-Null
$properties = @{
    DisplayName = 'Claude Accounts'
    DisplayVersion = '0.1.0'
    Publisher = 'Claude Accounts contributors'
    InstallLocation = $destination
    DisplayIcon = $installedExe
    UninstallString = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$destination\Uninstall.ps1`""
}
foreach ($entry in $properties.GetEnumerator()) { New-ItemProperty -Path $uninstallKey -Name $entry.Key -Value $entry.Value -PropertyType String -Force | Out-Null }
Write-Output "Installed to $destination. Your account profiles were preserved."
Start-Process -FilePath $installedExe
