# VR Pinball Launcher

A Steam VR launcher for Visual Pinball tables that provides an immersive VR menu interface for browsing and launching your pinball table collection.

## Features

- **VR Menu Interface**: Browse your table collection in VR
- **Automatic Table Detection**: Scans directories for .vpx files
- **Easy Table Launching**: Select and play tables with VPinballX_GL64.exe -play
- **Seamless Return**: Menu reappears when table exits
- **Configurable**: JSON configuration file for easy customization

## Requirements

- **Unity 2020.3 or later** (2021 LTS recommended)
- **SteamVR** installed and running
- **Visual Pinball X** with VPinballX_GL64.exe
- **VR Headset** compatible with SteamVR

## Installation

### Option 1: Build from Source

1. Clone or download this repository
2. Open the project in Unity Hub
3. Install required packages:
   - Go to **Window > Package Manager**
   - Install **XR Plugin Management**
   - Install **OpenXR Plugin** or **SteamVR Plugin**
4. Configure XR settings:
   - Go to **Edit > Project Settings > XR Plugin Management**
   - Enable **OpenXR** or **SteamVR** for your platform
   - **IMPORTANT**: Set **SteamVR** as the default OpenXR runtime in SteamVR settings
   - **For Meta Quest**: Also enable the **Oculus/Meta** plugin in XR Plugin Management
5. Open the main scene: `Assets/Scenes/VRLauncher.unity`
6. Build the project: **File > Build Settings > Build**

### Option 2: Use Pre-built Release

1. Download the latest release from the releases page
2. Extract to a folder of your choice
3. Edit `launcher-config.json` to set your paths
4. Run `VRLauncher.exe`

## Configuration

Edit `launcher-config.json` in the same folder as the executable:

```json
{
  "vpinballExecutable": "C:\\Visual Pinball\\VPinballX_GL64.exe",
  "tablesDirectory": "C:\\Visual Pinball\\Tables",
  "searchSubdirectories": true,
  "wheelDirectory": "C:\\Visual Pinball\\Media\\Wheel",
  "menuDistance": 2.0,
  "menuHeight": 1.5,
  "menuScale": 0.01,
  "showDebugConsole": false,
  "enableControllerBridge": false,
  "controllerBridgePath": "",
  "controllerBridgeArgs": "",
  "controllerBridgeWorkingDir": ""
}
```

### Configuration Options

