param(
    [ValidateSet('win-arm64', 'win-x64')][string]$Runtime = 'win-arm64',
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    if (-not $SkipTests) {
        & dotnet run --project tests/ClaudeAccounts.Tests -c Release --artifacts-path (Join-Path $repo 'artifacts/build')
        if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
    }
    $destination = Join-Path $repo "artifacts/$Runtime"
    & dotnet publish src/ClaudeAccounts.App -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false --artifacts-path (Join-Path $repo 'artifacts/build') -o $destination
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item (Join-Path $repo 'LICENSE') $destination
    Copy-Item (Join-Path $repo 'README.md') $destination
    Copy-Item (Join-Path $repo 'THIRD-PARTY.md') $destination
    $assets = Get-Content (Join-Path $repo 'artifacts/build/obj/ClaudeAccounts.App/project.assets.json') -Raw | ConvertFrom-Json
    $licenseFolder = Join-Path $destination 'licenses'
    New-Item -ItemType Directory -Path $licenseFolder -Force | Out-Null
    foreach ($pack in @("microsoft.netcore.app.runtime.$Runtime", "microsoft.windowsdesktop.app.runtime.$Runtime")) {
        $copied = $false
        foreach ($cache in $assets.packageFolders.PSObject.Properties.Name) {
            $packRoot = Join-Path $cache $pack
            if (-not (Test-Path -LiteralPath $packRoot)) { continue }
            $version = Get-ChildItem -LiteralPath $packRoot -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
            $license = Get-ChildItem -LiteralPath $version.FullName -File | Where-Object { $_.Name -match '^LICENSE(\.TXT)?$' } | Select-Object -First 1
            if ($license) { Copy-Item -LiteralPath $license.FullName -Destination (Join-Path $licenseFolder "$pack-LICENSE.txt"); $copied = $true; break }
        }
        if (-not $copied) { throw "Missing redistribution license for $pack." }
    }
    Copy-Item (Join-Path $PSScriptRoot 'Install.ps1') $destination
    Copy-Item (Join-Path $PSScriptRoot 'Uninstall.ps1') $destination
    Copy-Item (Join-Path $PSScriptRoot 'Install.cmd') $destination
    if (Test-Path (Join-Path $repo 'docs/screenshot.png')) {
        New-Item -ItemType Directory -Path (Join-Path $destination 'docs') -Force | Out-Null
        Copy-Item (Join-Path $repo 'docs/screenshot.png') (Join-Path $destination 'docs')
    }
    $zip = Join-Path $repo "artifacts/ClaudeAccounts-0.1.0-$Runtime.zip"
    Compress-Archive -Path "$destination/*" -DestinationPath $zip -Force
    Get-FileHash $zip -Algorithm SHA256 | Format-List
    Write-Output "Built $zip"
} finally { Pop-Location }
