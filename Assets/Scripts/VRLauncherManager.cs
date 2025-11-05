using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VRLauncher
{
    /// <summary>
    /// Main manager that orchestrates the VR launcher application
    /// Ensures SteamVR is initialized and manages application lifecycle
    /// </summary>
    public class VRLauncherManager : MonoBehaviour
    {
        [Header("References")]
        public VRMenuController menuController;
        public TableScanner tableScanner;
        public TableLauncher tableLauncher;

        [Header("Settings")]
        [Tooltip("Automatically initialize VR on startup")]
        public bool autoInitVR = true;

        private bool vrInitialized = false;
        private Keyboard keyboard;

        void Awake()
        {
            // Ensure this persists across scenes
            DontDestroyOnLoad(gameObject);

            // Initialize keyboard reference
            keyboard = Keyboard.current;

            // Initialize VR if needed
            if (autoInitVR)
            {
                InitializeVR();
            }
        }

        void Start()
        {
            // Find components if not set
            if (menuController == null)
            {
                menuController = FindFirstObjectByType<VRMenuController>();
            }

            if (tableScanner == null)
            {
                tableScanner = FindFirstObjectByType<TableScanner>();
            }

            if (tableLauncher == null)
            {
                tableLauncher = FindFirstObjectByType<TableLauncher>();
            }

            // Verify all components exist
            if (menuController == null)
            {
                Debug.LogError("VRMenuController not found! Please add it to the scene.");
            }

            if (tableScanner == null)
            {
                Debug.LogError("TableScanner not found! Please add it to the scene.");
            }

            if (tableLauncher == null)
            {
                Debug.LogError("TableLauncher not found! Please add it to the scene.");
            }

            CheckVRStatus();
        }

        /// <summary>
        /// Initializes VR system
        /// </summary>
        void InitializeVR()
        {
            if (vrInitialized) return;

            Debug.Log("Initializing VR...");

            // Unity's XR system should auto-initialize if configured in project settings
            // For SteamVR, ensure SteamVR plugin is installed via Unity Package Manager

            vrInitialized = true;
            Debug.Log("VR initialization complete");
        }

        /// <summary>
        /// Checks VR status and logs information
        /// </summary>
        void CheckVRStatus()
        {
            bool vrEnabled = XRSettings.enabled;
            bool vrSupported = XRSettings.isDeviceActive;
            string deviceName = XRSettings.loadedDeviceName;

            Debug.Log($"VR Enabled: {vrEnabled}");
            Debug.Log($"VR Device Active: {vrSupported}");
            Debug.Log($"VR Device Name: {deviceName}");

            if (!vrEnabled)
            {
                Debug.LogWarning("VR is not enabled! Check Project Settings > XR Plugin Management");
            }

            if (!vrSupported)
            {
                Debug.LogWarning("No VR device detected! Make sure SteamVR is running.");
            }
        }

        /// <summary>
        /// Refreshes the table list
        /// </summary>
        public void RefreshTables()
        {
            if (menuController != null)
            {
                menuController.RefreshTableList();
            }
        }

        void Update()
        {
            // Check for quit command (useful for development)
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("Escape pressed - quitting application");
                QuitApplication();
            }
        }

        /// <summary>
        /// Quits the application
        /// </summary>
        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
