<#
  install.ps1 -- set up the VR Pinball Launcher.

  Two things can be installed from here:

    1. VPinballX (VR)      -- install and/or VR-enable Visual Pinball X, then point
                              the launcher's launcher-config.json at it.
                              * Uses an existing VPinballX_GL64.exe if one is found.
                              * Otherwise offers to download the latest VR-capable
                                Windows build (VPinballX_GL, x64) from GitHub.
                              * Writes [PlayerVR] AskToTurnOn=0 into VPinballX.ini so
                                VPX boots straight into VR.

    2. Controller bridge   -- ensures AutoHotkey 1.1 is installed and wires the
                              Touch-controller -> keystroke bridge into the launcher.

  The installer (VRPinballLauncher-Setup.exe) runs this automatically for the
  setup steps you tick. You can also run it by hand at any time from the install
  folder to (re)configure things.

  How to run (no arguments shows a menu):
    Right-click this file -> "Run with PowerShell", or from a terminal:
      powershell -ExecutionPolicy Bypass -File install.ps1

  Non-interactive examples:
      powershell -ExecutionPolicy Bypass -File install.ps1 -Setup both
      powershell -ExecutionPolicy Bypass -File install.ps1 -Setup vpx -Yes -VpxDir "C:\Visual Pinball"

  This script is expected to sit next to vr-launch.exe, with the ControllerBridge
  folder beside it.
#>

param(
    # What to set up. 'menu' (default) prompts interactively. 'both' = everything.
    [ValidateSet('menu', 'vpx', 'vpinmame', 'bridge', 'both')]
    [string]$Setup = 'menu',

    # Where to install / find VPinballX (folder that holds VPinballX_GL64.exe).
    [string]$VpxDir = '',

    # Folder that holds your .vpx tables. Leave empty to keep the current config value.
    [string]$TablesDir = '',

    # Assume "yes" to prompts (e.g. downloading VPinballX). Used by the installer.
    [switch]$Yes
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$rootDir    = Split-Path -Parent $MyInvocation.MyCommand.Definition   # = folder next to vr-launch.exe
$bridgeDir  = Join-Path $rootDir "ControllerBridge"
$scriptPath = Join-Path $bridgeDir "vpx_vr_controls.ahk"

Write-Host "VR Pinball Launcher -- setup"
Write-Host "Launcher folder: $rootDir"
Write-Host ""

# ---------------------------------------------------------------------------
# Shared helpers
# ---------------------------------------------------------------------------

# Locate (or create) launcher-config.json -- it lives next to vr-launch.exe.
function Get-LauncherConfigPath {
    $candidates = @(
        (Join-Path $rootDir   "launcher-config.json"),
        (Join-Path $bridgeDir "launcher-config.json")
    )
    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $found) {
        $found = Join-Path $rootDir "launcher-config.json"
        Write-Host "launcher-config.json not found -- creating $found"
        '{}' | Set-Content -Path $found -Encoding ascii
    }
    return $found
}

# Apply a set of name/value pairs to launcher-config.json.
function Set-LauncherConfig {
    param([hashtable]$Values)
    $configPath = Get-LauncherConfigPath
    $json = Get-Content -Raw -Path $configPath | ConvertFrom-Json
    foreach ($key in $Values.Keys) {
        $json | Add-Member -NotePropertyName $key -NotePropertyValue $Values[$key] -Force
    }
    [System.IO.File]::WriteAllText($configPath, ($json | ConvertTo-Json -Depth 12))
    Write-Host "Updated $configPath"
}

# Set "Key = Value" inside [Section] of an INI file, creating the file,
# section, or key as needed and preserving everything else.
function Set-IniValue {
    param(
        [string]$Path,
        [string]$Section,
        [string]$Key,
        [string]$Value
    )

    $lines = @()
    if (Test-Path $Path) { $lines = @([System.IO.File]::ReadAllLines($Path)) }

    $out           = New-Object System.Collections.Generic.List[string]
    $sectionHeader = "[$Section]"
    $inSection     = $false
    $sectionFound  = $false
    $keyWritten    = $false

    foreach ($line in $lines) {
        $trim = $line.Trim()

        if ($trim -match '^\[.+\]$') {
            # Leaving a section: if it was the target and we never saw the key, add it now.
            if ($inSection -and -not $keyWritten) {
                $out.Add("$Key = $Value")
                $keyWritten = $true
            }
            $inSection = ($trim -ieq $sectionHeader)
            if ($inSection) { $sectionFound = $true }
            $out.Add($line)
            continue
        }

        if ($inSection -and ($trim -match "^$([regex]::Escape($Key))\s*=")) {
            if (-not $keyWritten) {
                $out.Add("$Key = $Value")
                $keyWritten = $true
            }
            continue   # drop the original key line (replaced)
        }

        $out.Add($line)
    }

    # File ended while still inside the target section without the key.
    if ($inSection -and -not $keyWritten) {
        $out.Add("$Key = $Value")
        $keyWritten = $true
    }

    # Section never appeared at all -- append it.
    if (-not $sectionFound) {
        if ($out.Count -gt 0 -and $out[$out.Count - 1].Trim() -ne '') { $out.Add('') }
        $out.Add($sectionHeader)
        $out.Add("$Key = $Value")
    }

    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllLines($Path, $out)
}

