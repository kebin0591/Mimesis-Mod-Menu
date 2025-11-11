using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using HarmonyLib;
using MelonLoader;
using Mimic.Actors;
using MimicAPI.GameAPI;
using shadcnui.GUIComponents.Core;
using shadcnui.GUIComponents.Layout;
using UnityEngine;
using Input = UnityEngine.Input;

namespace MIMESIS
{
    public class MainGUI : MonoBehaviour
    {
        #region Fields

        private GUIHelper guiHelper;
        private Rect windowRect = new Rect(20, 20, 900, 750);
        private bool showDemoWindow = true;
        private Vector2 scrollPosition;
        private int currentDemoTab;
        private Tabs.TabConfig[] demoTabs;

        private static readonly List<LootingLevelObject> PickupQueue = new List<LootingLevelObject>();
        private static float pickupCooldown;
        private static bool isPickingUp;

        public static bool godModeEnabled;
        public static bool infiniteStaminaEnabled;
        public static bool noFallDamageEnabled;

        public static bool speedBoostEnabled;
        public static float speedBoostMultiplier = 2f;

        public static bool espEnabled;
        public static bool espShowLoot;
        public static bool espShowPlayers = true;
        public static bool espShowMonsters = true;
        public static bool espShowInteractors;
        public static bool espShowNPCs;
        public static bool espShowFieldSkills;
        public static bool espShowProjectiles;
        public static bool espShowAuraSkills;
        public static Color espColor = Color.yellow;
        public static float espDistance = 150f;

        public static bool autoLootEnabled;
        public static float autoLootDistance = 50f;
        private Coroutine autoLootCoroutine;
        private float lastAutoLootPickupTime;

        public static bool fullbright;
        private bool fullbrightApplied;
        private float originalAmbientIntensity = -1f;
        private Color originalAmbientColor;
        private bool storedAmbient;
        private readonly List<Light> modifiedLights = new List<Light>();

        private ProtoActor PlayerForActions;
        private ProtoActor[] cachedPlayers;
        private float lastPlayersCacheTime;
        private const float PLAYERS_CACHE_INTERVAL = 0.5f;

        public KeyCode menuHotkey = KeyCode.Insert;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            guiHelper = new GUIHelper();

            demoTabs = new Tabs.TabConfig[]
            {
                new Tabs.TabConfig("Local Player", DrawLocalPlayerTab),
                new Tabs.TabConfig("Movement", DrawMovementTab),
                new Tabs.TabConfig("Visual/ESP", DrawVisualTab),
                new Tabs.TabConfig("Inventory/Items", DrawInventoryTab),
                new Tabs.TabConfig("Players", DrawPlayersTab),
                new Tabs.TabConfig("Settings", DrawSettingsTab),
            };

            cachedPlayers = new ProtoActor[0];
            ESPMain.Initialize();
            MIMESIS.Patches.Patches.ApplyPatches();
        }

