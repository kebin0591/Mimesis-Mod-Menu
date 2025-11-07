using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using Mimic;
using Mimic.Actors;
using ReluProtocol;
using ReluProtocol.Enum;
using shadcnui.GUIComponents.Controls;
using shadcnui.GUIComponents.Core;
using shadcnui.GUIComponents.Data;
using shadcnui.GUIComponents.Display;
using shadcnui.GUIComponents.Layout;
using UnityEngine;
using Input = UnityEngine.Input;

public class MainGUI : MonoBehaviour
{
    private GUIHelper guiHelper;
    private Rect windowRect = new Rect(20, 20, 900, 750);
    private bool showDemoWindow = true;
    private Vector2 scrollPosition;
    private int currentDemoTab = 0;
    private Tabs.TabConfig[] demoTabs;

    private static readonly List<LootingLevelObject> PickupQueue = new List<LootingLevelObject>();
    private static float pickupCooldown = 0f;
    private static bool isPickingUp = false;

    public static bool godModeEnabled = false;
    public static bool infiniteStaminaEnabled = false;
    public static bool noFallDamageEnabled = false;

    // esp
    public static bool espEnabled = false;
    public static bool espShowLoot = false;
    public static bool espShowPlayers = true;
    public static bool espShowMonsters = true;
    public static bool espShowInteractors = false;
    public static bool espShowNPCs = false;
    public static bool espShowFieldSkills = false;
    public static bool espShowProjectiles = false;
    public static bool espShowAuraSkills = false;

    public static bool speedBoostEnabled = false;

    public static Color espColor = Color.yellow;
    public static float espDistance = 100f;
    public static float speedBoostMultiplier = 2f;

    void Start()
    {
        guiHelper = new GUIHelper();
        demoTabs = new Tabs.TabConfig[] { new Tabs.TabConfig("Local Player", DrawLocalPlayerTab), new Tabs.TabConfig("Movement", DrawMovementTab), new Tabs.TabConfig("Economy", DrawEconomyTab), new Tabs.TabConfig("ESP", DrawESPTab), new Tabs.TabConfig("Inventory", DrawInventoryTab) };

        ESPMain.Initialize();
        ApplyHarmonyPatches();
    }

