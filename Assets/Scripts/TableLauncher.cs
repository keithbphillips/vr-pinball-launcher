using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;

namespace VRLauncher
{
    /// <summary>
    /// Launches Visual Pinball tables and monitors the process
    /// </summary>
    public class TableLauncher : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Path to VPinballX_GL64.exe")]
        public string vpinballExecutable = @"C:\Visual Pinball\VPinballX_GL64.exe";

        private Process currentProcess;
        private bool isTableRunning = false;
        private string currentTablePath;
        private Keyboard keyboard;

        public delegate void TableExitedHandler();
        public event TableExitedHandler OnTableExited;

        /// <summary>
        /// Launches a Visual Pinball table
        /// </summary>
        /// <param name="tablePath">Full path to the .vpx file</param>
        /// <returns>True if launch was successful</returns>
        public bool LaunchTable(string tablePath)
        {
            if (isTableRunning)
            {
                UnityEngine.Debug.LogWarning("A table is already running!");
                return false;
            }

            if (!File.Exists(vpinballExecutable))
            {
                UnityEngine.Debug.LogError($"VPinballX executable not found: {vpinballExecutable}");
                return false;
            }

            if (!File.Exists(tablePath))
            {
                UnityEngine.Debug.LogError($"Table file not found: {tablePath}");
                return false;
            }

            try
            {
                currentTablePath = tablePath;

                // Stop Unity's XR session before launching VPinballX
                UnityEngine.Debug.Log("Stopping Unity XR session before launching table...");
                StopXR();

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = vpinballExecutable,
                    Arguments = $"-minimized -Play \"{tablePath}\"",
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(vpinballExecutable)
                };

                UnityEngine.Debug.Log($"Launching table: {Path.GetFileName(tablePath)}");
                UnityEngine.Debug.Log($"Command: {startInfo.FileName} {startInfo.Arguments}");

                currentProcess = Process.Start(startInfo);

                if (currentProcess != null)
                {
                    isTableRunning = true;
                    currentProcess.EnableRaisingEvents = true;
                    currentProcess.Exited += OnProcessExited;

                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError("Failed to start process");
                    // Restart XR if launch failed
                    StartXR();
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error launching table: {ex.Message}");
                return false;
            }
        }

        private void OnProcessExited(object sender, System.EventArgs e)
        {
            UnityEngine.Debug.Log($"Table exited: {Path.GetFileName(currentTablePath)}");
            isTableRunning = false;
            currentTablePath = null;

            // Restart Unity's XR session after table exits
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                UnityEngine.Debug.Log("Table exited - Restarting Unity XR session...");
                StartXR();
                OnTableExited?.Invoke();
            });
        }

        /// <summary>
        /// Checks if a table is currently running
        /// </summary>
        public bool IsTableRunning()
        {
            return isTableRunning;
        }

        /// <summary>
        /// Gets the currently running table path
        /// </summary>
        public string GetCurrentTablePath()
        {
            return currentTablePath;
        }

        void Awake()
        {
            keyboard = Keyboard.current;
        }

        void Update()
        {
            // Check if process has exited (fallback if event doesn't fire)
            if (isTableRunning && currentProcess != null && currentProcess.HasExited)
            {
                UnityEngine.Debug.Log("Detected table exit via polling");
                isTableRunning = false;
                string exitedTable = currentTablePath;
                currentTablePath = null;
                OnTableExited?.Invoke();
            }

            // Allow manual exit with Backspace key - kills VPinballX and returns to menu
            if (isTableRunning && keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
            {
                UnityEngine.Debug.Log("BACKSPACE pressed - Killing VPinballX process and returning to menu");
                KillCurrentTable();
            }
        }

        /// <summary>
        /// Manually kills the current VPinballX process
        /// </summary>
        public void KillCurrentTable()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                try
                {
                    UnityEngine.Debug.Log("Terminating VPinballX process");
                    currentProcess.Kill();
                    currentProcess.WaitForExit(2000); // Wait up to 2 seconds for clean exit
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error killing process: {ex.Message}");
                }
            }

            isTableRunning = false;
            currentTablePath = null;

            // Restart XR after killing table
            UnityEngine.Debug.Log("Table killed - Restarting Unity XR session...");
            StartXR();

            OnTableExited?.Invoke();
        }

        /// <summary>
        /// Stops Unity's XR session
        /// </summary>
        private void StopXR()
        {
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null && xrManager.isInitializationComplete)
            {
                UnityEngine.Debug.Log("Stopping XR...");
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
                UnityEngine.Debug.Log("XR stopped");
            }
        }

        /// <summary>
        /// Starts Unity's XR session
        /// </summary>
        private void StartXR()
        {
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null && !xrManager.isInitializationComplete)
            {
                UnityEngine.Debug.Log("Starting XR...");
                StartCoroutine(StartXRCoroutine());
            }
        }

        private System.Collections.IEnumerator StartXRCoroutine()
        {
            UnityEngine.Debug.Log("Initializing XR...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                UnityEngine.Debug.Log("XR Loader initialized, starting subsystems...");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                UnityEngine.Debug.Log("XR started successfully");
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to initialize XR loader");
            }
        }

        void OnDestroy()
        {
            // Clean up process monitoring
            if (currentProcess != null)
            {
                currentProcess.Exited -= OnProcessExited;
                currentProcess.Dispose();
            }
        }
    }
}