        void Update()
        {
            if (isPickingUp && PickupQueue.Count > 0)
            {
                pickupCooldown -= Time.deltaTime;
                if (pickupCooldown <= 0f)
                    ProcessNextPickup();
            }

            if (Input.GetKeyDown(menuHotkey))
                showDemoWindow = !showDemoWindow;

            if (fullbright)
            {
                if (!fullbrightApplied)
                    EnableFullbright();
                ApplyFullbrightTick();
            }
            else
            {
                if (fullbrightApplied)
                    DisableFullbright();
            }
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 30), showDemoWindow ? "Hide Mod Menu" : "Open Mod Menu"))
                showDemoWindow = !showDemoWindow;

            if (showDemoWindow)
                windowRect = GUI.Window(101, windowRect, DrawDemoWindow, "MIMESIS Mod Menu");

            if (espEnabled)
                ESPMain.UpdateESP();
        }

        void OnDestroy()
        {
            ESPMain.Cleanup();
            if (fullbrightApplied)
                DisableFullbright(true);
        }

        #endregion

        #region Window / Tabs

        void DrawDemoWindow(int windowID)
        {
            guiHelper.UpdateAnimations(showDemoWindow);
            if (!guiHelper.BeginAnimatedGUI())
                return;

            currentDemoTab = guiHelper.VerticalTabs(
                demoTabs.Select(t => t.Name).ToArray(),
                currentDemoTab,
                () =>
                {
                    scrollPosition = guiHelper.DrawScrollView(scrollPosition, DrawCurrentTabContent, GUILayout.Height(650));
                },
                maxLines: 1
            );

            guiHelper.EndAnimatedGUI();
            GUI.DragWindow();
        }

        void DrawCurrentTabContent()
        {
            guiHelper.BeginVerticalGroup();
            demoTabs[currentDemoTab].Content?.Invoke();
            guiHelper.EndVerticalGroup();
        }

        #endregion

        #region Tab: Local Player

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

        #endregion

        #region Tab: Movement

        void DrawMovementTab()
        {
            guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper.Label("Speed", LabelVariant.Default);
            speedBoostEnabled = guiHelper.Switch("Speed Boost", speedBoostEnabled);
            DrawSlider("Multiplier", ref speedBoostMultiplier, 1f, 5f, "x");

            guiHelper.HorizontalSeparator();
            guiHelper.Label("Navigation", LabelVariant.Default);
            if (guiHelper.Button("Teleport Forward 50u", ButtonVariant.Default, ButtonSize.Small))
                TeleportForward(50f);
            guiHelper.MutedLabel("Teleport 50 units ahead");

            guiHelper.EndVerticalGroup();
        }

        #endregion

        #region Tab: Visual / ESP

        void DrawVisualTab()
        {
            guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper.Label("ESP Master", LabelVariant.Default);
            espEnabled = guiHelper.Switch("Enable ESP", espEnabled);

            guiHelper.HorizontalSeparator();
            guiHelper.Label("ESP Entities", LabelVariant.Default);
            espShowPlayers = guiHelper.Switch("Players", espShowPlayers);
            espShowMonsters = guiHelper.Switch("Monsters", espShowMonsters);
            espShowLoot = guiHelper.Switch("Loot", espShowLoot);
            espShowInteractors = guiHelper.Switch("Interactors", espShowInteractors);
            espShowNPCs = guiHelper.Switch("NPCs", espShowNPCs);
            espShowFieldSkills = guiHelper.Switch("Field Skills", espShowFieldSkills);
            espShowProjectiles = guiHelper.Switch("Projectiles", espShowProjectiles);
            espShowAuraSkills = guiHelper.Switch("Aura Skills", espShowAuraSkills);

            guiHelper.HorizontalSeparator();
            DrawSlider("ESP Distance", ref espDistance, 50f, 500f, "m");

            guiHelper.HorizontalSeparator();
            guiHelper.Label("Visual", LabelVariant.Default);
            bool newFullbright = guiHelper.Switch("Fullbright", fullbright);
            if (newFullbright != fullbright)
            {
                fullbright = newFullbright;
                if (fullbright)
                    EnableFullbright();
                else
                    DisableFullbright();
            }

            guiHelper.EndVerticalGroup();
        }

        #endregion

        #region Tab: Inventory / Items

        void DrawInventoryTab()
        {
            guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper.Label("Looting", LabelVariant.Default);

            if (guiHelper.Button(isPickingUp ? "Stop Pickup" : "Pickup All Items", isPickingUp ? ButtonVariant.Destructive : ButtonVariant.Default, ButtonSize.Small))
            {
                if (isPickingUp)
                    StopPickup();
                else
                    StartPickupAllItems();
            }

            guiHelper.HorizontalSeparator();
            guiHelper.Label("Auto Loot", LabelVariant.Default);
            autoLootEnabled = guiHelper.Switch("Auto Loot", autoLootEnabled);
            DrawSlider("Loot Distance", ref autoLootDistance, 10f, 200f, "m");

            if (autoLootEnabled)
            {
                if (autoLootCoroutine == null)
                    StartAutoLoot();

                if (guiHelper.Button("Stop Auto Loot", ButtonVariant.Destructive, ButtonSize.Small))
                {
                    autoLootEnabled = false;
                    StopAutoLoot();
                }
            }
            else
            {
                if (autoLootCoroutine != null)
                    StopAutoLoot();

                if (guiHelper.Button("Start Auto Loot", ButtonVariant.Default, ButtonSize.Small))
                {
                    autoLootEnabled = true;
                    StartAutoLoot();
                }
            }

            guiHelper.EndVerticalGroup();
        }

        #endregion

        #region Tab: Players

        void DrawPlayersTab()
        {
            guiHelper.BeginHorizontalGroup();

            guiHelper.BeginVerticalGroup(GUILayout.Width(250));
            guiHelper.Label("Players", LabelVariant.Default);

            if (Time.time - lastPlayersCacheTime > PLAYERS_CACHE_INTERVAL)
            {
                cachedPlayers = PlayerAPI.GetAllPlayers().Where(p => p != null && !string.IsNullOrEmpty(p.nickName)).OrderBy(p => p.nickName).ToArray();
                lastPlayersCacheTime = Time.time;
            }

            if (cachedPlayers.Length == 0)
                guiHelper.MutedLabel("No players found.");

            foreach (ProtoActor p in cachedPlayers)
            {
                string label = p.nickName;
                ProtoActor me = PlayerAPI.GetLocalPlayer();
                if (me != null && p.ActorID == me.ActorID)
                    label += " [You]";

                bool isSelected = PlayerForActions != null && PlayerForActions.ActorID == p.ActorID;
                if (guiHelper.Button(label, isSelected ? ButtonVariant.Secondary : ButtonVariant.Default, ButtonSize.Small))
                    PlayerForActions = p;
            }

            guiHelper.EndVerticalGroup();

            guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));
            guiHelper.Label("Actions", LabelVariant.Default);

            if (PlayerForActions == null)
            {
                guiHelper.MutedLabel("Select a player from the list.");
            }
            else
            {
                ProtoActor me = PlayerAPI.GetLocalPlayer();
                bool hasMe = me != null;

                if (hasMe)
                {
                    if (guiHelper.Button("Teleport Me -> Player", ButtonVariant.Default, ButtonSize.Small))
                        TeleportSelf();
                }

                if (hasMe && PlayerForActions.ActorID != me.ActorID)
                {
                    if (guiHelper.Button("Teleport Player -> Me", ButtonVariant.Default, ButtonSize.Small))
                        Teleport();
                }

                if (guiHelper.Button("Kill Player", ButtonVariant.Destructive, ButtonSize.Small))
                    KillPlayer();
            }

            guiHelper.EndVerticalGroup();
            guiHelper.EndHorizontalGroup();
        }

        void TeleportSelf()
        {
            try
            {
                if (PlayerForActions == null)
                    return;

                ProtoActor me = PlayerAPI.GetLocalPlayer();
                if (me == null)
                    return;

                me.Teleport(PlayerForActions.transform.position + Vector3.back * 1.5f, PlayerForActions.transform.eulerAngles, false);
                MelonLogger.Msg("Teleported to " + PlayerForActions.nickName);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("TeleportSelf error: " + ex.Message);
            }
        }

        void Teleport()
        {
            try
            {
                if (PlayerForActions == null)
                    return;

                ProtoActor me = PlayerAPI.GetLocalPlayer();
                if (me == null)
                    return;

                if (PlayerForActions.ActorID == me.ActorID)
                    return;

                PlayerForActions.Teleport(me.transform.position + Vector3.back * 1.5f, me.transform.eulerAngles, false);
                MelonLogger.Msg("Teleported " + PlayerForActions.nickName + " to you");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Teleport error: " + ex.Message);
            }
        }

        void KillPlayer()
        {
            try
            {
                if (PlayerForActions == null)
                    return;

                PlayerForActions.UpdateHp(0, PlayerForActions.netSyncActorData.maxHP);
                MelonLogger.Msg("Attempted to kill " + PlayerForActions.nickName);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("KillPlayer error: " + ex.Message);
            }
        }

        #endregion

        #region Tab: Settings

        void DrawSettingsTab()
        {
            guiHelper.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper.Label("Item Patches", LabelVariant.Default);
            guiHelper.MutedLabel("Changes require restart to take effect");

            guiHelper.HorizontalSeparator();

            bool durabilityChanged = MIMESIS.Patches.Patches.durabilityPatchEnabled;
            MIMESIS.Patches.Patches.durabilityPatchEnabled = guiHelper.Switch("Infinite Durability", MIMESIS.Patches.Patches.durabilityPatchEnabled);
            if (durabilityChanged != MIMESIS.Patches.Patches.durabilityPatchEnabled)
            {
                MIMESIS.Patches.Patches.ToggleDurabilityPatch(MIMESIS.Patches.Patches.durabilityPatchEnabled);
                guiHelper.Label("Restart game to apply changes", LabelVariant.Default);
            }

            bool priceChanged = MIMESIS.Patches.Patches.pricePatchEnabled;
            MIMESIS.Patches.Patches.pricePatchEnabled = guiHelper.Switch("Infinite Price", MIMESIS.Patches.Patches.pricePatchEnabled);
            if (priceChanged != MIMESIS.Patches.Patches.pricePatchEnabled)
            {
                MIMESIS.Patches.Patches.TogglePricePatch(MIMESIS.Patches.Patches.pricePatchEnabled);
                guiHelper.Label("Restart game to apply changes", LabelVariant.Default);
            }

            bool gaugeChanged = MIMESIS.Patches.Patches.gaugePatchEnabled;
            MIMESIS.Patches.Patches.gaugePatchEnabled = guiHelper.Switch("Infinite Gauge", MIMESIS.Patches.Patches.gaugePatchEnabled);
            if (gaugeChanged != MIMESIS.Patches.Patches.gaugePatchEnabled)
            {
                MIMESIS.Patches.Patches.ToggleGaugePatch(MIMESIS.Patches.Patches.gaugePatchEnabled);
                guiHelper.Label("Restart game to apply changes", LabelVariant.Default);
            }

            guiHelper.EndVerticalGroup();
        }

        #endregion

        #region GUI Helpers

        void DrawSlider(string label, ref float value, float min, float max, string suffix)
        {
            guiHelper.BeginHorizontalGroup();
            guiHelper.Label(label + ": " + value.ToString("F1") + suffix, LabelVariant.Default);
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            guiHelper.EndHorizontalGroup();
        }

        #endregion

        #region Pickup / AutoLoot

        void StartPickupAllItems()
        {
            try
            {
                PickupQueue.Clear();
                LootingLevelObject[] allLoot = LootAPI.GetAllLoot();
                if (allLoot.Length > 0)
                {
                    PickupQueue.AddRange(allLoot);
                    isPickingUp = true;
                    pickupCooldown = 0.05f;
                    MelonLogger.Msg("Starting to pickup " + PickupQueue.Count + " items");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("StartPickupAllItems error: " + ex.Message);
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
                ProtoActor player = PlayerAPI.GetLocalPlayer();
                if (player == null)
                {
                    StopPickup();
                    return;
                }

                LootingLevelObject loot = PickupQueue[0];
                PickupQueue.RemoveAt(0);

                if (loot != null && loot.gameObject.activeInHierarchy)
                    player.GrapLootingObject(loot.ActorID);

                pickupCooldown = 0.05f;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error picking up item: " + ex.Message);
                pickupCooldown = 0.05f;
            }
        }

        void StopPickup()
        {
            isPickingUp = false;
            PickupQueue.Clear();
            MelonLogger.Msg("Pickup stopped");
        }

        void StartAutoLoot()
        {
            if (autoLootCoroutine != null)
                StopCoroutine(autoLootCoroutine);
            lastAutoLootPickupTime = 0f;
            autoLootCoroutine = StartCoroutine(AutoLootCoroutine());
        }

        IEnumerator AutoLootCoroutine()
        {
            while (autoLootEnabled)
            {
                yield return new WaitForSeconds(0.5f);
                if (!autoLootEnabled)
                    yield break;

                try
                {
                    ProtoActor player = PlayerAPI.GetLocalPlayer();
                    if (player != null)
                    {
                        LootingLevelObject[] loot = LootAPI.GetLootNearby(autoLootDistance, player.transform.position);
                        foreach (LootingLevelObject l in loot)
                        {
                            if (l != null && l.gameObject.activeInHierarchy && Time.time - lastAutoLootPickupTime > 0.5f)
                            {
                                player.GrapLootingObject(l.ActorID);
                                lastAutoLootPickupTime = Time.time;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("AutoLoot error: " + ex.Message);
                }
            }
        }

        void StopAutoLoot()
        {
            if (autoLootCoroutine != null)
            {
                StopCoroutine(autoLootCoroutine);
                autoLootCoroutine = null;
            }
            MelonLogger.Msg("Auto loot stopped");
        }

        #endregion

        #region Movement Helpers

        void TeleportForward(float distance)
        {
            try
            {
                ProtoActor player = PlayerAPI.GetLocalPlayer();
                if (player == null)
                    return;

                Vector3 newPos = player.transform.position + player.transform.forward * distance;
                player.Teleport(newPos, player.transform.eulerAngles, false);
                MelonLogger.Msg("Teleported forward");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("TeleportForward error: " + ex.Message);
            }
        }

        #endregion

        #region Fullbright

        void EnableFullbright()
        {
            try
            {
                if (!storedAmbient)
                {
                    originalAmbientColor = RenderSettings.ambientLight;
                    originalAmbientIntensity = RenderSettings.ambientIntensity;
                    storedAmbient = true;
                }

                RenderSettings.ambientLight = Color.white;
                RenderSettings.ambientIntensity = 1.5f;

                modifiedLights.Clear();
                foreach (Light l in FindObjectsOfType<Light>())
                {
                    if (l != null && l.enabled)
                    {
                        l.intensity *= 1.5f;
                        modifiedLights.Add(l);
                    }
                }

                fullbrightApplied = true;
                MelonLogger.Msg("Fullbright enabled");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("EnableFullbright error: " + ex.Message);
            }
        }

        void DisableFullbright(bool force = false)
        {
            if (!storedAmbient && !force)
                return;

            try
            {
                if (storedAmbient)
                {
                    RenderSettings.ambientLight = originalAmbientColor;
                    if (originalAmbientIntensity > 0f)
                        RenderSettings.ambientIntensity = originalAmbientIntensity;
                }

                foreach (Light l in modifiedLights)
                {
                    if (l != null)
                        l.intensity /= 1.5f;
                }
                modifiedLights.Clear();

                fullbrightApplied = false;
                MelonLogger.Msg("Fullbright disabled");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("DisableFullbright error: " + ex.Message);
            }
        }

        void ApplyFullbrightTick()
        {
            try
            {
                RenderSettings.ambientLight = Color.white;
                if (RenderSettings.ambientIntensity < 1.4f)
                    RenderSettings.ambientIntensity = 1.5f;
            }
            catch { }
        }

        #endregion
    }
}