# ---------------------------------------------------------------------------
# 1. VPinballX (VR)
# ---------------------------------------------------------------------------

function Find-Vpx {
    param([string]$PreferDir)

    $candidates = @()
    if ($PreferDir) { $candidates += (Join-Path $PreferDir "VPinballX_GL64.exe") }

    # Whatever the current config already points at.
    $configPath = Join-Path $rootDir "launcher-config.json"
    if (Test-Path $configPath) {
        try {
            $existing = (Get-Content -Raw -Path $configPath | ConvertFrom-Json).vpinballExecutable
            if ($existing) { $candidates += $existing }
        } catch { }
    }

    $candidates += @(
        "C:\Visual Pinball\VPinballX_GL64.exe",
        "C:\Games\Visual Pinball\VPinballX_GL64.exe",
        "C:\Program Files\Visual Pinball\VPinballX_GL64.exe",
        "C:\Program Files (x86)\Visual Pinball\VPinballX_GL64.exe"
    )

    return $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}

# Download the newest VR-capable Windows build and extract it into $DestDir.
# Returns the full path to VPinballX_GL64.exe.
function Install-VpxFromGitHub {
    param([string]$DestDir)

    Write-Host "Querying GitHub for the latest VPinballX VR build..."
    $headers  = @{ "User-Agent" = "vr-pinball-launcher-setup" }
    $releases = Invoke-RestMethod -Uri "https://api.github.com/repos/vpinball/vpinball/releases" `
                                  -Headers $headers -UseBasicParsing
    # Most recent first; the VR (10.8.x) builds are published as pre-releases.
    $release = $releases | Select-Object -First 1
    $asset   = $release.assets |
        Where-Object { $_.name -match '^VPinballX_GL-.*-windows-x64-Release\.zip$' } |
        Select-Object -First 1
    if (-not $asset) {
        throw "No Windows x64 GL (VR) asset found in release $($release.tag_name). Download VPinballX_GL manually from https://github.com/vpinball/vpinball/releases and re-run with -VpxDir."
    }

    Write-Host "Release: $($release.tag_name)"
    Write-Host "Asset:   $($asset.name)"

    $zipPath = Join-Path $env:TEMP $asset.name
    Write-Host "Downloading (~$([math]::Round($asset.size / 1MB)) MB)..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $headers -UseBasicParsing

    if (-not (Test-Path $DestDir)) { New-Item -ItemType Directory -Path $DestDir -Force | Out-Null }
    Write-Host "Extracting to $DestDir ..."
    Expand-Archive -Path $zipPath -DestinationPath $DestDir -Force
    Remove-Item $zipPath -ErrorAction SilentlyContinue

    $exe = Get-ChildItem -Path $DestDir -Filter "VPinballX_GL64.exe" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $exe) {
        throw "VPinballX_GL64.exe not found under $DestDir after extracting $($asset.name)."
    }
    return $exe.FullName
}

# Returns VPinMAME's ROM folder (where the .zip ROMs go). Reads the path VPinMAME
# actually uses from its registry "globals", falling back to the usual location
# next to VPinballX.
function Get-RomPath {
    param([string]$VpxExe)

    foreach ($k in @(
        'HKCU:\Software\Freeware\Visual PinMame\globals',
        'HKLM:\Software\WOW6432Node\Freeware\Visual PinMame\globals',
        'HKLM:\Software\Freeware\Visual PinMame\globals'
    )) {
        if (Test-Path $k) {
            $rp = (Get-ItemProperty -Path $k -ErrorAction SilentlyContinue).rompath
            if ($rp) { return $rp }
        }
    }
    if ($VpxExe) {
        return (Join-Path (Split-Path -Parent $VpxExe) "VPinMAME\roms")
    }
    return $null
}

# Creates the target folder (if missing) and a Desktop shortcut to it, so the user
# can drag content straight in.
function New-DesktopFolderShortcut {
    param([string]$Name, [string]$TargetDir)

    if ([string]::IsNullOrWhiteSpace($TargetDir)) {
        Write-Host "Skipping '$Name' desktop shortcut -- folder is not known." -ForegroundColor Yellow
        return
    }

    if (-not (Test-Path $TargetDir)) {
        New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
        Write-Host "Created folder: $TargetDir"
    }

    $lnkPath = Join-Path ([Environment]::GetFolderPath('Desktop')) "$Name.lnk"
    $wsh = New-Object -ComObject WScript.Shell
    $lnk = $wsh.CreateShortcut($lnkPath)
    $lnk.TargetPath       = $TargetDir
    $lnk.WorkingDirectory = $TargetDir
    $lnk.Description      = $Name
    $lnk.Save()
    Write-Host "Desktop shortcut: $Name -> $TargetDir"
}

function Install-VpxVR {
    Write-Host "=== VPinballX (VR) ===" -ForegroundColor Cyan

    $vpxExe = Find-Vpx -PreferDir $VpxDir

    if ($vpxExe) {
        Write-Host "Found existing VPinballX: $vpxExe"
    }
    else {
        Write-Host "No VPinballX_GL64.exe found."
        $dest = $VpxDir
        if (-not $dest) { $dest = "C:\Visual Pinball" }

        if (-not $Yes) {
            $answer = Read-Host "Download the latest VR build and install it to '$dest'? [Y/n]"
            if ($answer -and $answer.Trim().ToLower().StartsWith('n')) {
                Write-Host "Skipping download. Re-run with -VpxDir pointing at an existing install to configure it." -ForegroundColor Yellow
                return
            }
        }
        else {
            Write-Host "Downloading the latest VR build to '$dest'..."
        }
        $vpxExe = Install-VpxFromGitHub -DestDir $dest
        Write-Host "Installed VPinballX: $vpxExe"
    }

    # --- Enable VR in VPinballX.ini ---
    # VPX reads its config from %AppData%\VPinballX\VPinballX.ini, unless a
    # VPinballX.ini sits next to the executable (portable mode). Prefer the
    # portable one if it already exists, otherwise use the AppData copy.
    $portableIni = Join-Path (Split-Path -Parent $vpxExe) "VPinballX.ini"
    $appDataIni  = Join-Path $env:APPDATA "VPinballX\VPinballX.ini"
    $iniPath = if (Test-Path $portableIni) { $portableIni } else { $appDataIni }

    Set-IniValue -Path $iniPath -Section "PlayerVR" -Key "AskToTurnOn" -Value "0"
    Write-Host "Enabled VR ([PlayerVR] AskToTurnOn=0) in: $iniPath"

    # --- Wire the launcher config ---
    $values = @{ vpinballExecutable = $vpxExe }

    $tables = $TablesDir
    if (-not $tables) {
        $guess = Join-Path (Split-Path -Parent $vpxExe) "Tables"
        if (Test-Path $guess) { $tables = $guess }
    }
    if ($tables) { $values["tablesDirectory"] = $tables }

    Set-LauncherConfig -Values $values

    # --- Desktop shortcuts to the Tables and ROMs folders (easy drag-and-drop) ---
    # Point at the tables folder the launcher actually scans, and VPinMAME's ROM path.
    $finalTables = (Get-Content -Raw -Path (Get-LauncherConfigPath) | ConvertFrom-Json).tablesDirectory
    $romPath = Get-RomPath -VpxExe $vpxExe
    New-DesktopFolderShortcut -Name "VR Pinball Tables" -TargetDir $finalTables
    New-DesktopFolderShortcut -Name "VR Pinball ROMs"   -TargetDir $romPath

    Write-Host ""
    Write-Host "VPinballX VR is ready." -ForegroundColor Green
    Write-Host "  Executable: $vpxExe"
    if ($finalTables) { Write-Host "  Tables:     $finalTables" }
    if ($romPath)     { Write-Host "  ROMs:       $romPath" }
    Write-Host "  Desktop shortcuts added for the Tables and ROMs folders -- drag your tables/ROMs in."
    Write-Host "  VR needs a SteamVR-compatible runtime (Quest Link / Air Link + SteamVR) running before launch."
    Write-Host ""
}

# ---------------------------------------------------------------------------
# 2. VPinMAME (ROM emulator most tables need)
# ---------------------------------------------------------------------------

# Returns the DLL path the 64-bit VPinMAME.Controller COM object resolves to,
# or $null if VPinMAME isn't registered.
function Get-VPinMAMERegisteredDll {
    $clsid = (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\VPinMAME.Controller\CLSID' -ErrorAction SilentlyContinue).'(default)'
    if (-not $clsid) { return $null }
    $ip = "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32"
    if (Test-Path $ip) { return (Get-ItemProperty $ip -ErrorAction SilentlyContinue).'(default)' }
    return $null
}

# Registers VPinMAME64.dll as a COM server. This writes to HKLM, so it needs
# admin: if we're not already elevated, elevate just the regsvr32 call (one UAC
# prompt). Skips the work if it's already registered to this exact DLL.
function Register-VPinMAME {
    param([string]$Dll)

    $current = Get-VPinMAMERegisteredDll
    if ($current -and ($current -ieq $Dll) -and (Test-Path $current)) {
        Write-Host "VPinMAME already registered: $Dll"
        return $true
    }

    # Direct attempt -- succeeds when this process is already elevated
    # (e.g. an all-users install).
    $p = Start-Process regsvr32.exe -ArgumentList '/s', "`"$Dll`"" -PassThru -Wait -ErrorAction SilentlyContinue
    if ($p -and $p.ExitCode -eq 0) {
        Write-Host "Registered VPinMAME." -ForegroundColor Green
        return $true
    }

    # Not elevated -- ask Windows to elevate the registration only.
    Write-Host "Registering VPinMAME needs administrator rights -- approve the Windows prompt..."
    try {
        $p = Start-Process regsvr32.exe -ArgumentList '/s', "`"$Dll`"" -Verb RunAs -PassThru -Wait
        if ($p.ExitCode -eq 0) {
            Write-Host "Registered VPinMAME." -ForegroundColor Green
            return $true
        }
        Write-Host "regsvr32 returned exit code $($p.ExitCode)." -ForegroundColor Yellow
    }
    catch {
        Write-Host "VPinMAME registration was declined or failed." -ForegroundColor Yellow
    }
    Write-Host "To finish it manually: run Setup64.exe in '$(Split-Path -Parent $Dll)' as administrator and click Install." -ForegroundColor Yellow
    return $false
}

# Downloads the latest VPinMAME (x64) and extracts it into $DestDir.
function Install-VPinMAMEFromGitHub {
    param([string]$DestDir)

    Write-Host "Querying GitHub for the latest VPinMAME (x64)..."
    $headers  = @{ "User-Agent" = "vr-pinball-launcher-setup" }
    $releases = Invoke-RestMethod -Uri "https://api.github.com/repos/vpinball/pinmame/releases" `
                                  -Headers $headers -UseBasicParsing
    $release = $releases | Select-Object -First 1
    # Plain VPinMAME (version starts with a digit -- skips the "-sc" variant).
    $asset = $release.assets |
        Where-Object { $_.name -match '^VPinMAME-[0-9].*-win-x64\.zip$' } |
        Select-Object -First 1
    if (-not $asset) {
        throw "No VPinMAME x64 asset found in release $($release.tag_name). Download it from https://github.com/vpinball/pinmame/releases and unzip it into '$DestDir'."
    }

    Write-Host "Release: $($release.tag_name)"
    Write-Host "Asset:   $($asset.name)"

    $zip = Join-Path $env:TEMP $asset.name
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -Headers $headers -UseBasicParsing
    if (-not (Test-Path $DestDir)) { New-Item -ItemType Directory -Path $DestDir -Force | Out-Null }
    # -Force overwrites the program files but leaves any existing roms/nvram/cfg.
    Expand-Archive -Path $zip -DestinationPath $DestDir -Force
    Remove-Item $zip -ErrorAction SilentlyContinue
    Write-Host "Installed VPinMAME -> $DestDir"
}

function Install-VPinMAME {
    Write-Host "=== VPinMAME (ROM emulator) ===" -ForegroundColor Cyan

    # VPinMAME lives next to VPinballX so VPX can find its ROMs.
    $vpxExe = (Get-Content -Raw -Path (Get-LauncherConfigPath) | ConvertFrom-Json).vpinballExecutable
    if (-not $vpxExe -or -not (Test-Path $vpxExe)) { $vpxExe = Find-Vpx -PreferDir $VpxDir }
    if (-not $vpxExe) {
        Write-Host "VPinballX not found -- run the VPinballX (VR) step first, then VPinMAME." -ForegroundColor Yellow
        return
    }

    $vpmDir = Join-Path (Split-Path -Parent $vpxExe) "VPinMAME"
    $dll    = Join-Path $vpmDir "VPinMAME64.dll"

    if (Test-Path $dll) {
        Write-Host "VPinMAME present: $dll"
    }
    else {
        Install-VPinMAMEFromGitHub -DestDir $vpmDir
    }

    # Content folders + the paths VPinMAME reads from HKCU.
    $roms    = Join-Path $vpmDir "roms"
    $samples = Join-Path $vpmDir "samples"
    foreach ($d in @($roms, $samples, (Join-Path $vpmDir "nvram"), (Join-Path $vpmDir "cfg"))) {
        if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
    }
    $globals = 'HKCU:\Software\Freeware\Visual PinMame\globals'
    if (-not (Test-Path $globals)) { New-Item -Path $globals -Force | Out-Null }
    Set-ItemProperty -Path $globals -Name rompath    -Value $roms
    Set-ItemProperty -Path $globals -Name samplepath -Value $samples

    Register-VPinMAME -Dll $dll | Out-Null

    # Make the ROMs folder easy to find even if the VPX VR step was skipped.
    New-DesktopFolderShortcut -Name "VR Pinball ROMs" -TargetDir $roms

    Write-Host ""
    Write-Host "VPinMAME ready." -ForegroundColor Green
    Write-Host "  DLL:  $dll"
    Write-Host "  ROMs: $roms  (drop ROM .zip files here)"
    Write-Host ""
}

# ---------------------------------------------------------------------------
# 3. Controller bridge
# ---------------------------------------------------------------------------

function Install-ControllerBridge {
    Write-Host "=== Controller bridge ===" -ForegroundColor Cyan

    if (-not (Test-Path $scriptPath)) {
        throw "ControllerBridge\vpx_vr_controls.ahk not found ($scriptPath) -- make sure the ControllerBridge folder sits next to install.ps1."
    }

    # --- Ensure AutoHotkey 1.1 (AutoHotkeyU64.exe) ---
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

    Set-LauncherConfig -Values @{
        enableControllerBridge     = $true
        controllerBridgePath       = $ahk
        controllerBridgeArgs       = "`"$scriptPath`""
        controllerBridgeWorkingDir = $bridgeDir
    }

    Write-Host ""
    Write-Host "Controller bridge ready -- the launcher will start/stop it automatically." -ForegroundColor Green
    Write-Host "In-game mapping (VPinballX must be the focused window):"
    Write-Host "  Left/Right trigger = flippers   X = start   B = coin   Y = exit   right stick (back) = plunger"
    Write-Host "  Make sure Quest Link / Air Link is running before you launch a table."
    Write-Host ""
}

# ---------------------------------------------------------------------------
# Menu / dispatch
# ---------------------------------------------------------------------------

if ($Setup -eq 'menu') {
    Write-Host "What would you like to set up?"
    Write-Host "  [1] VPinballX (VR)      -- install / VR-enable Visual Pinball and point the launcher at it"
    Write-Host "  [2] VPinMAME           -- ROM emulator most tables need"
    Write-Host "  [3] Controller bridge   -- Touch-controller in-game controls (AutoHotkey)"
    Write-Host "  [4] Everything          (recommended)"
    Write-Host "  [Q] Quit"
    Write-Host ""
    $choice = Read-Host "Enter choice"
    switch ($choice.Trim().ToLower()) {
        '1'  { $Setup = 'vpx' }
        '2'  { $Setup = 'vpinmame' }
        '3'  { $Setup = 'bridge' }
        '4'  { $Setup = 'both' }
        'q'  { Write-Host "Cancelled."; return }
        ''   { Write-Host "Cancelled."; return }
        default { Write-Host "Unrecognized choice '$choice'. Cancelled." -ForegroundColor Yellow; return }
    }
    Write-Host ""
}

if ($Setup -eq 'vpx'      -or $Setup -eq 'both') { Install-VpxVR }
if ($Setup -eq 'vpinmame' -or $Setup -eq 'both') { Install-VPinMAME }
if ($Setup -eq 'bridge'   -or $Setup -eq 'both') { Install-ControllerBridge }

Write-Host "Done." -ForegroundColor Green
