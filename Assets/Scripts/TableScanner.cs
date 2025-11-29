using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VRLauncher
{
    /// <summary>
    /// Scans directories for Visual Pinball table files (.vpx)
    /// </summary>
    public class TableScanner : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Directory containing .vpx table files")]
        public string tablesDirectory = @"C:\Visual Pinball\Tables";

        [Tooltip("Directory containing wheel images")]
        public string wheelDirectory = @"Media\Wheel";

        [Tooltip("Search subdirectories for tables")]
        public bool searchSubdirectories = true;

        private List<TableInfo> cachedTables = new List<TableInfo>();

        public class TableInfo
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
            public string Directory { get; set; }
            public string WheelImagePath { get; set; }

            public TableInfo(string fullPath)
            {
                FullPath = fullPath;
                Name = Path.GetFileNameWithoutExtension(fullPath);
                Directory = Path.GetDirectoryName(fullPath);
                WheelImagePath = null;
            }
        }

        /// <summary>
        /// Scans the configured directory for .vpx files
        /// </summary>
        /// <returns>List of found table files</returns>
        public List<TableInfo> ScanForTables()
        {
            cachedTables.Clear();

            if (string.IsNullOrEmpty(tablesDirectory))
            {
                Debug.LogError("Tables directory is not configured!");
                return cachedTables;
            }

            if (!Directory.Exists(tablesDirectory))
            {
                Debug.LogError($"Tables directory does not exist: {tablesDirectory}");
                return cachedTables;
            }

            try
            {
                SearchOption searchOption = searchSubdirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                string[] tableFiles = Directory.GetFiles(
                    tablesDirectory,
                    "*.vpx",
                    searchOption
                );

                cachedTables = tableFiles
                    .Select(path => new TableInfo(path))
                    .OrderBy(table => table.Name)
                    .ToList();

                Debug.Log($"Found {cachedTables.Count} table(s) in {tablesDirectory}");

                // Load wheel images for tables
                LoadWheelImages();

                return cachedTables;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error scanning for tables: {ex.Message}");
                return cachedTables;
            }
        }

        /// <summary>
        /// Gets the cached list of tables (call ScanForTables first)
        /// </summary>
        public List<TableInfo> GetTables()
        {
            return cachedTables;
        }

        /// <summary>
        /// Refreshes the table list
        /// </summary>
        public void RefreshTables()
        {
            ScanForTables();
        }

        /// <summary>
        /// Loads wheel images for scanned tables
        /// </summary>
        private void LoadWheelImages()
        {
            if (string.IsNullOrEmpty(wheelDirectory))
            {
                Debug.LogWarning("Wheel directory not configured - skipping wheel image loading");
                return;
            }

            // Support both absolute and relative paths
            string wheelPath = wheelDirectory;
            if (!Path.IsPathRooted(wheelPath))
            {
                // Relative to working directory
                wheelPath = Path.Combine(Application.dataPath, "..", wheelDirectory);
                wheelPath = Path.GetFullPath(wheelPath);
            }

            if (!Directory.Exists(wheelPath))
            {
                Debug.LogWarning($"Wheel directory does not exist: {wheelPath} - skipping wheel image loading");
                return;
            }

            Debug.Log($"Scanning for wheel images in: {wheelPath}");

            // Scan for image files (png, jpg, jpeg)
            string[] imageExtensions = { "*.png", "*.jpg", "*.jpeg" };
            var imageFiles = new List<string>();

            foreach (var ext in imageExtensions)
            {
                imageFiles.AddRange(Directory.GetFiles(wheelPath, ext, SearchOption.TopDirectoryOnly));
            }

            Debug.Log($"Found {imageFiles.Count} wheel images");

            // Match images to tables by name
            int matchedCount = 0;
            foreach (var table in cachedTables)
            {
                // Try to find image with same name as table
                var matchingImage = imageFiles.FirstOrDefault(img =>
                {
                    string imageName = Path.GetFileNameWithoutExtension(img);
                    return imageName.Equals(table.Name, System.StringComparison.OrdinalIgnoreCase);
                });

                if (matchingImage != null)
                {
                    table.WheelImagePath = matchingImage;
                    matchedCount++;
                    Debug.Log($"Matched wheel image for '{table.Name}': {Path.GetFileName(matchingImage)}");
                }
            }

            Debug.Log($"Matched {matchedCount} wheel images to tables");
        }

        void Awake()
        {
            // Load configuration first thing
            LauncherConfig config = LauncherConfig.Instance;
            tablesDirectory = config.tablesDirectory;
            searchSubdirectories = config.searchSubdirectories;

            Debug.Log($"TableScanner.Awake: Loaded config - tablesDirectory={tablesDirectory}, searchSubdirs={searchSubdirectories}");
        }

        void Start()
        {
            Debug.Log($"TableScanner.Start: Scanning for tables in {tablesDirectory}");
            // Scan on startup
            ScanForTables();
        }
    }
}
