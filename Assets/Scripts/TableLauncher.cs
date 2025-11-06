using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        // Windows API for window focus management
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Window show commands
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        // SetWindowPos flags
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [Header("Configuration")]
        [Tooltip("Path to VPinballX_GL64.exe")]
        public string vpinballExecutable = @"C:\Visual Pinball\VPinballX_GL64.exe";

        [Tooltip("Time in seconds to wait before attempting to focus the VP window")]
        public float focusDelaySeconds = 2.0f;

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

                    // Start coroutine to focus the VPinballX window after a delay
                    StartCoroutine(FocusVPinballWindow());

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
        /// Waits for VPinballX window to appear and sets focus to it
        /// </summary>
        private IEnumerator FocusVPinballWindow()
        {
            UnityEngine.Debug.Log($"Waiting {focusDelaySeconds} seconds before focusing VPinballX window...");
            yield return new WaitForSeconds(focusDelaySeconds);

            if (currentProcess == null || currentProcess.HasExited)
            {
                UnityEngine.Debug.LogWarning("Process exited before window focus could be set");
                yield break;
            }

            // Try multiple times to find and focus the window
            bool focused = false;
            for (int attempt = 0; attempt < 10 && !focused; attempt++)
            {
                if (currentProcess == null || currentProcess.HasExited)
                {
                    break;
                }

                // Try to get the main window handle
                IntPtr mainWindowHandle = IntPtr.Zero;
                bool windowIsMinimized = false;

                try
                {
                    mainWindowHandle = currentProcess.MainWindowHandle;
                    if (mainWindowHandle != IntPtr.Zero)
                    {
                        windowIsMinimized = IsIconic(mainWindowHandle);
                    }
                    else
                    {
                        // MainWindowHandle not available yet, try refreshing
                        currentProcess.Refresh();
                        UnityEngine.Debug.Log($"Window handle not ready yet, attempt {attempt + 1}/10");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error getting window handle on attempt {attempt + 1}: {ex.Message}");
                }

                if (mainWindowHandle != IntPtr.Zero)
                {
                    UnityEngine.Debug.Log($"Found VPinballX main window handle: {mainWindowHandle}");

                    // If window is minimized, restore it
                    if (windowIsMinimized)
                    {
                        UnityEngine.Debug.Log("Window is minimized, restoring...");
                        try
                        {
                            ShowWindow(mainWindowHandle, SW_RESTORE);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"Error restoring window: {ex.Message}");
                        }
                        yield return new WaitForSeconds(0.2f);
                    }

                    // Make window topmost temporarily to ensure it comes to front
                    try
                    {
                        SetWindowPos(mainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0,
                                   SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Error setting window topmost: {ex.Message}");
                    }

                    // Set focus
                    bool result = false;
                    try
                    {
                        result = SetForegroundWindow(mainWindowHandle);
                        if (result)
                        {
                            UnityEngine.Debug.Log("Successfully focused VPinballX window");
                            focused = true;
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"SetForegroundWindow failed on attempt {attempt + 1}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Error setting foreground window: {ex.Message}");
                    }

                    // Remove topmost flag after a brief delay (outside try-catch)
                    if (result)
                    {
                        yield return new WaitForSeconds(0.1f);

                        try
                        {
                            SetWindowPos(mainWindowHandle, IntPtr.Zero, 0, 0, 0, 0,
                                       SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"Error removing topmost flag: {ex.Message}");
                        }
                    }
                }

                if (!focused)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            if (!focused)
            {
                UnityEngine.Debug.LogWarning("Could not focus VPinballX window after multiple attempts");
            }
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

        private IEnumerator StartXRCoroutine()
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
