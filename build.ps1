<#
.SYNOPSIS
  Builds ThreadModeler (net10.0-windows), runs the tests and produces the installer.

.DESCRIPTION
  Output in dist\:
    ThreadModeler-<version>.msi            per-machine installer (the normal way to install)
    ThreadModeler-<version>.msi.sha256     checksum (used by the add-in's updater)
    ThreadModeler.bundle\              the raw ApplicationPlugins bundle (manual install alternative)
    ThreadModeler.bundle.zip

.PARAMETER Configuration
  Release (default) or Debug.

.PARAMETER Version
  Product version (e.g. 30.0.2). Default: the version in Directory.Build.props.

.PARAMETER SkipTests
  Do not run the unit tests.

.PARAMETER SkipInstaller
  Do not build the MSI.

.PARAMETER Install
  Also copies the bundle to %ProgramData%\Autodesk\ApplicationPlugins (requires an elevated prompt).
  For normal installs use the MSI instead.

.PARAMETER InventorInteropPath
  Optional path to Autodesk.Inventor.Interop.dll to compile against (see docs\BUILDING.md).

.EXAMPLE
  .\build.ps1
  .\build.ps1 -Version 30.0.2
  .\build.ps1 -Configuration Debug -SkipInstaller -Install
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [string]$Version,
    [switch]$SkipTests,
    [switch]$SkipInstaller,
    [switch]$Install,
    [string]$InventorInteropPath
)

$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$bundleName = 'ThreadModeler.bundle'
$binDir     = Join-Path $root "bin\$Configuration"
$dist       = Join-Path $root 'dist'
$bundle     = Join-Path $dist $bundleName
$zip        = Join-Path $dist "$bundleName.zip"

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args[0]) failed ($LASTEXITCODE)" }
}

$props = @()
if ($Version) { $props += "-p:Version=$Version" }
if ($InventorInteropPath) { $props += "-p:InventorInteropPath=$InventorInteropPath" }

# 1. Add-in -> bin\<Configuration>\ (PackageContents.xml + Contents\)
if (Test-Path $binDir) { Remove-Item $binDir -Recurse -Force }
Invoke-Dotnet build (Join-Path $root 'ThreadModeler\ThreadModeler.csproj') -c $Configuration @props

# 2. Tests
if (-not $SkipTests) {
    Invoke-Dotnet test (Join-Path $root 'ThreadModeler.Tests\ThreadModeler.Tests.csproj') -c $Configuration @props
}

# 3. Bundle folder + zip
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist -Force | Out-Null
Copy-Item $binDir $bundle -Recurse
if ($Configuration -eq 'Release') {
    Get-ChildItem $bundle -Recurse -Filter *.pdb | Remove-Item -Force
}
Compress-Archive -Path $bundle -DestinationPath $zip

# 4. MSI + checksum
if (-not $SkipInstaller) {
    Invoke-Dotnet build (Join-Path $root 'Installer\ThreadModeler.Installer.wixproj') -c $Configuration @props
    Get-ChildItem $dist -Filter *.wixpdb | Remove-Item -Force
    foreach ($msi in Get-ChildItem $dist -Filter *.msi) {
        $hash = (Get-FileHash $msi.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        Set-Content -Path "$($msi.FullName).sha256" -Value "$hash  $($msi.Name)" -Encoding ascii -NoNewline
    }
}

Write-Host ""
Get-ChildItem $dist | ForEach-Object { Write-Host "  dist\$($_.Name)" }

if ($Install) {
    $target = Join-Path $env:ProgramData "Autodesk\ApplicationPlugins\$bundleName"
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    Copy-Item $bundle $target -Recurse
    Write-Host "Installed to $target - restart Inventor."
}
