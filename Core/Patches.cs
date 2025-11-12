using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Mimesis_Mod_Menu.Core.Config;
using Mimic;
using Mimic.Actors;
using MimicAPI.GameAPI;
using ReluProtocol;
using ReluProtocol.Enum;

namespace Mimesis_Mod_Menu.Core
{
    public static class Patches
    {
        #region Configuration

        public static bool durabilityPatchEnabled = false;
        public static bool pricePatchEnabled = false;
        public static bool gaugePatchEnabled = false;
        public static bool forceBuyEnabled = false;
        public static bool forceRepairEnabled = false;

        private static ConfigManager? configManager;

        #endregion

        #region Initialization

        public static void ApplyPatches(ConfigManager? config)
        {
            try
            {
                configManager = config;
                LoadConfig();
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.Mimesis.modmenu");
                harmony.PatchAll(typeof(Patches).Assembly);
                MelonLogger.Msg("Harmony patches applied successfully");
                SaveConfig();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error applying patches: {ex.Message}");
            }
        }

        #endregion

        #region Configuration Management

        private static void LoadConfig()
        {
            if (configManager == null)
                return;

            try
            {
                durabilityPatchEnabled = configManager.GetBool("durabilityPatchEnabled", false);
                pricePatchEnabled = configManager.GetBool("pricePatchEnabled", false);
                gaugePatchEnabled = configManager.GetBool("gaugePatchEnabled", false);
                forceBuyEnabled = configManager.GetBool("forceBuyEnabled", false);
                forceRepairEnabled = configManager.GetBool("forceRepairEnabled", false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading patch config: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            if (configManager == null)
                return;

            try
            {
                configManager.SetBool("durabilityPatchEnabled", durabilityPatchEnabled);
                configManager.SetBool("pricePatchEnabled", pricePatchEnabled);
                configManager.SetBool("gaugePatchEnabled", gaugePatchEnabled);
                configManager.SetBool("forceBuyEnabled", forceBuyEnabled);
                configManager.SetBool("forceRepairEnabled", forceRepairEnabled);

                configManager.SaveMainConfig();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error saving patch config: {ex.Message}");
            }
        }

        public static void SaveConfigAll() => SaveConfig();

        #endregion

        #region Reflection Utilities

        private static readonly BindingFlags AllFieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

        private static readonly string[] NumericFieldPatterns = new[] { "<{0}>k__BackingField", "{0}", "_{0}", "m_{0}" };

        public static void SetIntField(object instance, string fieldName, int value)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
                return;

            Type type = instance.GetType();

            foreach (string pattern in NumericFieldPatterns)
            {
                string actualFieldName = string.Format(pattern, fieldName);
                FieldInfo field = type.GetField(actualFieldName, AllFieldFlags);

                if (field != null && IsNumericType(field.FieldType))
                {
                    try
                    {
                        field.SetValue(instance, Convert.ChangeType(value, field.FieldType));
                        return;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"Failed to set field {fieldName}: {ex.Message}");
                    }
                }
            }
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte);
        }

        #endregion
    }

    #region Item Patches

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
            catch (Exception ex)
            {
                MelonLogger.Warning($"ItemInfoConstructorPatch error: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                MelonLogger.Warning($"InventoryItemUpdateInfoPatch error: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                MelonLogger.Warning($"EquipmentItemElementConstructorPatch error: {ex.Message}");
            }
        }
    }

    #endregion

    #region Player Stat Patches

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
                    ProtoActor localPlayer = PlayerAPI.GetLocalPlayer();
                    if (localPlayer != null && localPlayer.ActorID == vplayer.ObjectID)
                        return false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"StatManagerOnDamagedPatch error: {ex.Message}");
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), "ConsumeStamina")]
    internal static class StatManagerConsumeStaminaPatch
    {
        private static bool Prefix()
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

    #endregion

    #region Maintenance Room Patches

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleBuyItem))]
    internal static class VPlayerHandleBuyItemPatch
    {
        private static bool Prefix(VPlayer __instance, int itemMasterID, int hashCode, int machineIndex, ref MsgErrorCode __result)
        {
            if (!Patches.forceBuyEnabled)
                return true;

            try
            {
                MaintenanceRoom maintenanceRoom = __instance.VRoom as MaintenanceRoom;
                if (maintenanceRoom == null)
                {
                    __result = MsgErrorCode.InvalidRoomType;
                    return false;
                }

                ItemElement itemElement = maintenanceRoom.GetNewItemElement(itemMasterID, false, 1, 0, 0);
                if (itemElement == null)
                {
                    __result = MsgErrorCode.ItemNotFound;
                    return false;
                }

                __instance.InventoryControlUnit.HandleAddItem(itemElement, out _, true, true);

                __instance.SendToMe(new BuyItemRes(hashCode) { remainCurrency = maintenanceRoom.Currency });

                __instance.SendInSight(new BuyItemSig { itemMasterID = itemMasterID, machineIndex = machineIndex }, false);

                __result = MsgErrorCode.Success;
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Force buy patch error: {ex.Message}");
                __result = MsgErrorCode.InvalidErrorCode;
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleRepairTrain))]
    internal static class VPlayerHandleRepairTrainPatch
    {
        private static bool Prefix(VPlayer __instance, int hashCode, ref MsgErrorCode __result)
        {
            if (!Patches.forceRepairEnabled)
                return true;

            try
            {
                MaintenanceRoom maintenanceRoom = __instance.VRoom as MaintenanceRoom;
                if (maintenanceRoom == null)
                {
                    __result = MsgErrorCode.InvalidRoomType;
                    return false;
                }

                __instance.SendToMe(new RepairTramRes(hashCode) { errorCode = MsgErrorCode.Success });

                __instance.SendToChannel(new StartRepairTramSig { remainCurrency = maintenanceRoom.Currency });

                __result = MsgErrorCode.Success;
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Force repair patch error: {ex.Message}");
                __result = MsgErrorCode.InvalidErrorCode;
                return false;
            }
        }
    }

    #endregion
}
