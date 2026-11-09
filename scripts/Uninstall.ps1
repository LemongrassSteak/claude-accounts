$ErrorActionPreference = 'Stop'
# Uninstallation preserves %LOCALAPPDATA%\ClaudeAccounts (signed-in profiles).
$expected = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs\ClaudeAccounts'))
if ([IO.Path]::GetFullPath($PSScriptRoot) -ne $expected) { throw 'Run the installed Uninstall.ps1 from the app installation folder.' }
if (Get-Process -Name ClaudeAccounts -ErrorAction SilentlyContinue) { throw 'Quit Claude Accounts from its tray menu, then retry uninstalling.' }
if (Test-Path -LiteralPath (Join-Path $env:LOCALAPPDATA 'ClaudeAccounts\sign-in-route.json')) {
    $restore = Start-Process -FilePath (Join-Path $expected 'ClaudeAccounts.exe') -ArgumentList '--restore-sign-in-routing' -WindowStyle Hidden -Wait -PassThru
    if ($restore.ExitCode -ne 0) { throw 'Could not restore the previous Claude link handler. Uninstall stopped.' }
}
foreach ($link in @((Join-Path ([Environment]::GetFolderPath('Programs')) 'Claude Accounts\Claude Accounts.lnk'), (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Claude Accounts.lnk'))) {
    if (Test-Path -LiteralPath $link) {
        $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($link)
        if ($shortcut.TargetPath -eq (Join-Path $expected 'ClaudeAccounts.exe')) { Remove-Item -LiteralPath $link }
    }
}
foreach ($name in @('ClaudeAccounts.exe', 'LICENSE', 'README.md', 'THIRD-PARTY.md', 'licenses\microsoft.netcore.app.runtime.win-arm64-LICENSE.txt', 'licenses\microsoft.windowsdesktop.app.runtime.win-arm64-LICENSE.txt', 'licenses\microsoft.netcore.app.runtime.win-x64-LICENSE.txt', 'licenses\microsoft.windowsdesktop.app.runtime.win-x64-LICENSE.txt')) {
    $file = Join-Path $expected $name
    if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
}
$key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeAccounts'
if (Test-Path $key) { Remove-Item -LiteralPath $key }
Write-Output 'Uninstalled app. Your saved account profiles remain in %LOCALAPPDATA%\ClaudeAccounts.'
# Leave this uninstall script in place; no recursive directory deletion.
