using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VRLauncher
{
    /// <summary>
    /// Maps VR controller joystick buttons to keyboard events for VPinballX
    /// Uses Windows SendInput API to simulate keyboard presses
    /// Based on Unity's joystick button detection
    /// </summary>
    public class VRControllerInput : MonoBehaviour
    {
        #region Windows API Structures and Imports

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // Virtual key codes
        private const ushort VK_LSHIFT = 0xA0;      // Left Shift
        private const ushort VK_RSHIFT = 0xA1;      // Right Shift
        private const ushort VK_RETURN = 0x0D;      // Enter/Return
        private const ushort VK_1 = 0x31;           // 1 key
        private const ushort VK_5 = 0x35;           // 5 key
        private const ushort VK_NUMPAD2 = 0x62;     // Numpad 2
        private const ushort VK_NUMPAD5 = 0x65;     // Numpad 5
        private const ushort VK_NUMPAD8 = 0x68;     // Numpad 8

        #endregion

        [Header("Controller Button Mappings")]
        [Tooltip("Enable controller input (only active when table is running)")]
        public bool enableControllerInput = true;

        [Header("Debug")]
        [Tooltip("Log controller input events")]
        public bool debugLogging = true;

        private TableLauncher tableLauncher;

        // Track button states to detect press/release
        private bool button0Pressed = false;  // Return
        private bool button1Pressed = false;  // 5
        private bool button2Pressed = false;  // Numpad 8
        private bool button3Pressed = false;  // Numpad 2
        private bool button4Pressed = false;  // 1
        private bool button5Pressed = false;  // Numpad 5
        private bool button14Pressed = false; // Left Shift
        private bool button15Pressed = false; // Right Shift

        void Start()
        {
            tableLauncher = FindFirstObjectByType<TableLauncher>();
            if (tableLauncher == null)
            {
                UnityEngine.Debug.LogWarning("VRControllerInput: TableLauncher not found!");
            }

            UnityEngine.Debug.Log("VRControllerInput initialized with custom button mappings");
        }

        private float lastLogTime = 0f;

        void Update()
        {
            // Only process input when a table is running
            if (!enableControllerInput || tableLauncher == null || !tableLauncher.IsTableRunning())
            {
                return;
            }

            // Log every 2 seconds to confirm Update is still running
            if (debugLogging && Time.time - lastLogTime > 2f)
            {
                UnityEngine.Debug.Log($"[VRControllerInput] Update() still running at {Time.time:F1}s");
                lastLogTime = Time.time;
            }

            ProcessButtonInput();
        }

        // Add this for debugging - shows all button states every frame
        void OnGUI()
        {
            if (!LauncherConfig.Instance.showDebugConsole)
                return;

            if (!debugLogging || tableLauncher == null || !tableLauncher.IsTableRunning())
                return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;

            int y = 10;
            GUI.Label(new Rect(10, y, 400, 30), "=== VR BUTTON DEBUG ===", style);
            y += 30;

            // Check all buttons 0-19
            for (int i = 0; i < 20; i++)
            {
                KeyCode buttonCode = KeyCode.JoystickButton0 + i;
                bool isPressed = Input.GetKey(buttonCode);

                if (isPressed)
                {
                    GUI.Label(new Rect(10, y, 400, 25), $"Button {i}: PRESSED", style);
                    y += 25;
                }
            }
        }

        private void ProcessButtonInput()
        {
            // Check ALL buttons to see if ANY are being detected
            if (debugLogging)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (Input.GetKeyDown(KeyCode.JoystickButton0 + i))
                    {
                        UnityEngine.Debug.Log($"[VRControllerInput] *** DETECTED BUTTON {i} PRESS ***");
                    }
                }
            }

            // Button 15 → Right Shift (Right Flipper)
            CheckButton(KeyCode.JoystickButton15, ref button15Pressed, VK_RSHIFT, "Button 15 (Right Shift)");

            // Button 14 → Left Shift (Left Flipper)
            CheckButton(KeyCode.JoystickButton14, ref button14Pressed, VK_LSHIFT, "Button 14 (Left Shift)");

            // Button 4 → 1 key
            CheckButton(KeyCode.JoystickButton4, ref button4Pressed, VK_1, "Button 4 (1 key)");

            // Button 5 → Numpad 5
            CheckButton(KeyCode.JoystickButton5, ref button5Pressed, VK_NUMPAD5, "Button 5 (Numpad 5)");

            // Button 2 → Numpad 8
            CheckButton(KeyCode.JoystickButton2, ref button2Pressed, VK_NUMPAD8, "Button 2 (Numpad 8)");

            // Button 3 → Numpad 2
            CheckButton(KeyCode.JoystickButton3, ref button3Pressed, VK_NUMPAD2, "Button 3 (Numpad 2)");

            // Button 0 → Return key
            CheckButton(KeyCode.JoystickButton0, ref button0Pressed, VK_RETURN, "Button 0 (Return)");

            // Button 1 → 5 key
            CheckButton(KeyCode.JoystickButton1, ref button1Pressed, VK_5, "Button 1 (5 key)");
        }

        /// <summary>
        /// Checks button state and sends keyboard event if changed
        /// </summary>
        private void CheckButton(KeyCode buttonCode, ref bool wasPressed, ushort virtualKey, string debugName)
        {
            bool isPressed = Input.GetKey(buttonCode);

            if (isPressed && !wasPressed)
            {
                // Button just pressed
                SendKeyPress(virtualKey, true);
                if (debugLogging)
                {
                    UnityEngine.Debug.Log($"[VRControllerInput] {debugName} PRESSED");
                }
                wasPressed = true;
            }
            else if (!isPressed && wasPressed)
            {
                // Button just released
                SendKeyPress(virtualKey, false);
                if (debugLogging)
                {
                    UnityEngine.Debug.Log($"[VRControllerInput] {debugName} RELEASED");
                }
                wasPressed = false;
            }
        }

        /// <summary>
        /// Sends a keyboard event using Windows SendInput API
        /// </summary>
        private void SendKeyPress(ushort virtualKeyCode, bool keyDown)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = virtualKeyCode;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = keyDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            uint result = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            if (result == 0)
            {
                int error = Marshal.GetLastWin32Error();
                UnityEngine.Debug.LogWarning($"SendInput failed for key {virtualKeyCode}, Error: {error}");
            }
        }

        void OnDestroy()
        {
            // Release all keys on cleanup
            if (button0Pressed) SendKeyPress(VK_RETURN, false);
            if (button1Pressed) SendKeyPress(VK_5, false);
            if (button2Pressed) SendKeyPress(VK_NUMPAD8, false);
            if (button3Pressed) SendKeyPress(VK_NUMPAD2, false);
            if (button4Pressed) SendKeyPress(VK_1, false);
            if (button5Pressed) SendKeyPress(VK_NUMPAD5, false);
            if (button14Pressed) SendKeyPress(VK_LSHIFT, false);
            if (button15Pressed) SendKeyPress(VK_RSHIFT, false);
        }
    }
}
