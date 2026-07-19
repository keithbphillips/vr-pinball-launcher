# Controller Bridge

In-game VR controller support for the VR Pinball Launcher.

## Why this exists

Visual Pinball's VR build reads **keyboard / DirectInput**, not VR motion
controllers, and Unity hands its XR session over to VPinballX while a table is
running. So the controls can't live inside the launcher — they're provided by a
small **external bridge** that reads the Touch controllers and sends the
keystrokes VPinballX expects. The launcher starts this bridge on startup and
kills it on exit (see `enableControllerBridge` in `launcher-config.json`).

## Contents

| File | Purpose |
|------|---------|
| `vpx_vr_controls.ahk` | The control mapping (this project) |
| `auto_oculus_touch.ahk` / `auto_oculus_touch.dll` | Reads the Quest/Touch controllers via the Oculus runtime — by Kojack (rajetic), MIT (see `auto_oculus_touch_README.txt`) |
| `vJoyInterface.dll` | **Required.** `auto_oculus_touch.dll` is built with vJoy support, so it has a load-time dependency on this file even though we don't use vJoy. It must sit beside `auto_oculus_touch.dll` or the DLL won't load ("auto_oculus_touch.dll file is missing"). The vJoy *driver* itself is **not** required. |

Setup for both VPinballX (VR) **and** this controller bridge is handled by
**`install.ps1`**, which lives in the launcher root folder (next to `vr-launch.exe`),
one level up from here.

## Install

1. Make sure this `ControllerBridge` folder sits **next to `vr-launch.exe`** (and
   therefore next to `install.ps1`).
2. Right-click **`install.ps1`** (in the launcher root folder) → **Run with PowerShell**
   (or `powershell -ExecutionPolicy Bypass -File install.ps1`).

`install.ps1` opens a menu:

| Option | What it does |
|---|---|
| **1. VPinballX (VR)** | Uses an existing `VPinballX_GL64.exe` if found; otherwise offers to download the latest VR-capable Windows build (`VPinballX_GL`, x64) from the [vpinball releases](https://github.com/vpinball/vpinball/releases). It then enables VR (`[PlayerVR] AskToTurnOn=0` in `VPinballX.ini`) and points `launcher-config.json` at the exe and your tables folder. |
| **2. Controller bridge** | Ensures **AutoHotkey 1.1** is present (downloading the official installer if needed) and writes the bridge settings into `launcher-config.json` so the launcher manages it automatically. |
| **3. Both** | Runs both of the above (recommended). |

No vJoy, no kernel driver, and no Windows security changes required.

### Non-interactive use

```powershell
# everything, no prompts where avoidable
powershell -ExecutionPolicy Bypass -File install.ps1 -Setup both

# only VPinballX VR, into an existing/target folder, with a specific tables dir
powershell -ExecutionPolicy Bypass -File install.ps1 -Setup vpx -VpxDir "C:\Visual Pinball" -TablesDir "C:\Visual Pinball\Tablesvr"

# only the controller bridge
powershell -ExecutionPolicy Bypass -File install.ps1 -Setup bridge
```

VR requires a **SteamVR-compatible runtime** running before you launch a table
(Quest **Link / Air Link** + SteamVR). The first time VPinballX starts in VR it
may still prompt 2D-vs-VR until a headset is detected.

## Controls (in-game)

The bridge only sends keys while the **VPinballX window is focused**, so the
triggers double as carousel navigation in the menu and flippers in-game.

| Control | Action | Key (VPX default) |
|---|---|---|
| Left trigger | Left flipper | Left Shift |
| Right trigger | Right flipper | Right Shift |
| X (left) | Start game | `1` |
| B (right) | Insert coin | `5` |
| Y (left) | Exit table | `Esc` |
| Right thumbstick (pull back) | Plunger | Enter |

Mappings assume VPinballX's default key bindings. If yours differ, check
**VPX → Preferences → Configure Keys** and edit the `Send` lines in
`vpx_vr_controls.ahk` (then restart the launcher so the bridge reloads).

## Requirements

- Oculus/Meta runtime running (Quest **Link / Air Link**) — the reader uses the
  Oculus SDK, so pure Virtual-Desktop / OpenXR-only streaming is not supported by
  this reader.
- AutoHotkey 1.1 (the installer handles this).

## Manual configuration

If you'd rather not run the installer, set these in `launcher-config.json`
(next to `vr-launch.exe`), adjusting paths to where you put this folder:

```json
"enableControllerBridge": true,
"controllerBridgePath": "C:\\Program Files\\AutoHotkey\\AutoHotkeyU64.exe",
"controllerBridgeArgs": "\"C:\\...\\ControllerBridge\\vpx_vr_controls.ahk\"",
"controllerBridgeWorkingDir": "C:\\...\\ControllerBridge"
```

`controllerBridgeWorkingDir` must be this folder so `auto_oculus_touch.dll` loads.