- **vpinballExecutable**: Full path to VPinballX_GL64.exe
- **tablesDirectory**: Directory containing your .vpx table files
- **searchSubdirectories**: Whether to search subdirectories for tables
- **wheelDirectory**: Directory containing wheel images for tables (supports both absolute paths like `C:\\Visual Pinball\\Media\\Wheel` or relative paths like `Media\\Wheel`). Images should be named to match the table files (e.g., `TableName.png` for `TableName.vpx`). Supported formats: PNG, JPG, JPEG.
- **menuDistance**: Distance (in meters) to position menu from camera
- **menuHeight**: Height offset (in meters) for menu positioning
- **menuScale**: Scale factor for the menu UI
- **showDebugConsole**: Show on-screen VR controller/button debug overlays (useful for troubleshooting). Defaults to `false`.
- **enableControllerBridge**: Launch an external VR-controller input bridge when the launcher starts and stop it when the launcher exits. See [In-Game Controls](#in-game-controls-while-a-table-is-running). Defaults to `false`.
- **controllerBridgePath**: Full path to the bridge executable to launch (e.g. the AutoHotkey executable, or a compiled bridge). Leave empty to disable.
- **controllerBridgeArgs**: Command-line arguments for the bridge (e.g. the quoted full path to the AutoHotkey script).
- **controllerBridgeWorkingDir**: Working directory for the bridge process. Leave empty to use the executable's folder. For the AutoHotkey script, set this to the folder containing `auto_oculus_touch.dll` so the script can load it.

## Setup in Unity

### Scene Setup

1. Create a new scene or open `Assets/Scenes/VRLauncher.unity`
2. Add the **VRLauncherManager** prefab to the scene
3. Configure the Canvas UI:

#### Create Menu Canvas

1. **Create Canvas**:
   - Right-click in Hierarchy > UI > Canvas
   - Set **Render Mode** to **World Space**
   - Set **Event Camera** to Main Camera

2. **Create List Container**:
   - Right-click Canvas > Create Empty
   - Name it "ListContainer"
   - Add **Vertical Layout Group** component
   - Add **Content Size Fitter** component

3. **Create Table Item Prefab**:
   - Create UI > Button
   - Add Text child for table name
   - Save as prefab: `Assets/Prefabs/TableItem.prefab`

4. **Create Status Text**:
   - Create UI > Text
   - Position at top of canvas
   - Name it "StatusText"

5. **Configure VRMenuController**:
   - Select the VRLauncherManager object
   - Drag Canvas to **Menu Canvas** field
   - Drag ListContainer to **List Container** field
   - Drag TableItem prefab to **Table Item Prefab** field
   - Drag StatusText to **Status Text** field

### Script Components

The project includes these main scripts:

- **VRLauncherManager.cs**: Main manager (attach to root GameObject)
- **VRMenuController.cs**: Handles VR menu UI
- **TableScanner.cs**: Scans for .vpx files
- **TableLauncher.cs**: Launches Visual Pinball tables
- **LauncherConfig.cs**: Configuration management
- **UnityMainThreadDispatcher.cs**: Utility for thread-safe callbacks

## Usage

1. **Start SteamVR** if not already running
2. **Put on your VR headset**
3. **Launch the application**
4. The menu will appear in front of you showing available tables
5. **Point and click** on a table to launch it
6. The menu will hide while the table is running
7. When you exit the table, the menu reappears
8. Press **Escape** to quit (desktop mode only)

## Controls

### Menu Navigation (Carousel)

- **VR Controllers**:
  - **Left Trigger** — Previous table
  - **Right Trigger** — Next table
  - **A Button** (right controller) — Launch selected table
- **Keyboard**:
  - Left/Right Shift or Left/Right Arrow — Browse tables
  - Enter/Space — Launch table
  - Esc — Quit application

Menu input is read through the XR Input System (OpenXR), so it works reliably with Quest/Touch and other OpenXR controllers.

### In-Game Controls (While a Table is Running)

Visual Pinball's VR build reads keyboard and DirectInput, **not** VR motion controllers, and Unity has released its XR session to VPinballX while a table is running. So in-game controls are provided by an **external controller bridge** that reads the VR controllers and sends the keystrokes VPinballX expects. The launcher starts this bridge on startup and stops it on exit, controlled by the `enableControllerBridge` configuration options above.

A ready-to-use bridge is bundled in the [`ControllerBridge/`](ControllerBridge/) folder. **Setup is one step:** make sure that folder sits next to `vr-launch.exe`, then right-click `ControllerBridge/install.ps1` → **Run with PowerShell**. It installs AutoHotkey 1.1 if needed and wires the bridge into your config automatically (no vJoy, no kernel driver, no security changes). See [`ControllerBridge/README.md`](ControllerBridge/README.md) for details.

The bundled bridge ([`auto_oculus_touch`](https://github.com/rajetic/auto_oculus_touch/) driving an AutoHotkey script) uses these mappings, which match VPinballX's default keys:

| Control | Action | Key sent |
|---|---|---|
| **Left Trigger** | Left flipper | Left Shift |
| **Right Trigger** | Right flipper | Right Shift |
| **X Button** (left) | Start game | `1` |
| **B Button** (right) | Insert coin | `5` |
| **Y Button** (left) | Exit table | `Esc` |
| **Right Thumbstick** (pull back) | Plunger | Enter |

**Notes**:
- The bridge only sends keys while the **VPinballX window is focused**. This is why the triggers act as carousel navigation in the menu and as flippers in-game — there's no conflict.
- Mappings assume VPinballX's default key bindings. If yours differ, check **VPX > Preferences > Configure Keys** and adjust the bridge script accordingly.
- The bridge cannot run inside Unity (Unity hands its XR session to VPinballX during play); it must be a separate process, which is why the launcher spawns/kills it.

## Troubleshooting

### No Tables Appear

- Check that `tablesDirectory` in config points to correct folder
- Verify .vpx files exist in that directory
- Check Unity console for error messages
- Ensure `searchSubdirectories` is true if tables are in subfolders

### Table Won't Launch

- Verify `vpinballExecutable` path is correct
- Make sure VPinballX_GL64.exe exists at that location
- Check that the .vpx file is not corrupted
- Look for error messages in Unity console

### VR Not Working

- Ensure SteamVR is running before launching
- **Verify SteamVR is set as the default OpenXR runtime**:
  - Open SteamVR settings
  - Go to **OpenXR** section
  - Click **Set SteamVR as OpenXR Runtime**
- Check Project Settings > XR Plugin Management
- Verify OpenXR or SteamVR plugin is enabled
- **For Meta Quest users**: Enable both OpenXR and Oculus/Meta plugins
- Test that your headset works in other SteamVR apps

### Menu Not Visible

- Check that Canvas is set to World Space render mode
- Verify menuDistance and menuHeight values in config
- Try adjusting menuScale (default 0.01)
- Make sure camera has proper tracking

### Menu Doesn't Return After Exiting Table

- Check Unity console for process exit errors
- Verify the table process actually exited
- Try relaunching the application

### Controller Input Not Working in VPinballX

**Understanding the Input System**:
- Menu navigation reads the controllers directly via the XR Input System (OpenXR)
- In-game controls are provided by the external controller bridge (see [In-Game Controls](#in-game-controls-while-a-table-is-running)), which simulates keyboard input
- VR controllers don't appear in Windows joy.cpl and VPinballX can't read them as joysticks (this is normal — hence the bridge)

**Troubleshooting Steps**:
- Confirm the bridge is enabled and configured (`enableControllerBridge`, `controllerBridgePath`, `controllerBridgeArgs`, `controllerBridgeWorkingDir`) and that it actually launched with the app
- The bridge only sends keys while the **VPinballX window is focused** — make sure no other window (terminal, launcher menu, etc.) has stolen focus
- Verify your VPinballX key bindings match the bridge mappings (Shift for flippers, `1` = start, `5` = coin, Enter = plunger) under **VPX > Preferences > Configure Keys**
- Some tables require inserting a coin (B) before Start (X) does anything
- Confirm the VR runtime is running (e.g. Quest Link/Air Link) so the bridge can read the controllers
- Temporarily set `showDebugConsole` to `true` to see the `ControllerBridge: started …` log line and confirm the bridge launched

## Development

### Project Structure

```
vr-launch/
├── Assets/
│   ├── Scenes/
│   │   └── VRLauncher.unity
│   ├── Scripts/
│   │   ├── VRLauncherManager.cs
│   │   ├── VRMenuController.cs
│   │   ├── TableScanner.cs
│   │   ├── TableLauncher.cs
│   │   ├── LauncherConfig.cs
│   │   └── UnityMainThreadDispatcher.cs
│   └── Prefabs/
│       ├── VRLauncherManager.prefab
│       └── TableItem.prefab
└── launcher-config.json
```

### Building

1. **Configure Build Settings**:
   - File > Build Settings
   - Platform: Windows (64-bit)
   - Add scene to build list

2. **Player Settings**:
   - Company Name, Product Name
   - Icon (optional)
   - XR Settings: Ensure VR is enabled

3. **Build**:
   - Click "Build" and choose output folder
   - Copy `launcher-config.json` to build folder

### Extending

To add new features:

- **Custom table sorting**: Modify `TableScanner.cs`
- **Table metadata**: Extend `TableInfo` class
- **Additional launch parameters**: Modify `TableLauncher.cs`
- **Better UI**: Enhance the Canvas prefab
- **Controller input**: Add input handling in `VRMenuController.cs`

## Credits

Created for the Visual Pinball VR community.

## License

MIT License - Feel free to use and modify for your own purposes.

## Support

For issues and feature requests, please use the GitHub issues page.

## Tips

- **Performance**: If menu lags, reduce number of visible tables or optimize prefab
- **Positioning**: Adjust menuDistance/Height in config for comfort
- **Large Collections**: Enable subdirectory search and organize tables in folders
- **Quick Access**: Create shortcuts to favorite tables by organizing in subfolders
