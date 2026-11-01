<#
.SYNOPSIS
  Builds ThreadModeler (net10.0-windows) and packages an Autodesk ApplicationPlugins bundle.

.DESCRIPTION
  Output:
    dist\cODTthreadModeler.bundle\       -> copy to %ProgramData%\Autodesk\ApplicationPlugins\
    dist\cODTthreadModeler.bundle.zip

.PARAMETER Configuration
  Release (default) or Debug.

.PARAMETER Install
  Also copies the bundle to %ProgramData%\Autodesk\ApplicationPlugins (requires an elevated prompt).

.PARAMETER InventorInteropPath
  Optional path to Autodesk.Inventor.Interop.dll. By default the Inventor 2027/2026 installation
  is used, falling back to References\Autodesk.Inventor.Interop.dll.

.EXAMPLE
  .\build.ps1
  .\build.ps1 -Configuration Debug -Install
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$Install,
    [string]$InventorInteropPath
)

$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$bundleName = 'cODTthreadModeler.bundle'
$out        = Join-Path $root "bin\$Configuration\Contents"
$dist       = Join-Path $root 'dist'
$bundle     = Join-Path $dist $bundleName
$zip        = Join-Path $dist "$bundleName.zip"

if (Test-Path $out) { Remove-Item $out -Recurse -Force }

$buildArgs = @('build', (Join-Path $root 'ThreadModeler\ThreadModeler.csproj'), '-c', $Configuration)
if ($InventorInteropPath) { $buildArgs += "-p:InventorInteropPath=$InventorInteropPath" }
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }

if (Test-Path $bundle) { Remove-Item $bundle -Recurse -Force }
if (Test-Path $zip)    { Remove-Item $zip -Force }
New-Item -ItemType Directory -Path (Join-Path $bundle 'Contents') -Force | Out-Null

Copy-Item (Join-Path $root 'ThreadModeler\PackageContents.xml') $bundle
Copy-Item (Join-Path $out '*') (Join-Path $bundle 'Contents') -Recurse
if ($Configuration -eq 'Release') {
    Get-ChildItem $bundle -Recurse -Filter *.pdb | Remove-Item -Force
}

Compress-Archive -Path $bundle -DestinationPath $zip

Write-Host ""
Write-Host "Bundle: $bundle"
Write-Host "Zip:    $zip"

if ($Install) {
    $target = Join-Path $env:ProgramData "Autodesk\ApplicationPlugins\$bundleName"
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    Copy-Item $bundle $target -Recurse
    Write-Host "Installed to $target - restart Inventor."
}
