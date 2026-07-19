using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace VRLauncher
{
    /// <summary>
    /// Starts an external VR-controller input bridge process when the launcher
    /// starts and terminates it when the launcher exits. The bridge reads the VR
    /// controllers and simulates keyboard input for VPinballX (it cannot live in
    /// Unity, since Unity releases its XR session while a table is running).
    ///
    /// Configured via LauncherConfig: enableControllerBridge, controllerBridgePath,
    /// controllerBridgeArgs, controllerBridgeWorkingDir.
    /// </summary>
    public class ControllerBridge : MonoBehaviour
    {
        private Process bridgeProcess;

        void Start()
        {
            LauncherConfig config = LauncherConfig.Instance;

            if (!config.enableControllerBridge)
            {
                return;
            }

            string path = config.controllerBridgePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                UnityEngine.Debug.LogWarning(
                    $"ControllerBridge: enabled but AutoHotkey executable not found: '{path}'. " +
                    "Re-run install.ps1 (Controller bridge) to install AutoHotkey and fix the config.");
                return;
            }

            string workingDir = config.controllerBridgeWorkingDir;
            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetDirectoryName(path);
            }

            // Verify the bridge script and the native files it loads are present
            // before launching AutoHotkey. Otherwise AHK starts and pops a cryptic
            // "auto_oculus_touch.dll file is missing" dialog on top of the launcher.
            if (!BridgeFilesPresent(config.controllerBridgeArgs, workingDir, out string missing))
            {
                UnityEngine.Debug.LogWarning(
                    $"ControllerBridge: not starting the bridge -- {missing} " +
                    "Make sure the ControllerBridge folder (with auto_oculus_touch.dll and " +
                    "vJoyInterface.dll) sits next to vr-launch.exe, then re-run install.ps1 " +
                    "(Controller bridge).");
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = config.controllerBridgeArgs ?? "",
                    UseShellExecute = false,
                    WorkingDirectory = workingDir
                };

                bridgeProcess = Process.Start(startInfo);
                UnityEngine.Debug.Log(
                    $"ControllerBridge: started '{path}' {config.controllerBridgeArgs} (cwd: {workingDir})");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ControllerBridge: failed to start: {ex.Message}");
            }
        }

        void OnApplicationQuit()
        {
            KillBridge();
        }

        void OnDestroy()
        {
            KillBridge();
        }

        private void KillBridge()
        {
            if (bridgeProcess == null)
            {
                return;
            }

            try
            {
                if (!bridgeProcess.HasExited)
                {
                    bridgeProcess.Kill();
                    bridgeProcess.WaitForExit(2000);
                    UnityEngine.Debug.Log("ControllerBridge: bridge process terminated");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"ControllerBridge: error killing bridge: {ex.Message}");
            }
            finally
            {
                bridgeProcess.Dispose();
                bridgeProcess = null;
            }
        }

        /// <summary>
        /// Checks that the bridge script and the native files it loads at startup
        /// are present, so we can skip launching AutoHotkey (and its error dialog)
        /// when something is missing. Returns false with a human-readable reason.
        /// </summary>
        private static bool BridgeFilesPresent(string scriptArgs, string workingDir, out string missing)
        {
            missing = null;

            // The bridge arguments are normally the quoted full path to the .ahk.
            string scriptPath = ExtractScriptPath(scriptArgs);

            // Folder that should hold the script and its sibling .ahk/.dll files.
            string bridgeDir = !string.IsNullOrEmpty(scriptPath)
                ? Path.GetDirectoryName(scriptPath)
                : workingDir;

            if (!string.IsNullOrEmpty(scriptPath) && !File.Exists(scriptPath))
            {
                missing = $"bridge script not found: '{scriptPath}'.";
                return false;
            }

            if (string.IsNullOrEmpty(bridgeDir) || !Directory.Exists(bridgeDir))
            {
                missing = $"bridge folder not found: '{bridgeDir}'.";
                return false;
            }

            // auto_oculus_touch.dll is built with vJoy support, so vJoyInterface.dll
            // must be present too or the DLL fails to load.
            string[] required = { "auto_oculus_touch.ahk", "auto_oculus_touch.dll", "vJoyInterface.dll" };
            foreach (string file in required)
            {
                if (!File.Exists(Path.Combine(bridgeDir, file)))
                {
                    missing = $"'{file}' is missing from '{bridgeDir}'.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Pulls the script path out of the bridge arguments, which are normally the
        /// quoted full path to the .ahk file (e.g. "C:\...\vpx_vr_controls.ahk").
        /// </summary>
        private static string ExtractScriptPath(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return null;
            }

            args = args.Trim();
            if (args.StartsWith("\""))
            {
                int end = args.IndexOf('"', 1);
                if (end > 0)
                {
                    return args.Substring(1, end - 1);
                }
            }
            return args;
        }
    }
}
