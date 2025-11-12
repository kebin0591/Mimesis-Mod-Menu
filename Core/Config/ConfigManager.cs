using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mimesis_Mod_Menu.Core.Config
{
    public class ConfigManager
    {
        private static readonly string ConfigDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "Mimesis");
        private static readonly string MainConfigPath = Path.Combine(ConfigDirectory, "config.cfg");
        private static readonly string HotkeysConfigPath = Path.Combine(ConfigDirectory, "hotkeys.cfg");

        private Dictionary<string, string> mainConfig = new Dictionary<string, string>();
        private Dictionary<string, HotkeyConfig> hotkeyConfig = new Dictionary<string, HotkeyConfig>();

        public ConfigManager()
        {
            EnsureDirectoryExists();
            LoadAllConfigs();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);
        }

        public void LoadAllConfigs()
        {
            LoadMainConfig();
            LoadHotkeysConfig();
        }

        private void LoadMainConfig()
        {
            mainConfig.Clear();
            if (!File.Exists(MainConfigPath))
            {
                MelonLogger.Msg("No main config found, creating defaults");
                SaveMainConfig();
                return;
            }

            try
            {
                foreach (string line in File.ReadAllLines(MainConfigPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                        mainConfig[parts[0].Trim()] = parts[1].Trim();
                }

                MelonLogger.Msg("Loaded main configuration");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading main config: {ex.Message}");
            }
        }

        private void LoadHotkeysConfig()
        {
            hotkeyConfig.Clear();
            if (!File.Exists(HotkeysConfigPath))
            {
                InitializeDefaultHotkeys();
                SaveHotkeysConfig();
                return;
            }

            try
            {
                foreach (string line in File.ReadAllLines(HotkeysConfigPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        hotkeyConfig[key] = HotkeyConfig.Parse(parts[1].Trim());
                    }
                }

                MelonLogger.Msg("Loaded hotkey configuration");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading hotkeys config: {ex.Message}");
            }
        }

        private void InitializeDefaultHotkeys()
        {
            hotkeyConfig["ToggleMenu"] = new HotkeyConfig(KeyCode.Insert);
            hotkeyConfig["ToggleGodMode"] = new HotkeyConfig(KeyCode.G, ctrl: true);
            hotkeyConfig["ToggleInfiniteStamina"] = new HotkeyConfig(KeyCode.S, ctrl: true);
            hotkeyConfig["ToggleNoFallDamage"] = new HotkeyConfig(KeyCode.F, ctrl: true);
            hotkeyConfig["ToggleSpeedBoost"] = new HotkeyConfig(KeyCode.D, ctrl: true);
            hotkeyConfig["ToggleESP"] = new HotkeyConfig(KeyCode.E, ctrl: true);
            hotkeyConfig["ToggleAutoLoot"] = new HotkeyConfig(KeyCode.L, ctrl: true);
            hotkeyConfig["ToggleFullbright"] = new HotkeyConfig(KeyCode.B, ctrl: true);
        }

        public void SaveMainConfig()
        {
            try
            {
                List<string> lines = new List<string> { "# Mimesis Main Configuration", "# Auto-generated, do not edit manually unless you know what you're doing", "" };

                foreach (var kvp in mainConfig.OrderBy(x => x.Key))
                    lines.Add($"{kvp.Key}={kvp.Value}");

                File.WriteAllLines(MainConfigPath, lines);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error saving main config: {ex.Message}");
            }
        }

        public void SaveHotkeysConfig()
        {
            try
            {
                List<string> lines = new List<string> { "# Mimesis Hotkey Configuration", "# Format: Feature=Ctrl+Shift+Alt+Key", "# Remove modifiers as needed (e.g., just 'F' for F key)", "" };

                foreach (var kvp in hotkeyConfig.OrderBy(x => x.Key))
                    lines.Add($"{kvp.Key}={kvp.Value}");

                File.WriteAllLines(HotkeysConfigPath, lines);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error saving hotkeys config: {ex.Message}");
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            return mainConfig.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (mainConfig.TryGetValue(key, out var value))
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (mainConfig.TryGetValue(key, out var value) && float.TryParse(value, out var result))
                return result;
            return defaultValue;
        }

        public void SetString(string key, string value)
        {
            mainConfig[key] = value;
        }

        public void SetBool(string key, bool value)
        {
            mainConfig[key] = value.ToString().ToLower();
        }

        public void SetFloat(string key, float value)
        {
            mainConfig[key] = value.ToString();
        }

        public HotkeyConfig GetHotkey(string feature)
        {
            return hotkeyConfig.TryGetValue(feature, out var hotkey) ? hotkey : new HotkeyConfig();
        }

        public void SetHotkey(string feature, HotkeyConfig hotkey)
        {
            hotkeyConfig[feature] = hotkey;
        }

        public bool IsHotkeyPressed(string feature)
        {
            return GetHotkey(feature).IsPressed();
        }

        public Dictionary<string, HotkeyConfig> GetAllHotkeys()
        {
            return new Dictionary<string, HotkeyConfig>(hotkeyConfig);
        }
    }
}
