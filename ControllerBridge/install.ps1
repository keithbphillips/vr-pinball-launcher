<#
  install.ps1 -- set up the VR controller bridge for the VR Pinball Launcher.

  What it does:
    1. Ensures AutoHotkey 1.1 is installed (downloads + runs the installer if missing).
    2. Points the launcher's launcher-config.json at this bridge folder so that
       vr-launch.exe starts the bridge on launch and stops it on exit.

  How to run:
    Right-click this file -> "Run with PowerShell", or from a terminal:
      powershell -ExecutionPolicy Bypass -File install.ps1

  This folder (ControllerBridge) is expected to sit next to vr-launch.exe.
#>

$ErrorActionPreference = "Stop"

$bridgeDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$scriptPath = Join-Path $bridgeDir "vpx_vr_controls.ahk"

Write-Host "VR Pinball Launcher -- controller bridge setup"
Write-Host "Bridge folder: $bridgeDir"

if (-not (Test-Path $scriptPath)) {
    throw "vpx_vr_controls.ahk not found in $bridgeDir -- run this from inside the ControllerBridge folder."
}

# --- 1. Ensure AutoHotkey 1.1 (AutoHotkeyU64.exe) ---
$ahkCandidates = @(
    "C:\Program Files\AutoHotkey\AutoHotkeyU64.exe",
    "C:\Program Files\AutoHotkey\v1.1.37.02\AutoHotkeyU64.exe",
    "C:\Program Files (x86)\AutoHotkey\AutoHotkeyU64.exe"
)
$ahk = $ahkCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $ahk) {
    Write-Host "AutoHotkey 1.1 not found -- downloading the official installer..."
    $installer = Join-Path $env:TEMP "ahk-install-v1.1.exe"
    Invoke-WebRequest -Uri "https://www.autohotkey.com/download/ahk-install.exe" `
                      -OutFile $installer -UseBasicParsing
    Write-Host "Launching the AutoHotkey installer -- accept the prompts to finish."
    Start-Process -FilePath $installer -Wait
    $ahk = $ahkCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $ahk) {
        throw "AutoHotkey 1.1 still not found. Install it from https://www.autohotkey.com (v1.1) and re-run this script."
    }
}
Write-Host "AutoHotkey: $ahk"

# --- 2. Locate launcher-config.json (next to vr-launch.exe = parent of this folder) ---
$parent = Split-Path -Parent $bridgeDir
$configCandidates = @(
    (Join-Path $parent    "launcher-config.json"),
    (Join-Path $bridgeDir "launcher-config.json")
)
$configPath = $configCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $configPath) {
    $configPath = Join-Path $parent "launcher-config.json"
    Write-Host "launcher-config.json not found -- creating $configPath"
    '{}' | Set-Content -Path $configPath -Encoding ascii
}
Write-Host "Config: $configPath"

# --- 3. Write the bridge settings into the config ---
$json = Get-Content -Raw -Path $configPath | ConvertFrom-Json
$json | Add-Member -NotePropertyName enableControllerBridge     -NotePropertyValue $true              -Force
$json | Add-Member -NotePropertyName controllerBridgePath       -NotePropertyValue $ahk               -Force
$json | Add-Member -NotePropertyName controllerBridgeArgs       -NotePropertyValue "`"$scriptPath`""  -Force
$json | Add-Member -NotePropertyName controllerBridgeWorkingDir -NotePropertyValue $bridgeDir         -Force

$out = $json | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($configPath, $out)

Write-Host ""
Write-Host "Done -- the launcher will now start/stop the controller bridge automatically." -ForegroundColor Green
Write-Host ""
Write-Host "In-game mapping (VPinballX must be the focused window):"
Write-Host "  Left/Right trigger = flippers   X = start   B = coin   Y = exit   right stick (back) = plunger"
Write-Host ""
Write-Host "Make sure Quest Link / Air Link is running before you launch a table."
