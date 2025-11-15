using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mimesis_Mod_Menu.Core.Config
{
    public class HotkeyConfig
    {
        public KeyCode Key { get; set; }
        public bool Shift { get; set; }
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }

        public HotkeyConfig(KeyCode key = KeyCode.None, bool shift = false, bool ctrl = false, bool alt = false)
        {
            Key = key;
            Shift = shift;
            Ctrl = ctrl;
            Alt = alt;
        }

        public bool IsPressed()
        {
            try
            {
                if (Key == KeyCode.None)
                    return false;

                var keyboard = Keyboard.current;
                if (keyboard == null)
                    return false;

                var targetKey = keyboard.FindKeyOnCurrentKeyboardLayout(Key.ToString());
                if (targetKey?.wasPressedThisFrame != true)
                    return false;

                return CheckModifiers(keyboard);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"IsPressed error: {ex.Message}");
                return false;
            }
        }

        private bool CheckModifiers(Keyboard keyboard)
        {
            bool shiftPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            bool ctrlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            bool altPressed = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;

            return Shift == shiftPressed && Ctrl == ctrlPressed && Alt == altPressed;
        }

        public override string ToString()
        {
            try
            {
                string result = Key.ToString();
                if (Ctrl)
                    result = "Ctrl+" + result;
                if (Shift)
                    result = "Shift+" + result;
                if (Alt)
                    result = "Alt+" + result;
                return result;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"ToString error: {ex.Message}");
                return "None";
            }
        }

        public static HotkeyConfig Parse(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input) || input.Equals("None", StringComparison.OrdinalIgnoreCase))
                    return new HotkeyConfig();

                bool ctrl = input.Contains("Ctrl+");
                bool shift = input.Contains("Shift+");
                bool alt = input.Contains("Alt+");

                string keyPart = input.Replace("Ctrl+", "").Replace("Shift+", "").Replace("Alt+", "").Trim();

                if (Enum.TryParse<KeyCode>(keyPart, true, out var key))
                    return new HotkeyConfig(key, shift, ctrl, alt);

                MelonLogger.Warning($"Failed to parse hotkey: {input}");
                return new HotkeyConfig();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Parse error: {ex.Message}");
                return new HotkeyConfig();
            }
        }
    }
}