    void Update()
    {
        if (!isPickingUp || PickupQueue.Count == 0)
            return;

        pickupCooldown -= Time.deltaTime;
        if (pickupCooldown <= 0)
            ProcessNextPickup();
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 30), "Open Mod Menu"))
            showDemoWindow = !showDemoWindow;

        if (showDemoWindow)
            windowRect = GUI.Window(101, windowRect, DrawDemoWindow, "MIMESIS_Mod_Menu");

        if (espEnabled)
            ESPMain.UpdateESP();
    }

    void OnDestroy() => ESPMain.Cleanup();

    void DrawDemoWindow(int windowID)
    {
        guiHelper.UpdateAnimations(showDemoWindow);
        if (!guiHelper.BeginAnimatedGUI())
            return;

        currentDemoTab = guiHelper.VerticalTabs(demoTabs.Select(tab => tab.Name).ToArray(), currentDemoTab, () => scrollPosition = guiHelper.DrawScrollView(scrollPosition, DrawCurrentTabContent, GUILayout.Height(650)), maxLines: 1);

        guiHelper.EndAnimatedGUI();
        GUI.DragWindow();
    }

    void DrawCurrentTabContent()
    {
        guiHelper.BeginVerticalGroup();
        demoTabs[currentDemoTab].Content?.Invoke();
        guiHelper.EndVerticalGroup();
    }

    void DrawLocalPlayerTab()
    {
        guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

        guiHelper.Label("Protection", LabelVariant.Default);
        godModeEnabled = guiHelper.Switch("God Mode", godModeEnabled);
        noFallDamageEnabled = guiHelper.Switch("No Fall Damage", noFallDamageEnabled);
        guiHelper.HorizontalSeparator();

        guiHelper.Label("Recovery", LabelVariant.Default);
        infiniteStaminaEnabled = guiHelper.Switch("Infinite Stamina", infiniteStaminaEnabled);

        guiHelper.EndVerticalGroup();
    }

    void DrawMovementTab()
    {
        guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

        guiHelper.Label("Speed", LabelVariant.Default);
        speedBoostEnabled = guiHelper.Switch("Speed Boost", speedBoostEnabled);
        DrawSlider("Multiplier", ref speedBoostMultiplier, 1f, 5f, "x");
        guiHelper.HorizontalSeparator();

        guiHelper.Label("Navigation", LabelVariant.Default);
        if (guiHelper.Button("Teleport Forward", ButtonVariant.Default, ButtonSize.Small))
            TeleportForward(50f);
        guiHelper.MutedLabel("Teleport 50 units ahead");

        guiHelper.EndVerticalGroup();
    }

    void DrawEconomyTab()
    {
        guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));
        guiHelper.Label("Currency", LabelVariant.Default);

        if (guiHelper.Button("Add 10000 Currency [CS?]", ButtonVariant.Default, ButtonSize.Small))
            AddCurrency(10000);
        if (guiHelper.Button("Add 50000 Currency [CS?]", ButtonVariant.Default, ButtonSize.Small))
            AddCurrency(50000);

        guiHelper.EndVerticalGroup();
    }

    void DrawESPTab()
    {
        guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

        guiHelper.Label("Entity ESP Settings", LabelVariant.Default);
        guiHelper.MutedLabel("Visualize all nearby entities");
        guiHelper.HorizontalSeparator();

        espEnabled = guiHelper.Switch("Enable ESP", espEnabled);
        guiHelper.HorizontalSeparator();

        guiHelper.Label("Entity Types", LabelVariant.Default);
        espShowPlayers = guiHelper.Switch("Players", espShowPlayers);
        espShowMonsters = guiHelper.Switch("Monsters", espShowMonsters);
        espShowInteractors = guiHelper.Switch("Interactors", espShowInteractors);
        espShowNPCs = guiHelper.Switch("NPCs", espShowNPCs);
        espShowLoot = guiHelper.Switch("Loot", espShowLoot);
        espShowFieldSkills = guiHelper.Switch("Field Skills", espShowFieldSkills);
        espShowProjectiles = guiHelper.Switch("Projectiles", espShowProjectiles);
        espShowAuraSkills = guiHelper.Switch("Aura Skills", espShowAuraSkills);
        guiHelper.HorizontalSeparator();

        DrawSlider("ESP Distance", ref espDistance, 50f, 500f, "m");
        guiHelper.HorizontalSeparator();

        guiHelper.EndVerticalGroup();
    }

    void DrawInventoryTab()
    {
        guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

        guiHelper.Label("Item Management", LabelVariant.Default);
        guiHelper.MutedLabel("Manage and organize inventory items");
        guiHelper.HorizontalSeparator();

        if (guiHelper.Button(isPickingUp ? "Stop Pickup" : "Pickup All Items", isPickingUp ? ButtonVariant.Destructive : ButtonVariant.Default, ButtonSize.Small))
        {
            if (isPickingUp)
                StopPickup();
            else
                StartPickupAllItems();
        }

        guiHelper.EndVerticalGroup();
    }

    void DrawSlider(string label, ref float value, float min, float max, string suffix)
    {
        guiHelper.BeginHorizontalGroup();
        guiHelper.Label($"{label}: {value:F1}{suffix}", LabelVariant.Default);
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
        guiHelper.EndHorizontalGroup();
    }

    void StartPickupAllItems()
    {
        try
        {
            PickupQueue.Clear();
            LootingLevelObject[] allLoot = GameAPI.GetAllLoot();

            if (allLoot.Length > 0)
            {
                PickupQueue.AddRange(allLoot);
                isPickingUp = true;
                pickupCooldown = 0.05f;
                MelonLogger.Msg($"Starting to pickup {PickupQueue.Count} items");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"StartPickupAllItems error: {ex.Message}");
        }
    }

    void ProcessNextPickup()
    {
        if (PickupQueue.Count == 0)
        {
            isPickingUp = false;
            MelonLogger.Msg("Pickup complete");
            return;
        }

        try
        {
            ProtoActor player = GameAPI.GetLocalPlayer();
            if (player == null)
            {
                StopPickup();
                return;
            }

            LootingLevelObject loot = PickupQueue[0];
            if (loot != null && loot.gameObject.activeInHierarchy)
                player.GrapLootingObject(loot.ActorID);

            PickupQueue.RemoveAt(0);
            pickupCooldown = 0.05f;
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Error picking up item: {ex.Message}");
            if (PickupQueue.Count > 0)
                PickupQueue.RemoveAt(0);
            pickupCooldown = 0.05f;
        }
    }

    void StopPickup()
    {
        isPickingUp = false;
        PickupQueue.Clear();
        MelonLogger.Msg("Pickup stopped");
    }

    void TeleportForward(float distance)
    {
        try
        {
            ProtoActor player = GameAPI.GetLocalPlayer();
            if (player == null)
                return;

            Vector3 newPos = player.transform.position + player.transform.forward * distance;
            player.Teleport(newPos, player.transform.eulerAngles, false);
            MelonLogger.Msg("Teleported forward");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"TeleportForward error: {ex.Message}");
        }
    }

    void AddCurrency(int amount)
    {
        try
        {
            Hub.PersistentData pdata = GameAPI.GetPersistentData();
            if (pdata == null)
                return;

            GameMainBase gameMain = pdata.main;
            gameMain.UpdateCurrency(gameMain.CurrentCurrency + amount);
            MelonLogger.Msg($"Added {amount} currency");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"AddCurrency error: {ex.Message}");
        }
    }

    void ApplyHarmonyPatches()
    {
        var harmony = new HarmonyLib.Harmony("com.mod.patches");

        PatchMethod(harmony, typeof(StatManager), "OnDamaged", nameof(PrefixOnDamaged));
        PatchMethod(harmony, typeof(StatManager), "ConsumeStamina", nameof(PrefixConsumeStamina));
        PatchMethod(harmony, typeof(MovementController), "CheckFallDamage", nameof(PrefixCheckFallDamage));
        PatchMethod(harmony, typeof(ProtoActor), "CaculateSpeed", nameof(PostfixCaculateSpeed), true);
    }

    void PatchMethod(HarmonyLib.Harmony harmony, Type type, string methodName, string patchName, bool isPostfix = false)
    {
        var method = AccessTools.Method(type, methodName);
        if (method == null)
            return;

        var patchMethod = new HarmonyMethod(typeof(MainGUI), patchName);
        if (isPostfix)
            harmony.Patch(method, null, patchMethod);
        else
            harmony.Patch(method, patchMethod);
    }

    static bool PrefixOnDamaged(object __instance, object args)
    {
        if (!godModeEnabled)
            return true;

        object victim = ModHelper.GetFieldValue(args, "Victim");
        return !(victim is VPlayer);
    }

    static bool PrefixConsumeStamina(long amount) => !infiniteStaminaEnabled;

    static bool PrefixCheckFallDamage(ref float __result)
    {
        if (!noFallDamageEnabled)
            return true;

        __result = 0f;
        return false;
    }

    static void PostfixCaculateSpeed(ref float __result)
    {
        if (speedBoostEnabled)
            __result *= speedBoostMultiplier;
    }
}
