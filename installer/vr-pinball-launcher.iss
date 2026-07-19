; vr-pinball-launcher.iss -- Inno Setup script for the VR Pinball Launcher.
;
; Builds VRPinballLauncher-Setup.exe, which installs the launcher, creates
; Start Menu / Desktop shortcuts, registers an uninstaller, and optionally runs
; install.ps1 to set up VPinballX VR and the controller bridge.
;
; Build it with build-installer.ps1 (repo root), or directly:
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ^
;       /DAppVersion=1.0.0 /DBuildDir=..\Build installer\vr-pinball-launcher.iss
;
; Defines (override with ISCC /D...):
;   AppVersion  -- version shown in the wizard / Add-Remove Programs
;   BuildDir    -- folder with the Unity build output (vr-launch.exe, *_Data, ...)

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef BuildDir
  #define BuildDir "..\Build"
#endif

#define AppName "VR Pinball Launcher"
#define AppExeName "vr-launch.exe"
#define AppPublisher "keithbphillips"
#define AppUrl "https://github.com/keithbphillips/vr-pinball-launcher"

[Setup]
AppId={{8804A507-FC38-44F5-9E05-CF9BF2913F64}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
WizardStyle=modern
; Let the user choose "all users" (Program Files, needs admin) or "just me"
; (%LocalAppData%\Programs, no admin). {autopf} follows that choice.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\VR Pinball Launcher
DefaultGroupName=VR Pinball Launcher
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=VRPinballLauncher-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"
Name: "setupvpxvr"; Description: "Set up VPinballX for VR (downloads the VR build if you don't have it)"; GroupDescription: "Additional setup (recommended):"
Name: "setupvpinmame"; Description: "Set up VPinMAME, the ROM emulator most tables need (needs admin to register)"; GroupDescription: "Additional setup (recommended):"
Name: "setupbridge"; Description: "Set up the VR controller bridge (installs AutoHotkey if needed)"; GroupDescription: "Additional setup (recommended):"

[Files]
; Unity build output. The dev build's launcher-config.json holds machine-specific
; paths, so exclude it and ship a clean default instead (below).
Source: "{#BuildDir}\*"; DestDir: "{app}"; Excludes: "launcher-config.json"; Flags: recursesubdirs createallsubdirs ignoreversion
; Clean default config -- only on a fresh install, never overwrite a user's edits.
Source: "launcher-config.default.json"; DestDir: "{app}"; DestName: "launcher-config.json"; Flags: onlyifdoesntexist
; Controller bridge + the setup script (sourced from the repo root, not the build).
Source: "..\ControllerBridge\*"; DestDir: "{app}\ControllerBridge"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "..\install.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\VR Pinball Launcher"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall VR Pinball Launcher"; Filename: "{uninstallexe}"
Name: "{autodesktop}\VR Pinball Launcher"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; Optional post-install setup steps -- only run for ticked tasks. A console
; window is shown so the user can see the VPX download / AutoHotkey installer.
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install.ps1"" -Setup vpx -Yes"; \
  WorkingDir: "{app}"; StatusMsg: "Setting up VPinballX for VR..."; \
  Flags: waituntilterminated; Tasks: setupvpxvr
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install.ps1"" -Setup vpinmame -Yes"; \
  WorkingDir: "{app}"; StatusMsg: "Setting up VPinMAME (ROM emulator)..."; \
  Flags: waituntilterminated; Tasks: setupvpinmame
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install.ps1"" -Setup bridge"; \
  WorkingDir: "{app}"; StatusMsg: "Setting up the controller bridge..."; \
  Flags: waituntilterminated; Tasks: setupbridge
; Offer to launch when the wizard finishes.
Filename: "{app}\{#AppExeName}"; Description: "Launch VR Pinball Launcher now"; \
  Flags: nowait postinstall skipifsilent
