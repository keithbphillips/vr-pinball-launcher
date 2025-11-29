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
  "showDebugConsole": true
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
- **showDebugConsole**: Show Unity debug console (useful for troubleshooting)

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

### Menu Navigation
- **VR Controllers (Joystick Mode)**:
  - **Left Trigger** (Button 14) - Previous table
  - **Right Trigger** (Button 15) - Next table
  - **Right A Button** (Button 0) - Launch table
- **Keyboard**:
  - Left/Right Shift or Arrow Keys - Browse tables
  - Enter/Space - Launch table
  - ESC - Quit application

### In-Game Controls (While Table is Running)

VR controller buttons are automatically converted to keyboard inputs for VPinballX using the Windows SendInput API. This works because VR controllers use OpenXR/SteamVR APIs that VPinballX can't directly access, so we simulate the keyboard presses that VPinballX expects.

**VR Controller Mappings** (automatically converted to keyboard):

- **Left Trigger** → Left Flipper (simulates Left Shift key)
- **Right Trigger** → Right Flipper (simulates Right Shift key)
- **Right Grip** → Launch Ball/Plunger (simulates Space key)
- **Left Primary Button (X/A)** → Nudge Forward (simulates Up Arrow)
- **Left Joystick** → Nudge Left/Right/Back (simulates Arrow Keys)
- **Right Secondary Button (B/A)** → Exit Table (simulates Escape key)

**Note**: Controller input is only active while a table is running and automatically disabled in the menu. The menu uses native joystick detection (Unity's OpenXR support), while in-game uses keyboard simulation (VPinballX compatibility).

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
- Menu navigation uses native joystick detection (Unity/OpenXR)
- In-game controls use keyboard simulation (VPinballX compatibility)
- VR controllers don't appear in Windows joy.cpl (this is normal)
- VPinballX can't see VR controllers as joysticks (uses DirectInput API)

**Troubleshooting Steps**:
- Ensure a table is running (controller input only works during gameplay)
- Check that VPinballX window has focus (automatic after v1.1.0)
- Verify controllers are tracked in SteamVR
- Enable debug logging in VRControllerInput component to see input events
- Check VPinballX key bindings match the default mappings (Shift for flippers, etc.)
- Some tables may use custom key bindings - check table documentation
- See TEST_KEYBOARD_SIMULATION.md for detailed testing guide

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
