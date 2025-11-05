using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRLauncher
{
    /// <summary>
    /// Controls the VR menu interface for table selection
    /// Works with Unity's built-in VR support or SteamVR
    /// </summary>
    public class VRMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Container for table list items")]
        public Transform listContainer;

        [Tooltip("Prefab for table list items")]
        public GameObject tableItemPrefab;

        [Tooltip("Text to display current status")]
        public Text statusText;

        [Tooltip("Main menu canvas")]
        public Canvas menuCanvas;

        [Header("Menu Settings")]
        [Tooltip("Distance from camera to place menu")]
        public float menuDistance = 2.0f;

        [Tooltip("Height offset for menu")]
        public float menuHeight = 1.5f;

        [Tooltip("Scale of menu panel")]
        public float menuScale = 0.01f;

        private TableScanner tableScanner;
        private TableLauncher tableLauncher;
        private List<GameObject> tableListItems = new List<GameObject>();

        void Start()
        {
            // Get or create required components
            tableScanner = FindObjectOfType<TableScanner>();
            if (tableScanner == null)
            {
                tableScanner = gameObject.AddComponent<TableScanner>();
            }

            tableLauncher = FindObjectOfType<TableLauncher>();
            if (tableLauncher == null)
            {
                tableLauncher = gameObject.AddComponent<TableLauncher>();
            }

            // Subscribe to table exit event
            tableLauncher.OnTableExited += OnTableExited;

            // Ensure dispatcher exists
            var dispatcher = UnityMainThreadDispatcher.Instance;

            // Position menu in VR space
            PositionMenuInVR();

            // Load and display tables
            RefreshTableList();
        }

        void OnDestroy()
        {
            if (tableLauncher != null)
            {
                tableLauncher.OnTableExited -= OnTableExited;
            }
        }

        /// <summary>
        /// Positions the menu canvas in VR space relative to the camera
        /// </summary>
        void PositionMenuInVR()
        {
            if (menuCanvas == null) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Position menu in front of camera
            Vector3 menuPosition = mainCamera.transform.position +
                                  mainCamera.transform.forward * menuDistance +
                                  Vector3.up * menuHeight;

            menuCanvas.transform.position = menuPosition;
            menuCanvas.transform.rotation = Quaternion.LookRotation(
                menuCanvas.transform.position - mainCamera.transform.position
            );
            menuCanvas.transform.localScale = Vector3.one * menuScale;
        }

        /// <summary>
        /// Refreshes the table list from the scanner
        /// </summary>
        public void RefreshTableList()
        {
            // Clear existing items
            ClearTableList();

            // Get tables
            List<TableScanner.TableInfo> tables = tableScanner.ScanForTables();

            // Create UI items
            foreach (var table in tables)
            {
                CreateTableListItem(table);
            }

            UpdateStatus($"Found {tables.Count} table(s)");
        }

        /// <summary>
        /// Creates a UI list item for a table
        /// </summary>
        void CreateTableListItem(TableScanner.TableInfo table)
        {
            if (tableItemPrefab == null || listContainer == null)
            {
                Debug.LogError("Table item prefab or list container not set!");
                return;
            }

            GameObject item = Instantiate(tableItemPrefab, listContainer);

            // Set table name
            Text nameText = item.GetComponentInChildren<Text>();
            if (nameText != null)
            {
                nameText.text = table.Name;
            }

            // Add click handler
            Button button = item.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnTableSelected(table));
            }

            tableListItems.Add(item);
        }

        /// <summary>
        /// Clears all table list items
        /// </summary>
        void ClearTableList()
        {
            foreach (var item in tableListItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            tableListItems.Clear();
        }

        /// <summary>
        /// Called when a table is selected from the menu
        /// </summary>
        void OnTableSelected(TableScanner.TableInfo table)
        {
            Debug.Log($"Table selected: {table.Name}");
            UpdateStatus($"Launching: {table.Name}...");

            bool success = tableLauncher.LaunchTable(table.FullPath);

            if (success)
            {
                // Hide menu while table is running
                SetMenuVisible(false);
                UpdateStatus($"Playing: {table.Name}");
            }
            else
            {
                UpdateStatus("Failed to launch table!");
            }
        }

        /// <summary>
        /// Called when a table exits
        /// </summary>
        void OnTableExited()
        {
            Debug.Log("Table exited, showing menu");

            // Show menu again
            SetMenuVisible(true);
            UpdateStatus("Select a table to play");

            // Reposition menu in front of current camera view
            PositionMenuInVR();
        }

        /// <summary>
        /// Shows or hides the menu
        /// </summary>
        void SetMenuVisible(bool visible)
        {
            if (menuCanvas != null)
            {
                menuCanvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Updates the status text
        /// </summary>
        void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"Status: {message}");
        }

        void Update()
        {
            // Optional: Allow menu repositioning with a button press
            // You can add controller input here to reposition the menu
        }
    }
}
