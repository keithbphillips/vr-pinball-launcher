using System;
using System.IO;
using UnityEngine;

namespace VRLauncher
{
    /// <summary>
    /// Configuration settings for the VR Launcher
    /// Supports loading from JSON file for easy customization
    /// </summary>
    [Serializable]
    public class LauncherConfig
    {
        [Header("Visual Pinball Settings")]
        [Tooltip("Path to VPinballX_GL64.exe")]
        public string vpinballExecutable = @"C:\Visual Pinball\VPinballX_GL64.exe";

        [Tooltip("Directory containing .vpx table files")]
        public string tablesDirectory = @"C:\Visual Pinball\Tables";

        [Tooltip("Search subdirectories for tables")]
        public bool searchSubdirectories = true;

        [Tooltip("Directory containing wheel images (supports both relative and absolute paths)")]
        public string wheelDirectory = @"C:\Visual Pinball\Media\Wheel";

        [Header("Menu Settings")]
        [Tooltip("Distance from camera to place menu")]
        public float menuDistance = 2.0f;

        [Tooltip("Height offset for menu")]
        public float menuHeight = 1.5f;

        [Tooltip("Scale of menu panel")]
        public float menuScale = 0.01f;

        [Header("Application Settings")]
        [Tooltip("Show debug console window")]
        public bool showDebugConsole = true;

        private static LauncherConfig _instance;
        private static readonly string ConfigFileName = "launcher-config.json";

        /// <summary>
        /// Gets the singleton instance of the config
        /// </summary>
        public static LauncherConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the full path to the config file
        /// </summary>
        public static string ConfigFilePath
        {
            get
            {
                // Store config next to executable in standalone builds
                string path = Application.dataPath;
                if (Application.isEditor)
                {
                    // In editor, store in project root
                    path = Path.Combine(Application.dataPath, "..");
                }
                else
                {
                    // In build, store next to exe
                    path = Path.GetDirectoryName(Application.dataPath);
                }

                return Path.Combine(path, ConfigFileName);
            }
        }

        /// <summary>
        /// Loads configuration from file or creates default
        /// </summary>
        public static LauncherConfig Load()
        {
            string configPath = ConfigFilePath;

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    LauncherConfig config = JsonUtility.FromJson<LauncherConfig>(json);
                    Debug.Log($"Loaded configuration from: {configPath}");
                    Debug.Log($"Config values - tablesDirectory: {config.tablesDirectory}, vpinballExecutable: {config.vpinballExecutable}");
                    return config;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading config: {ex.Message}");
                    Debug.Log("Using default configuration");
                    return CreateDefault();
                }
            }
            else
            {
                Debug.Log($"Config file not found at: {configPath}");
                LauncherConfig config = CreateDefault();
                config.Save();
                return config;
            }
        }

        /// <summary>
        /// Saves configuration to file
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(this, true);
                string configPath = ConfigFilePath;
                File.WriteAllText(configPath, json);
                Debug.Log($"Saved configuration to: {configPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving config: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        private static LauncherConfig CreateDefault()
        {
            LauncherConfig config = new LauncherConfig();

            // Try to auto-detect common installation paths
            string[] commonPaths = new string[]
            {
                @"C:\Visual Pinball\VPinballX_GL64.exe",
                @"C:\Games\Visual Pinball\VPinballX_GL64.exe",
                @"C:\Program Files\Visual Pinball\VPinballX_GL64.exe",
                @"C:\Program Files (x86)\Visual Pinball\VPinballX_GL64.exe"
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                {
                    config.vpinballExecutable = path;
                    config.tablesDirectory = Path.Combine(
                        Path.GetDirectoryName(path),
                        "Tables"
                    );
                    Debug.Log($"Auto-detected Visual Pinball at: {path}");
                    break;
                }
            }

            return config;
        }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrEmpty(vpinballExecutable))
            {
                errorMessage = "VPinball executable path is not set";
                return false;
            }

            if (!File.Exists(vpinballExecutable))
            {
                errorMessage = $"VPinball executable not found: {vpinballExecutable}";
                return false;
            }

            if (string.IsNullOrEmpty(tablesDirectory))
            {
                errorMessage = "Tables directory is not set";
                return false;
            }

            if (!Directory.Exists(tablesDirectory))
            {
                errorMessage = $"Tables directory not found: {tablesDirectory}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Opens the config file in the default text editor
        /// </summary>
        public static void OpenConfigFile()
        {
            string configPath = ConfigFilePath;

            if (!File.Exists(configPath))
            {
                Instance.Save();
            }

            try
            {
                System.Diagnostics.Process.Start(configPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error opening config file: {ex.Message}");
            }
        }
    }
}
