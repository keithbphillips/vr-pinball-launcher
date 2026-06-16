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
                    $"ControllerBridge: enabled but executable not found: '{path}'");
                return;
            }

            string workingDir = config.controllerBridgeWorkingDir;
            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetDirectoryName(path);
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
    }
}
