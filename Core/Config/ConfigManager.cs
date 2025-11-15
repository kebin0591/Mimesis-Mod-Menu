using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mimesis_Mod_Menu.Core.Config
{
    public class ConfigManager
    {
        private const string MAIN_CATEGORY = "Mod Menu";
        private const string HOTKEYS_CATEGORY = "Mod Menu Hotkeys";

        private MelonPreferences_Category mainCategory;
        private MelonPreferences_Category hotkeysCategory;

        private Dictionary<string, MelonPreferences_Entry<string>> stringEntries = new Dictionary<string, MelonPreferences_Entry<string>>();
        private Dictionary<string, MelonPreferences_Entry<bool>> boolEntries = new Dictionary<string, MelonPreferences_Entry<bool>>();
        private Dictionary<string, MelonPreferences_Entry<float>> floatEntries = new Dictionary<string, MelonPreferences_Entry<float>>();
        private Dictionary<string, MelonPreferences_Entry<string>> hotkeyEntries = new Dictionary<string, MelonPreferences_Entry<string>>();

        public ConfigManager()
        {
            mainCategory = MelonPreferences.CreateCategory(MAIN_CATEGORY, "Mod Menu Configuration");
            hotkeysCategory = MelonPreferences.CreateCategory(HOTKEYS_CATEGORY, "Mod Menu Hotkey Configuration");
            InitializeDefaultHotkeys();
        }

        public void LoadAllConfigs()
        {
            InitializeDefaultHotkeys();
        }

        private void InitializeDefaultHotkeys()
        {
            var defaults = new Dictionary<string, HotkeyConfig>
            {
                { "ToggleMenu", new HotkeyConfig(KeyCode.Insert) },
                { "ToggleGodMode", new HotkeyConfig(KeyCode.G, ctrl: true) },
                { "ToggleInfiniteStamina", new HotkeyConfig(KeyCode.S, ctrl: true) },
                { "ToggleNoFallDamage", new HotkeyConfig(KeyCode.F, ctrl: true) },
                { "ToggleSpeedBoost", new HotkeyConfig(KeyCode.D, ctrl: true) },
                { "ToggleESP", new HotkeyConfig(KeyCode.E, ctrl: true) },
                { "ToggleAutoLoot", new HotkeyConfig(KeyCode.L, ctrl: true) },
                { "ToggleFullbright", new HotkeyConfig(KeyCode.B, ctrl: true) },
            };

            foreach (var kvp in defaults)
            {
                if (!hotkeyEntries.ContainsKey(kvp.Key))
                    SetHotkey(kvp.Key, kvp.Value);
            }

            MelonPreferences.Save();
        }

        public string GetString(string key, string defaultValue = "")
        {
            return GetOrCreate(stringEntries, key, defaultValue, () => mainCategory.CreateEntry(key, defaultValue, key, ""));
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return GetOrCreate(boolEntries, key, defaultValue, () => mainCategory.CreateEntry(key, defaultValue, key, ""));
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return GetOrCreate(floatEntries, key, defaultValue, () => mainCategory.CreateEntry(key, defaultValue, key, ""));
        }

        private T GetOrCreate<T>(Dictionary<string, MelonPreferences_Entry<T>> dict, string key, T defaultValue, Func<MelonPreferences_Entry<T>> factory)
        {
            try
            {
                if (!dict.TryGetValue(key, out var entry))
                {
                    entry = factory();
                    dict[key] = entry;
                }
                return entry.Value;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error getting {typeof(T).Name} {key}: {ex.Message}");
                return defaultValue;
            }
        }

        public void SetString(string key, string value)
        {
            SetValue(stringEntries, key, value, () => mainCategory.CreateEntry(key, value, key, ""));
        }

        public void SetBool(string key, bool value)
        {
            SetValue(boolEntries, key, value, () => mainCategory.CreateEntry(key, value, key, ""));
        }

        public void SetFloat(string key, float value)
        {
            SetValue(floatEntries, key, value, () => mainCategory.CreateEntry(key, value, key, ""));
        }

        private void SetValue<T>(Dictionary<string, MelonPreferences_Entry<T>> dict, string key, T value, Func<MelonPreferences_Entry<T>> factory)
        {
            try
            {
                if (!dict.TryGetValue(key, out var entry))
                {
                    entry = factory();
                    dict[key] = entry;
                }
                else
                {
                    entry.Value = value;
                }
                MelonPreferences.Save();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error setting {typeof(T).Name} {key}: {ex.Message}");
            }
        }

        public HotkeyConfig GetHotkey(string feature)
        {
            try
            {
                if (!hotkeyEntries.TryGetValue(feature, out var entry))
                {
                    entry = hotkeysCategory.CreateEntry(feature, "None", feature, "");
                    hotkeyEntries[feature] = entry;
                }
                return HotkeyConfig.Parse(entry.Value);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error getting hotkey {feature}: {ex.Message}");
                return new HotkeyConfig();
            }
        }

        public void SetHotkey(string feature, HotkeyConfig hotkey)
        {
            try
            {
                if (!hotkeyEntries.TryGetValue(feature, out var entry))
                {
                    entry = hotkeysCategory.CreateEntry(feature, hotkey.ToString(), feature, "");
                    hotkeyEntries[feature] = entry;
                }
                else
                {
                    entry.Value = hotkey.ToString();
                }
                MelonPreferences.Save();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error setting hotkey {feature}: {ex.Message}");
            }
        }

        public bool IsHotkeyPressed(string feature)
        {
            return GetHotkey(feature).IsPressed();
        }

        public Dictionary<string, HotkeyConfig> GetAllHotkeys()
        {
            var result = new Dictionary<string, HotkeyConfig>();
            try
            {
                foreach (var entry in hotkeysCategory.Entries)
                {
                    if (entry is MelonPreferences_Entry<string> stringEntry)
                    {
                        result[entry.Identifier] = HotkeyConfig.Parse(stringEntry.Value);
                        hotkeyEntries[entry.Identifier] = stringEntry;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error getting all hotkeys: {ex.Message}");
            }
            return result;
        }
    }
}
