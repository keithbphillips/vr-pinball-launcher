using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace VRLauncher
{
    /// <summary>
    /// Diagnostic tool to map VR controller buttons to joystick button numbers
    /// Displays which buttons are pressed and their joystick IDs
    /// </summary>
    public class VRJoystickMapper : MonoBehaviour
    {
        [Header("Display Settings")]
        [Tooltip("Enable to show button mapping diagnostics on screen")]
        public bool showDiagnostics = true;

        [Tooltip("Font size for diagnostic text")]
        public int fontSize = 16;

        private XRInputDevice leftController;
        private XRInputDevice rightController;

        private StringBuilder displayText = new StringBuilder();
        private GUIStyle textStyle;

        // Track all possible button states
        private Dictionary<string, bool> buttonStates = new Dictionary<string, bool>();

        void Start()
        {
            InitializeControllers();
            InitializeButtonTracking();
        }

        void Update()
        {
            if (!showDiagnostics)
                return;

            // Re-initialize controllers if needed
            if (!leftController.isValid || !rightController.isValid)
            {
                InitializeControllers();
            }

            CheckAllInputs();
        }

        private void InitializeControllers()
        {
            var leftHandDevices = new List<XRInputDevice>();
            var rightHandDevices = new List<XRInputDevice>();

            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

            if (leftHandDevices.Count > 0)
            {
                leftController = leftHandDevices[0];
                UnityEngine.Debug.Log($"Left Controller: {leftController.name}");
            }

            if (rightHandDevices.Count > 0)
            {
                rightController = rightHandDevices[0];
                UnityEngine.Debug.Log($"Right Controller: {rightController.name}");
            }

            // Also log all connected joysticks
            string[] joystickNames = Input.GetJoystickNames();
            for (int i = 0; i < joystickNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(joystickNames[i]))
                {
                    UnityEngine.Debug.Log($"Joystick {i}: {joystickNames[i]}");
                }
            }
        }

        private void InitializeButtonTracking()
        {
            // Initialize all button states to false
            buttonStates["L_Trigger"] = false;
            buttonStates["L_Grip"] = false;
            buttonStates["L_Primary"] = false;
            buttonStates["L_Secondary"] = false;
            buttonStates["L_Menu"] = false;
            buttonStates["L_Joystick_Click"] = false;

            buttonStates["R_Trigger"] = false;
            buttonStates["R_Grip"] = false;
            buttonStates["R_Primary"] = false;
            buttonStates["R_Secondary"] = false;
            buttonStates["R_Menu"] = false;
            buttonStates["R_Joystick_Click"] = false;
        }

        private void CheckAllInputs()
        {
            displayText.Clear();
            displayText.AppendLine("=== VR CONTROLLER TO JOYSTICK MAPPER ===\n");

            // Check Unity's joystick buttons (0-19)
            displayText.AppendLine("UNITY JOYSTICK BUTTONS PRESSED:");
            bool anyJoystickButton = false;
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKey(KeyCode.JoystickButton0 + i))
                {
                    displayText.AppendLine($"  Joystick Button {i} (KeyCode.JoystickButton{i})");
                    anyJoystickButton = true;
                }
            }
            if (!anyJoystickButton)
            {
                displayText.AppendLine("  (none)");
            }

            displayText.AppendLine("\nVR CONTROLLER BUTTONS:");

            // Left Controller
            if (leftController.isValid)
            {
                displayText.AppendLine($"\nLEFT ({leftController.name}):");
                CheckButton(leftController, "L_Trigger", CommonUsages.trigger, true);
                CheckButton(leftController, "L_Grip", CommonUsages.grip, true);
                CheckButton(leftController, "L_Primary", CommonUsages.primaryButton, false);
                CheckButton(leftController, "L_Secondary", CommonUsages.secondaryButton, false);
                CheckButton(leftController, "L_Menu", CommonUsages.menuButton, false);
                CheckButton(leftController, "L_Joystick_Click", CommonUsages.primary2DAxisClick, false);

                // Check joystick/touchpad
                if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftJoystick))
                {
                    if (leftJoystick.magnitude > 0.1f)
                    {
                        displayText.AppendLine($"  Joystick: ({leftJoystick.x:F2}, {leftJoystick.y:F2})");
                    }
                }
            }
            else
            {
                displayText.AppendLine("\nLEFT: (not connected)");
            }

            // Right Controller
            if (rightController.isValid)
            {
                displayText.AppendLine($"\nRIGHT ({rightController.name}):");
                CheckButton(rightController, "R_Trigger", CommonUsages.trigger, true);
                CheckButton(rightController, "R_Grip", CommonUsages.grip, true);
                CheckButton(rightController, "R_Primary", CommonUsages.primaryButton, false);
                CheckButton(rightController, "R_Secondary", CommonUsages.secondaryButton, false);
                CheckButton(rightController, "R_Menu", CommonUsages.menuButton, false);
                CheckButton(rightController, "R_Joystick_Click", CommonUsages.primary2DAxisClick, false);

                // Check joystick/touchpad
                if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightJoystick))
                {
                    if (rightJoystick.magnitude > 0.1f)
                    {
                        displayText.AppendLine($"  Joystick: ({rightJoystick.x:F2}, {rightJoystick.y:F2})");
                    }
                }
            }
            else
            {
                displayText.AppendLine("\nRIGHT: (not connected)");
            }

            // Instructions
            displayText.AppendLine("\n=== INSTRUCTIONS ===");
            displayText.AppendLine("Press each VR controller button and note:");
            displayText.AppendLine("1. Which button name shows up above");
            displayText.AppendLine("2. Which Joystick Button number appears");
            displayText.AppendLine("\nThen configure VPinballX to use those");
            displayText.AppendLine("joystick button numbers for each action.");
            displayText.AppendLine("\nPress 'M' key to toggle this display");
        }

        private void CheckButton(XRInputDevice device, string name, InputFeatureUsage<float> usage, bool isAnalog)
        {
            if (device.TryGetFeatureValue(usage, out float value))
            {
                bool isPressed = value > 0.5f;
                if (isPressed)
                {
                    string valueStr = isAnalog ? $" ({value:F2})" : "";
                    displayText.AppendLine($"  {name}: PRESSED{valueStr}");
                }

                // Log on state change
                if (buttonStates.ContainsKey(name) && buttonStates[name] != isPressed)
                {
                    buttonStates[name] = isPressed;
                    if (isPressed)
                    {
                        UnityEngine.Debug.Log($"[VRJoystickMapper] {name} pressed");
                    }
                }
            }
        }

        private void CheckButton(XRInputDevice device, string name, InputFeatureUsage<bool> usage, bool isAnalog)
        {
            if (device.TryGetFeatureValue(usage, out bool value))
            {
                if (value)
                {
                    displayText.AppendLine($"  {name}: PRESSED");
                }

                // Log on state change
                if (buttonStates.ContainsKey(name) && buttonStates[name] != value)
                {
                    buttonStates[name] = value;
                    if (value)
                    {
                        UnityEngine.Debug.Log($"[VRJoystickMapper] {name} pressed");
                    }
                }
            }
        }

        void OnGUI()
        {
            if (!LauncherConfig.Instance.showDebugConsole)
                return;

            if (!showDiagnostics)
                return;

            // Handle toggle key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.M)
            {
                showDiagnostics = !showDiagnostics;
            }

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = fontSize;
                textStyle.normal.textColor = Color.white;
                textStyle.alignment = TextAnchor.UpperLeft;
            }

            // Dark background
            GUI.Box(new Rect(10, 10, 500, Screen.height - 20), "");

            // Display text
            GUI.Label(new Rect(20, 20, 480, Screen.height - 40), displayText.ToString(), textStyle);
        }
    }
}
