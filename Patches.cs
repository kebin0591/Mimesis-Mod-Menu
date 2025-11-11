using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bifrost.ConstEnum;
using Bifrost.Cooked;
using Bifrost.ItemEquipment;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Mimic;
using Mimic.Actors;
using MimicAPI.GameAPI;
using ReluProtocol;

namespace MIMESIS.Patches
{
    public static class Patches
    {
        public static bool durabilityPatchEnabled = false;
        public static bool pricePatchEnabled = false;
        public static bool gaugePatchEnabled = false;

        private static string configPath = Path.Combine(MelonEnvironment.UserDataDirectory, "MIMESIS_Patches.cfg");

        public static void ApplyPatches()
        {
            LoadConfig();
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.mimesis.modmenu");
            harmony.PatchAll(typeof(Patches).Assembly);
            MelonLogger.Msg("Harmony patches applied successfully");
            SaveConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("durabilityPatchEnabled="))
                            bool.TryParse(line.Split('=')[1], out durabilityPatchEnabled);
                        if (line.StartsWith("pricePatchEnabled="))
                            bool.TryParse(line.Split('=')[1], out pricePatchEnabled);
                        if (line.StartsWith("gaugePatchEnabled="))
                            bool.TryParse(line.Split('=')[1], out gaugePatchEnabled);
                    }
                    MelonLogger.Msg("Loaded patch config");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading config: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            try
            {
                string[] lines = new string[] { $"durabilityPatchEnabled={durabilityPatchEnabled}", $"pricePatchEnabled={pricePatchEnabled}", $"gaugePatchEnabled={gaugePatchEnabled}" };
                File.WriteAllLines(configPath, lines);
                MelonLogger.Msg("Saved patch config");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error saving config: {ex.Message}");
            }
        }

        public static void ToggleDurabilityPatch(bool value)
        {
            if (durabilityPatchEnabled != value)
            {
                durabilityPatchEnabled = value;
                SaveConfig();
            }
        }

        public static void TogglePricePatch(bool value)
        {
            if (pricePatchEnabled != value)
            {
                pricePatchEnabled = value;
                SaveConfig();
            }
        }

        public static void ToggleGaugePatch(bool value)
        {
            if (gaugePatchEnabled != value)
            {
                gaugePatchEnabled = value;
                SaveConfig();
            }
        }

        public static void SetIntField(object instance, string fieldName, int value)
        {
            if (instance == null)
                return;

            Type type = instance.GetType();

            FieldInfo field = type.GetField($"<{fieldName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null && (field.FieldType == typeof(int) || field.FieldType == typeof(long)))
            {
                field.SetValue(instance, value);
                return;
            }

            field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null && (field.FieldType == typeof(int) || field.FieldType == typeof(long)))
            {
                field.SetValue(instance, value);
                return;
            }

            field = type.GetField($"_{fieldName}", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null && (field.FieldType == typeof(int) || field.FieldType == typeof(long)))
            {
                field.SetValue(instance, value);
                return;
            }

            field = type.GetField($"m_{fieldName}", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null && (field.FieldType == typeof(int) || field.FieldType == typeof(long)))
            {
                field.SetValue(instance, value);
            }
        }
    }

    [HarmonyPatch(typeof(ItemInfo), MethodType.Constructor)]
    internal static class ItemInfoConstructorPatch
    {
        private static void Postfix(ItemInfo __instance)
        {
            try
            {
                if (Patches.durabilityPatchEnabled)
                    Patches.SetIntField(__instance, "durability", int.MaxValue);
                if (Patches.pricePatchEnabled)
                    Patches.SetIntField(__instance, "price", int.MaxValue);
                if (Patches.gaugePatchEnabled)
                    Patches.SetIntField(__instance, "remainGauge", int.MaxValue);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(InventoryItem), nameof(InventoryItem.UpdateInfo))]
    internal static class InventoryItemUpdateInfoPatch
    {
        private static void Postfix(InventoryItem __instance)
        {
            try
            {
                if (Patches.durabilityPatchEnabled)
                    Patches.SetIntField(__instance, "Durability", int.MaxValue);
                if (Patches.pricePatchEnabled)
                    Patches.SetIntField(__instance, "Price", int.MaxValue);
                if (Patches.gaugePatchEnabled)
                    Patches.SetIntField(__instance, "RemainGauge", int.MaxValue);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(EquipmentItemElement), MethodType.Constructor, new[] { typeof(int), typeof(long), typeof(bool), typeof(int), typeof(int) })]
    internal static class EquipmentItemElementConstructorPatch
    {
        private static void Postfix(EquipmentItemElement __instance)
        {
            try
            {
                if (Patches.durabilityPatchEnabled)
                    Patches.SetIntField(__instance, "RemainDurability", int.MaxValue);
                if (Patches.gaugePatchEnabled)
                    Patches.SetIntField(__instance, "RemainAmount", int.MaxValue);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(StatManager), "OnDamaged")]
    internal static class StatManagerOnDamagedPatch
    {
        private static bool Prefix(object __instance, object args)
        {
            if (!MainGUI.godModeEnabled)
                return true;
            try
            {
                object victim = ReflectionHelper.GetFieldValue(args, "Victim");
                if (victim is VPlayer vplayer)
                {
                    ProtoActor lp = PlayerAPI.GetLocalPlayer();
                    if (lp != null && lp.ActorID == vplayer.ObjectID)
                        return false;
                }
            }
            catch { }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), "ConsumeStamina")]
    internal static class StatManagerConsumeStaminaPatch
    {
        private static bool Prefix(long amount)
        {
            return !MainGUI.infiniteStaminaEnabled;
        }
    }

    [HarmonyPatch(typeof(MovementController), "CheckFallDamage")]
    internal static class MovementControllerCheckFallDamagePatch
    {
        private static bool Prefix(ref float __result)
        {
            if (!MainGUI.noFallDamageEnabled)
                return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(ProtoActor), "CaculateSpeed")]
    internal static class ProtoActorCaculateSpeedPatch
    {
        private static void Postfix(ref float __result)
        {
            if (MainGUI.speedBoostEnabled)
                __result *= MainGUI.speedBoostMultiplier;
        }
    }
}
