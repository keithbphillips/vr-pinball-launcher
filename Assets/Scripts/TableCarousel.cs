using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using TMPro;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace VRLauncher
{
    /// <summary>
    /// Carousel wheel system for table selection in VR
    /// Navigate with Left/Right Shift or Arrow keys, launch with Enter/Space
    /// </summary>
    public class TableCarousel : MonoBehaviour
    {
        // Windows API for direct keyboard state checking
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Virtual key codes
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_RETURN = 0x0D;
        private const int VK_SPACE = 0x20;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_LEFT = 0x25;
        private const int VK_RIGHT = 0x27;

        private bool leftShiftWasPressed = false;
        private bool rightShiftWasPressed = false;
        private bool returnWasPressed = false;
        private bool spaceWasPressed = false;
        private bool escapeWasPressed = false;
        private bool leftWasPressed = false;
        private bool rightWasPressed = false;

        // VR controller (XR Input System) state tracking
        // Legacy Input.GetKey(JoystickButtonXX) does not reliably report release
        // for OpenXR controllers, so triggers are read as analog feature values
        // with separate press/release thresholds (hysteresis) for clean edges.
        private XRInputDevice leftXRController;
        private XRInputDevice rightXRController;
        private bool xrLeftTriggerWasPressed = false;   // Left Trigger - Previous
        private bool xrRightTriggerWasPressed = false;  // Right Trigger - Next
        private bool xrLaunchWasPressed = false;         // Primary button (A) - Launch

        private const float TriggerPressThreshold = 0.7f;
        private const float TriggerReleaseThreshold = 0.4f;

        [Header("UI References")]
        [Tooltip("Image to display the current table icon")]
        public Image tableIcon;

        [Tooltip("Text to display the current table name")]
        public TextMeshProUGUI tableNameText;

        [Tooltip("Text to display control instructions")]
        public TextMeshProUGUI controlsText;

        [Tooltip("Canvas containing the UI")]
        public Canvas menuCanvas;

        [Header("Carousel Settings")]
        [Tooltip("Distance from camera to place wheel")]
        public float wheelDistance = 2.0f;

        [Tooltip("Size of the icon display")]
        public float iconSize = 1f;

        private List<TableScanner.TableInfo> tables = new List<TableScanner.TableInfo>();
        private int currentIndex = 0;
        private TableScanner tableScanner;
        private TableLauncher tableLauncher;
        private Keyboard keyboard;

        private string instanceId;
        private Color flashColor = Color.white;

        void Awake()
        {
            instanceId = System.Guid.NewGuid().ToString().Substring(0, 8);
            keyboard = Keyboard.current;
        }

        void Start()
        {
            // Enable running in background so we get input even when window not focused
            Application.runInBackground = true;

            // Get or create components
            tableScanner = FindFirstObjectByType<TableScanner>();
            if (tableScanner == null)
            {
                tableScanner = gameObject.AddComponent<TableScanner>();
            }

            tableLauncher = FindFirstObjectByType<TableLauncher>();
            if (tableLauncher == null)
            {
                tableLauncher = gameObject.AddComponent<TableLauncher>();
            }

            // Add VR controller input component
            var vrControllerInput = FindFirstObjectByType<VRControllerInput>();
            if (vrControllerInput == null)
            {
                vrControllerInput = gameObject.AddComponent<VRControllerInput>();
                UnityEngine.Debug.Log("VRControllerInput component added for VR controller support");
            }

            // Subscribe to table exit event
            tableLauncher.OnTableExited += OnTableExited;

            // Ensure dispatcher exists
            var dispatcher = UnityMainThreadDispatcher.Instance;

            // Position menu
            PositionMenu();

            // Load tables
            LoadTables();

            // Set controls text
            if (controlsText != null)
            {
                controlsText.text = "VR: Triggers to browse, A to launch | KB: Shift/Arrows to browse, Enter/Space to launch";
            }
        }

        void PositionMenu()
        {
            if (menuCanvas == null || Camera.main == null)
            {
                return;
            }

            Vector3 menuPosition = Camera.main.transform.position +
                                  Camera.main.transform.forward * wheelDistance;
            menuCanvas.transform.position = menuPosition;
            menuCanvas.transform.rotation = Quaternion.LookRotation(
                menuCanvas.transform.position - Camera.main.transform.position
            );
        }

        void LoadTables()
        {
            var scannedTables = tableScanner.ScanForTables();
            tables = new List<TableScanner.TableInfo>(scannedTables);

            if (tables.Count > 0)
            {
                currentIndex = 0;
                UpdateDisplay();
            }
            else
            {
                if (tableNameText != null)
                {
                    tableNameText.text = "No tables found";
                }
            }
        }

        void Update()
        {
            // Check if tables got cleared
            if (tables == null)
            {
                tables = new List<TableScanner.TableInfo>();
            }

            // On first update, verify tables are still loaded
            if (Time.frameCount == 1)
            {
                if (tables.Count == 0)
                {
                    LoadTables();
                }
            }

            // Handle input
            HandleInput();

            // Exit on Escape - but only when no table is running
            bool escapePressed = (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;
            if (escapePressed && !escapeWasPressed)
            {
                escapeWasPressed = true;
                if (tableLauncher == null || !tableLauncher.IsTableRunning())
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                }
            }
            else if (!escapePressed)
            {
                escapeWasPressed = false;
            }
        }

        void HandleInput()
        {
            if (tables.Count == 0)
            {
                return;
            }

            // Don't handle input if a table is running - let VRControllerInput handle it
            if (tableLauncher != null && tableLauncher.IsTableRunning())
            {
                return;
            }

            // Left Shift - Previous Table
            bool leftShiftPressed = (GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0;
            if (leftShiftPressed && !leftShiftWasPressed)
            {
                leftShiftWasPressed = true;
                PreviousTable();
            }
            else if (!leftShiftPressed)
            {
                leftShiftWasPressed = false;
            }

            // Right Shift - Next Table
            bool rightShiftPressed = (GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0;
            if (rightShiftPressed && !rightShiftWasPressed)
            {
                rightShiftWasPressed = true;
                NextTable();
            }
            else if (!rightShiftPressed)
            {
                rightShiftWasPressed = false;
            }

            // Left Arrow - Previous Table
            bool leftPressed = (GetAsyncKeyState(VK_LEFT) & 0x8000) != 0;
            if (leftPressed && !leftWasPressed)
            {
                leftWasPressed = true;
                PreviousTable();
            }
            else if (!leftPressed)
            {
                leftWasPressed = false;
            }

            // Right Arrow - Next Table
            bool rightPressed = (GetAsyncKeyState(VK_RIGHT) & 0x8000) != 0;
            if (rightPressed && !rightWasPressed)
            {
                rightWasPressed = true;
                NextTable();
            }
            else if (!rightPressed)
            {
                rightWasPressed = false;
            }

            // Return/Enter - Launch Table
            bool returnPressed = (GetAsyncKeyState(VK_RETURN) & 0x8000) != 0;
            if (returnPressed && !returnWasPressed)
            {
                returnWasPressed = true;
                LaunchCurrentTable();
            }
            else if (!returnPressed)
            {
                returnWasPressed = false;
            }

            // Space - Launch Table
            bool spacePressed = (GetAsyncKeyState(VK_SPACE) & 0x8000) != 0;
            if (spacePressed && !spaceWasPressed)
            {
                spaceWasPressed = true;
                LaunchCurrentTable();
            }
            else if (!spacePressed)
            {
                spaceWasPressed = false;
            }

            // VR controller triggers / buttons (XR Input System)
            HandleVRControllerInput();

            // Visual feedback - flash background when keys pressed
            if (tableIcon != null)
            {
                // Fade flash color back to white
                flashColor = Color.Lerp(flashColor, Color.white, Time.deltaTime * 5f);
                tableIcon.color = flashColor;
            }
        }

        /// <summary>
        /// Reads VR controller triggers/buttons via the XR Input System.
        /// Triggers use hysteresis (press above TriggerPressThreshold, release
        /// below TriggerReleaseThreshold) so each squeeze advances exactly once.
        /// </summary>
        void HandleVRControllerInput()
        {
            EnsureXRControllers();

            // Right trigger - Next Table
            UpdateTriggerNav(rightXRController, ref xrRightTriggerWasPressed, NextTable);

            // Left trigger - Previous Table
            UpdateTriggerNav(leftXRController, ref xrLeftTriggerWasPressed, PreviousTable);

            // Primary button (A) - Launch Table
            if (rightXRController.isValid &&
                rightXRController.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool launchPressed))
            {
                if (launchPressed && !xrLaunchWasPressed)
                {
                    xrLaunchWasPressed = true;
                    LaunchCurrentTable();
                }
                else if (!launchPressed)
                {
                    xrLaunchWasPressed = false;
                }
            }
        }

        /// <summary>
        /// Fires the given navigation action once per trigger squeeze, requiring
        /// the analog trigger to drop below the release threshold before re-firing.
        /// </summary>
        void UpdateTriggerNav(XRInputDevice device, ref bool wasPressed, System.Action navAction)
        {
            if (!device.isValid)
            {
                return;
            }

            if (!device.TryGetFeatureValue(XRCommonUsages.trigger, out float triggerValue))
            {
                return;
            }

            if (!wasPressed && triggerValue > TriggerPressThreshold)
            {
                wasPressed = true;
                navAction();
            }
            else if (wasPressed && triggerValue < TriggerReleaseThreshold)
            {
                wasPressed = false;
            }
        }

        /// <summary>
        /// (Re)acquires the left/right XR controllers if they aren't currently valid.
        /// Controllers can connect after Start or drop out and reconnect.
        /// </summary>
        void EnsureXRControllers()
        {
            if (!leftXRController.isValid)
            {
                var devices = new List<XRInputDevice>();
                InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
                if (devices.Count > 0)
                {
                    leftXRController = devices[0];
                }
            }

            if (!rightXRController.isValid)
            {
                var devices = new List<XRInputDevice>();
                InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
                if (devices.Count > 0)
                {
                    rightXRController = devices[0];
                }
            }
        }

        void PreviousTable()
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = tables.Count - 1;
            }
            UpdateDisplay();
        }

        void NextTable()
        {
            currentIndex++;
            if (currentIndex >= tables.Count)
            {
                currentIndex = 0;
            }
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (tables.Count == 0)
            {
                return;
            }

            TableScanner.TableInfo currentTable = tables[currentIndex];

            // Update text
            if (tableNameText != null)
            {
                tableNameText.text = $"{currentTable.Name}\n({currentIndex + 1}/{tables.Count})";
            }

            // Update icon
            if (tableIcon != null)
            {
                // Try to load wheel image
                if (!string.IsNullOrEmpty(currentTable.WheelImagePath) && File.Exists(currentTable.WheelImagePath))
                {
                    LoadWheelImage(currentTable.WheelImagePath);
                }
                else
                {
                    // Fallback to placeholder color if no image
                    Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
                    Color newColor = colors[currentIndex % colors.Length];
                    tableIcon.color = newColor;
                    tableIcon.sprite = null;
                }
            }
        }

        /// <summary>
        /// Loads a wheel image from disk and displays it
        /// </summary>
        void LoadWheelImage(string imagePath)
        {
            try
            {
                byte[] imageData = File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(imageData))
                {
                    // Create sprite from texture
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );

                    tableIcon.sprite = sprite;
                    tableIcon.color = Color.white; // Reset color to white to show image properly
                }
            }
            catch (System.Exception)
            {
                // Failed to load image, will use fallback color
            }
        }

        void LaunchCurrentTable()
        {
            if (tables.Count == 0)
            {
                return;
            }

            TableScanner.TableInfo table = tables[currentIndex];
            bool success = tableLauncher.LaunchTable(table.FullPath);

            if (success)
            {
                // Hide menu while table is running
                if (menuCanvas != null)
                {
                    menuCanvas.gameObject.SetActive(false);
                }
            }
        }

        void OnTableExited()
        {
            // Show menu again
            if (menuCanvas != null)
            {
                menuCanvas.gameObject.SetActive(true);
            }

            // Reposition menu in front of current camera view
            PositionMenu();
        }

        void OnDestroy()
        {
            if (tableLauncher != null)
            {
                tableLauncher.OnTableExited -= OnTableExited;
            }
        }
    }
}
