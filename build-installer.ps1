<#
  build-installer.ps1 -- compile the distributable installer (setup.exe).

  Runs the Inno Setup compiler on installer\vr-pinball-launcher.iss, bundling the
  Unity build output plus install.ps1 and the ControllerBridge folder into
  dist\VRPinballLauncher-Setup.exe.

  Run this after building the Unity project (File > Build -> Build\):
      powershell -ExecutionPolicy Bypass -File build-installer.ps1
      powershell -ExecutionPolicy Bypass -File build-installer.ps1 -Version 1.2.0
#>

param(
    [string]$BuildDir = (Join-Path $PSScriptRoot "Build"),
    [string]$Version  = "1.0.0",
    [string]$Iss      = (Join-Path $PSScriptRoot "installer\vr-pinball-launcher.iss")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path (Join-Path $BuildDir "vr-launch.exe"))) {
    throw "vr-launch.exe not found in '$BuildDir' -- build the Unity project first (File > Build)."
}

# Locate the Inno Setup command-line compiler.
$iscc = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6 from https://jrsoftware.org/isinfo.php and re-run."
}

Write-Host "Compiler: $iscc"
Write-Host "Build:    $BuildDir"
Write-Host "Version:  $Version"

& $iscc "/DAppVersion=$Version" "/DBuildDir=$BuildDir" $Iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }

$out = Join-Path $PSScriptRoot "dist\VRPinballLauncher-Setup.exe"
Write-Host ""
Write-Host "Created $out" -ForegroundColor Green
