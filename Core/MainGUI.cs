using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using Mimesis_Mod_Menu.Core.Config;
using Mimesis_Mod_Menu.Core.Features;
using Mimic.Actors;
using MimicAPI.GameAPI;
using ReluProtocol.Enum;
using shadcnui.GUIComponents.Core;
using shadcnui.GUIComponents.Layout;
using UnityEngine;

namespace Mimesis_Mod_Menu.Core
{
    public class MainGUI : MonoBehaviour
    {
        #region Fields

        private GUIHelper? guiHelper;
        private Rect windowRect = new Rect(20, 20, 1000, 800);
        private bool showDemoWindow = true;
        private Vector2 scrollPosition;
        private int currentDemoTab;
        private Tabs.TabConfig[]? demoTabs;
        private float lastConfigSaveTime;
        private ConfigManager? configManager;

        private PickupManager? pickupManager;
        private MovementManager? movementManager;

        private AutoLootManager? autoLootManager;
        private FullbrightManager? fullbrightManager;

        private ProtoActor? selectedPlayer;

        public static bool godModeEnabled = false;
        public static bool infiniteStaminaEnabled = false;
        public static bool noFallDamageEnabled = false;

        public static bool speedBoostEnabled = false;
        public static float speedBoostMultiplier = 2f;

        public static bool espEnabled = false;
        public static bool espShowLoot = false;
        public static bool espShowPlayers = true;
        public static bool espShowMonsters = true;
        public static bool espShowInteractors = false;
        public static bool espShowNPCs = false;
        public static bool espShowFieldSkills = false;
        public static bool espShowProjectiles = false;
        public static bool espShowAuraSkills = false;
        public static Color espColor = Color.yellow;
        public static float espDistance = 150f;

        public static bool autoLootEnabled = false;
        public static float autoLootDistance = 50f;

        public static bool fullbright = false;

        private ProtoActor[]? cachedPlayers;
        private float lastPlayerCacheTime;
        private const float PLAYER_CACHE_INTERVAL = 5f;

        #endregion

        #region Core

        void Start()
        {
            guiHelper = new GUIHelper();
            configManager = new ConfigManager();

            godModeEnabled = configManager.GetBool("godModeEnabled", false);
            infiniteStaminaEnabled = configManager.GetBool("infiniteStaminaEnabled", false);
            noFallDamageEnabled = configManager.GetBool("noFallDamageEnabled", false);
            speedBoostEnabled = configManager.GetBool("speedBoostEnabled", false);
            speedBoostMultiplier = configManager.GetFloat("speedBoostMultiplier", 2f);
            espEnabled = configManager.GetBool("espEnabled", false);
            espShowLoot = configManager.GetBool("espShowLoot", false);
            espShowPlayers = configManager.GetBool("espShowPlayers", true);
            espShowMonsters = configManager.GetBool("espShowMonsters", true);
            espShowInteractors = configManager.GetBool("espShowInteractors", false);
            espShowNPCs = configManager.GetBool("espShowNPCs", false);
            espShowFieldSkills = configManager.GetBool("espShowFieldSkills", false);
            espShowProjectiles = configManager.GetBool("espShowProjectiles", false);
            espShowAuraSkills = configManager.GetBool("espShowAuraSkills", false);
            espDistance = configManager.GetFloat("espDistance", 150f);
            autoLootEnabled = configManager.GetBool("autoLootEnabled", false);
            autoLootDistance = configManager.GetFloat("autoLootDistance", 50f);
            fullbright = configManager.GetBool("fullbright", false);

            demoTabs = new Tabs.TabConfig[]
            {
                new Tabs.TabConfig("Local Player", DrawLocalPlayerTab),
                new Tabs.TabConfig("Movement", DrawMovementTab),
                new Tabs.TabConfig("Visual/ESP", DrawVisualTab),
                new Tabs.TabConfig("Inventory/Items", DrawInventoryTab),
                new Tabs.TabConfig("Actor", DrawActorsTab),
                new Tabs.TabConfig("Hotkeys", DrawHotkeysTab),
                new Tabs.TabConfig("Misc", DrawMiscTab),
            };

            autoLootManager = new AutoLootManager();
            fullbrightManager = new FullbrightManager();
            pickupManager = new PickupManager();
            movementManager = new MovementManager();

            ESPManager.Initialize();
            Patches.ApplyPatches(configManager);

            MelonLogger.Msg("Mimesis Mod Menu initialized successfully");
        }

        private float lastMenuToggleTime = 0f;
        private const float HOTKEY_COOLDOWN = 0.5f;

        void Update()
        {
            if (configManager == null)
            {
                autoLootManager?.Update();
                fullbrightManager?.Update();
                pickupManager?.Update();
                movementManager?.Update();
                return;
            }

            try
            {
                var toggleMenuHotkey = configManager.GetHotkey("ToggleMenu");
                if (toggleMenuHotkey.Key != KeyCode.None)
                {
                    if (toggleMenuHotkey.IsPressed())
                    {
                        if (Time.time - lastMenuToggleTime > HOTKEY_COOLDOWN)
                        {
                            showDemoWindow = !showDemoWindow;
                            lastMenuToggleTime = Time.time;
                        }
                    }
                }

                if (configManager.GetHotkey("ToggleGodMode").IsPressed())
                {
                    godModeEnabled = !godModeEnabled;
                    configManager.SetBool("godModeEnabled", godModeEnabled);
                    SaveConfigDebounced();
                }

                if (configManager.GetHotkey("ToggleInfiniteStamina").IsPressed())
                {
                    infiniteStaminaEnabled = !infiniteStaminaEnabled;
                    configManager.SetBool("infiniteStaminaEnabled", infiniteStaminaEnabled);
                    SaveConfigDebounced();
                }

                if (configManager.GetHotkey("ToggleNoFallDamage").IsPressed())
                {
                    noFallDamageEnabled = !noFallDamageEnabled;
                    configManager.SetBool("noFallDamageEnabled", noFallDamageEnabled);
                    SaveConfigDebounced();
                }

                if (configManager.GetHotkey("ToggleSpeedBoost").IsPressed())
                {
                    speedBoostEnabled = !speedBoostEnabled;
                    configManager.SetBool("speedBoostEnabled", speedBoostEnabled);
                    SaveConfigDebounced();
                }

                if (configManager.GetHotkey("ToggleESP").IsPressed())
                {
                    espEnabled = !espEnabled;
                    configManager.SetBool("espEnabled", espEnabled);
                    SaveConfigDebounced();
                }

                if (configManager.GetHotkey("ToggleAutoLoot").IsPressed())
                {
                    autoLootEnabled = !autoLootEnabled;
                    autoLootManager?.SetEnabled(autoLootEnabled);
                    configManager.SetBool("autoLootEnabled", autoLootEnabled);
                    SaveConfigDebounced();
                }

                if (configManager.GetHotkey("ToggleFullbright").IsPressed())
                {
                    fullbright = !fullbright;
                    fullbrightManager?.SetEnabled(fullbright);
                    configManager.SetBool("fullbright", fullbright);
                    SaveConfigDebounced();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Hotkey update error: {ex.Message}");
            }

            autoLootManager?.Update();
            fullbrightManager?.Update();
            pickupManager?.Update();
            movementManager?.Update();
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 30), showDemoWindow ? "Hide Mod Menu" : "Open Mod Menu"))
                showDemoWindow = !showDemoWindow;

            if (showDemoWindow)
                windowRect = GUI.Window(101, windowRect, DrawDemoWindow, "Mimesis Mod Menu");

            ESPManager.UpdateESP();
        }

        void OnDestroy()
        {
            SaveConfig();
            fullbrightManager?.Cleanup();
            ESPManager.Cleanup();
            pickupManager?.Stop();
        }

        #endregion

        #region Window Tabs

        void DrawDemoWindow(int windowID)
        {
            guiHelper?.UpdateAnimations(showDemoWindow);
            if (!(guiHelper?.BeginAnimatedGUI() ?? false))
                return;

            currentDemoTab = guiHelper?.VerticalTabs(demoTabs?.Select(t => t.Name).ToArray() ?? new string[0], currentDemoTab, DrawCurrentTabContent, tabWidth: 140f, maxLines: 1) ?? 0;

            guiHelper?.EndAnimatedGUI();
            GUI.DragWindow();
        }

        void DrawCurrentTabContent()
        {
            scrollPosition =
                guiHelper?.DrawScrollView(
                    scrollPosition,
                    () =>
                    {
                        guiHelper?.BeginVerticalGroup(GUILayout.ExpandHeight(true));
                        demoTabs?[currentDemoTab].Content?.Invoke();
                        guiHelper?.EndVerticalGroup();
                    },
                    GUILayout.Height(700)
                ) ?? scrollPosition;
        }

        #endregion

        #region Tab Local Player

        void DrawLocalPlayerTab()
        {
            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Protection & Safety");
            guiHelper?.CardContent(() =>
            {
                bool godModeEnabled = MainGUI.godModeEnabled;
                bool newGodMode = guiHelper?.Switch("God Mode", godModeEnabled) ?? false;
                if (newGodMode != godModeEnabled)
                {
                    godModeEnabled = newGodMode;
                    configManager?.SetBool("godModeEnabled", newGodMode);
                    SaveConfigDebounced();
                }

                guiHelper?.AddSpace(8);

                bool noFallEnabled = noFallDamageEnabled;
                bool newNoFall = guiHelper?.Switch("No Fall Damage", noFallEnabled) ?? false;
                if (newNoFall != noFallEnabled)
                {
                    noFallDamageEnabled = newNoFall;
                    configManager?.SetBool("noFallDamageEnabled", newNoFall);
                    SaveConfigDebounced();
                }
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Recovery");
            guiHelper?.CardContent(() =>
            {
                bool staminaEnabled = infiniteStaminaEnabled;
                bool newStamina = guiHelper?.Switch("Infinite Stamina", staminaEnabled) ?? false;
                if (newStamina != staminaEnabled)
                {
                    infiniteStaminaEnabled = newStamina;
                    configManager?.SetBool("infiniteStaminaEnabled", newStamina);
                    SaveConfigDebounced();
                }
            });
            guiHelper?.EndCard();

            guiHelper?.EndVerticalGroup();
        }

        #endregion

        #region Tab Movement

        void DrawMovementTab()
        {
            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Speed");
            guiHelper?.CardContent(() =>
            {
                bool speedEnabled = speedBoostEnabled;
                bool newSpeedEnabled = guiHelper?.Switch("Speed Boost", speedEnabled) ?? false;
                if (newSpeedEnabled != speedEnabled)
                {
                    speedBoostEnabled = newSpeedEnabled;
                    configManager?.SetBool("speedBoostEnabled", newSpeedEnabled);
                    SaveConfigDebounced();
                }

                if (speedBoostEnabled)
                {
                    guiHelper?.AddSpace(8);
                    DrawLabeledSlider("Multiplier", ref speedBoostMultiplier, 1f, 5f, "x");
                    configManager?.SetFloat("speedBoostMultiplier", speedBoostMultiplier);
                    guiHelper?.MutedLabel($"Current: {speedBoostMultiplier:F2}x speed");
                }
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Navigation");
            guiHelper?.CardDescription("Teleport utilities");
            guiHelper?.CardContent(() =>
            {
                if (guiHelper?.Button("Forward 50u", ButtonVariant.Default, ButtonSize.Default) ?? false)
                    movementManager?.TeleportForward(50f);
                guiHelper?.AddSpace(6);
                if (guiHelper?.Button("Forward 100u", ButtonVariant.Default, ButtonSize.Default) ?? false)
                    movementManager?.TeleportForward(100f);
                guiHelper?.AddSpace(6);
                if (guiHelper?.Button("Forward 200u", ButtonVariant.Default, ButtonSize.Default) ?? false)
                    movementManager?.TeleportForward(200f);

                guiHelper?.AddSpace(8);
            });
            guiHelper?.EndCard();

            guiHelper?.EndVerticalGroup();
        }

        #endregion

        #region Tab Visual ESP

        void DrawVisualTab()
        {
            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("ESP");
            guiHelper?.CardContent(() =>
            {
                bool espEnabled = MainGUI.espEnabled;
                bool newEspEnabled = guiHelper?.Switch("Enable ESP", espEnabled) ?? false;
                if (newEspEnabled != espEnabled)
                {
                    espEnabled = newEspEnabled;
                    configManager?.SetBool("espEnabled", newEspEnabled);
                    SaveConfigDebounced();
                }

                if (espEnabled)
                {
                    guiHelper?.AddSpace(10);
                    DrawLabeledSlider("Distance", ref espDistance, 50f, 500f, "m");
                    configManager?.SetFloat("espDistance", espDistance);
                }
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Visibility");
            guiHelper?.CardDescription("Toggle which entities to display");
            guiHelper?.CardContent(() =>
            {
                guiHelper?.BeginHorizontalGroup();
                guiHelper?.BeginVerticalGroup(GUILayout.Width(150));
                espShowPlayers = guiHelper?.Switch("Players", espShowPlayers) ?? false;
                espShowMonsters = guiHelper?.Switch("Monsters", espShowMonsters) ?? false;
                espShowLoot = guiHelper?.Switch("Loot", espShowLoot) ?? false;
                guiHelper?.EndVerticalGroup();

                guiHelper?.BeginVerticalGroup(GUILayout.Width(150));
                espShowInteractors = guiHelper?.Switch("Interactors", espShowInteractors) ?? false;
                espShowNPCs = guiHelper?.Switch("NPCs", espShowNPCs) ?? false;
                espShowFieldSkills = guiHelper?.Switch("Field Skills", espShowFieldSkills) ?? false;
                guiHelper?.EndVerticalGroup();

                guiHelper?.BeginVerticalGroup(GUILayout.Width(150));
                espShowProjectiles = guiHelper?.Switch("Projectiles", espShowProjectiles) ?? false;
                espShowAuraSkills = guiHelper?.Switch("Aura Skills", espShowAuraSkills) ?? false;
                guiHelper?.EndVerticalGroup();
                guiHelper?.EndHorizontalGroup();

                SaveConfigDebounced();
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Visual Effects");
            guiHelper?.CardContent(() =>
            {
                bool fullbrightEnabled = fullbrightManager?.IsEnabled ?? false;
                bool newFullbright = guiHelper?.Switch("Fullbright", fullbrightEnabled) ?? false;
                if (newFullbright != fullbrightEnabled)
                {
                    fullbrightManager?.SetEnabled(newFullbright);
                    fullbright = newFullbright;
                    configManager?.SetBool("fullbright", newFullbright);
                    SaveConfigDebounced();
                }
            });
            guiHelper?.EndCard();

            guiHelper?.EndVerticalGroup();
        }

        #endregion

        #region Tab Inventory Items

        void DrawInventoryTab()
        {
            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Item Pickup");
            guiHelper?.CardContent(() =>
            {
                string buttonText = (pickupManager?.isActive ?? false) ? "Stop Picking Up" : "Pickup All Items";
                ButtonVariant variant = (pickupManager?.isActive ?? false) ? ButtonVariant.Destructive : ButtonVariant.Default;

                if (guiHelper?.Button(buttonText, variant, ButtonSize.Default) ?? false)
                {
                    if (pickupManager?.isActive ?? false)
                        pickupManager?.Stop();
                    else
                        pickupManager?.StartPickupAll();
                }

                guiHelper?.MutedLabel((pickupManager?.isActive ?? false) ? "Actively picking up items..." : "Click to start automatic item collection");
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Auto Loot");
            guiHelper?.CardDescription("Automatically collect nearby items");
            guiHelper?.CardContent(() =>
            {
                bool autoLootEnabled = autoLootManager?.IsEnabled ?? false;
                bool newAutoLoot = guiHelper?.Switch("Auto Loot Enabled", autoLootEnabled) ?? false;
                if (newAutoLoot != autoLootEnabled)
                {
                    autoLootManager?.SetEnabled(newAutoLoot);
                    configManager?.SetBool("autoLootEnabled", newAutoLoot);
                    SaveConfigDebounced();
                }

                if (autoLootManager?.IsEnabled ?? false)
                {
                    guiHelper?.AddSpace(8);
                    float distance = autoLootManager?.GetDistance() ?? 50f;
                    DrawLabeledSlider("Detection Range", ref distance, 10f, 200f, "m");
                    autoLootManager?.SetDistance(distance);
                    configManager?.SetFloat("autoLootDistance", distance);
                    guiHelper?.MutedLabel($"Current range: {distance:F1}m");
                }
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Gameplay Modifiers");
            guiHelper?.CardContent(() =>
            {
                DrawToggleWithSave("Force Buy Items", () => Patches.forceBuyEnabled, (v) => Patches.forceBuyEnabled = v, "");

                guiHelper?.AddSpace(10);

                DrawToggleWithSave("Force Repair", () => Patches.forceRepairEnabled, (v) => Patches.forceRepairEnabled = v, "");

                guiHelper?.AddSpace(10);

                DrawToggleWithSave("Infinite Currency", () => Patches.infiniteCurrencyEnabled, (v) => Patches.infiniteCurrencyEnabled = v, "");
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Items");
            guiHelper?.CardDescription("Requires restart to apply changes");
            guiHelper?.CardContent(() =>
            {
                DrawToggleWithRestart("Infinite Durability", () => Patches.durabilityPatchEnabled, (v) => Patches.durabilityPatchEnabled = v, "");

                guiHelper?.AddSpace(10);

                DrawToggleWithRestart("Infinite Price", () => Patches.pricePatchEnabled, (v) => Patches.pricePatchEnabled = v, "");

                guiHelper?.AddSpace(10);

                DrawToggleWithRestart("Infinite Gauge", () => Patches.gaugePatchEnabled, (v) => Patches.gaugePatchEnabled = v, "");
            });
            guiHelper?.EndCard();

            guiHelper?.EndVerticalGroup();
        }

        #endregion

        #region Tab Actors

        void DrawActorsTab()
        {
            UpdatePlayerCache();

            guiHelper?.BeginHorizontalGroup();

            guiHelper?.BeginVerticalGroup(GUILayout.Width(300));
            DrawActorListPanel();
            guiHelper?.EndVerticalGroup();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));
            DrawActorActionsPanel();
            guiHelper?.EndVerticalGroup();

            guiHelper?.EndHorizontalGroup();
        }

        private void DrawActorListPanel()
        {
            guiHelper?.BeginCard(width: 280, height: 600);
            guiHelper?.CardTitle("Actors");

            int totalCount = cachedPlayers?.Length ?? 0;
            guiHelper?.CardDescription($"Total: {totalCount}");

            guiHelper?.CardContent(() =>
            {
                ProtoActor[] displayPlayers = cachedPlayers ?? System.Array.Empty<ProtoActor>();

                if (displayPlayers.Length == 0)
                {
                    guiHelper?.MutedLabel("No actors found");
                }
                else
                {
                    int maxDisplay = Mathf.Min(displayPlayers.Length, 15);
                    for (int i = 0; i < maxDisplay; i++)
                    {
                        DrawActorListItem(displayPlayers[i]);
                    }

                    if (displayPlayers.Length > maxDisplay)
                    {
                        guiHelper?.MutedLabel($"...and {displayPlayers.Length - maxDisplay} more");
                    }
                }
            });
            guiHelper?.EndCard();
        }

        private void UpdatePlayerCache()
        {
            if (Time.time - lastPlayerCacheTime < PLAYER_CACHE_INTERVAL)
                return;

            try
            {
                ProtoActor[] allActors = PlayerAPI.GetAllPlayers();
                cachedPlayers = allActors.Where(p => p != null && !string.IsNullOrEmpty(p.nickName) && !p.dead).OrderBy(p => p.ActorType).ThenBy(p => p.nickName).ToArray();

                lastPlayerCacheTime = Time.time;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"UpdatePlayerCache error: {ex.Message}");
            }
        }

        private void DrawActorListItem(ProtoActor actor)
        {
            string label = actor.nickName;
            ProtoActor? localPlayer = PlayerAPI.GetLocalPlayer();
            string typeLabel = actor.ActorType == ActorType.Player ? "[P]" : "[M]";

            if (localPlayer != null && actor.ActorID == localPlayer.ActorID)
                label += " [YOU]";

            label = $"{typeLabel} {label}";

            bool isSelected = selectedPlayer != null && selectedPlayer.ActorID == actor.ActorID;
            ButtonVariant variant = isSelected ? ButtonVariant.Secondary : ButtonVariant.Ghost;

            if (guiHelper?.Button(label, variant, ButtonSize.Small) ?? false)
                selectedPlayer = actor;
        }

        private void DrawActorActionsPanel()
        {
            ProtoActor? localPlayer = PlayerAPI.GetLocalPlayer();

            guiHelper?.BeginCard(width: -1, height: 600);
            guiHelper?.CardTitle("Actor Actions");

            if (selectedPlayer != null)
            {
                string actorType = selectedPlayer.ActorType == ActorType.Player ? "Player" : "Monster";
                guiHelper?.CardDescription($"Target: {selectedPlayer.nickName} ({actorType})");
            }
            else
            {
                guiHelper?.CardDescription("Select an actor to perform actions");
            }

            guiHelper?.CardContent(() =>
            {
                if (selectedPlayer == null)
                {
                    guiHelper?.MutedLabel("No actor selected");
                }
                else
                {
                    DrawActorInfo(selectedPlayer, localPlayer);
                    guiHelper?.AddSpace(12);
                    guiHelper?.LabeledSeparator("Actions");
                    DrawActorActionButtons(selectedPlayer, localPlayer);
                }
            });
            guiHelper?.EndCard();
        }

        private void DrawActorInfo(ProtoActor? selectedTarget, ProtoActor? localPlayer)
        {
            if (selectedTarget == null)
                return;

            guiHelper?.Label($"Name: {selectedTarget.nickName}", LabelVariant.Default);
            guiHelper?.Label($"Type: {(selectedTarget.ActorType == ActorType.Player ? "Player" : "Monster")}", LabelVariant.Default);
            guiHelper?.Label($"Actor ID: {selectedTarget.ActorID}", LabelVariant.Default);

            if (localPlayer != null && selectedTarget.ActorID != localPlayer.ActorID)
            {
                float distance = Vector3.Distance(selectedTarget.transform.position, localPlayer.transform.position);
                guiHelper?.Label($"Distance: {distance:F1}m", LabelVariant.Default);
            }
        }

        private void DrawActorActionButtons(ProtoActor selectedTarget, ProtoActor? localPlayer)
        {
            if (localPlayer != null)
            {
                if (guiHelper?.Button("Teleport to Actor", ButtonVariant.Default, ButtonSize.Default) ?? false)
                    movementManager?.TeleportToPlayer(selectedTarget);

                if (selectedTarget.ActorID != localPlayer.ActorID)
                {
                    guiHelper?.AddSpace(6);
                    if (guiHelper?.Button("Teleport Actor to Me", ButtonVariant.Default, ButtonSize.Default) ?? false)
                        movementManager?.TeleportPlayerToSelf(selectedTarget);

                    guiHelper?.AddSpace(6);
                    if (selectedTarget.ActorType == ActorType.Player)
                    {
                        if (guiHelper?.Button("Kill Player", ButtonVariant.Destructive, ButtonSize.Default) ?? false)
                            KillPlayer(selectedTarget);
                    }
                    else if (selectedTarget.ActorType == ActorType.Monster)
                    {
                        if (guiHelper?.Button("Kill Monster", ButtonVariant.Destructive, ButtonSize.Default) ?? false)
                            KillMonster(selectedTarget);
                    }
                }
            }
        }

        private void KillPlayer(ProtoActor target)
        {
            try
            {
                if (target != null && target.ActorType == ActorType.Player)
                {
                    target.OnActorDeath(
                        new ProtoActor.ActorDeathInfo
                        {
                            DeadActorID = target.ActorID,
                            ReasonOfDeath = ReasonOfDeath.None,
                            AttackerActorID = 0,
                            LinkedMasterID = 0,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"KillPlayer error: {ex.Message}");
            }
        }

        private void KillMonster(ProtoActor target)
        {
            try
            {
                if (target != null && target.ActorType == ActorType.Monster)
                {
                    target.OnActorDeath(
                        new ProtoActor.ActorDeathInfo
                        {
                            DeadActorID = target.ActorID,
                            ReasonOfDeath = ReasonOfDeath.None,
                            AttackerActorID = 0,
                            LinkedMasterID = 0,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"KillMonster error: {ex.Message}");
            }
        }

        #endregion

        #region Tab Hotkeys

        void DrawHotkeysTab()
        {
            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Hotkey Configuration");
            guiHelper?.CardDescription("Edit hotkeys.cfg in UserData/Mimesis/ folder");
            guiHelper?.CardContent(() =>
            {
                var hotkeys = configManager?.GetAllHotkeys() ?? new Dictionary<string, Config.HotkeyConfig>();

                foreach (var kvp in hotkeys.OrderBy(x => x.Key))
                {
                    string displayName = System.Text.RegularExpressions.Regex.Replace(kvp.Key, "([a-z])([A-Z])", "$1 $2");
                    guiHelper?.Label($"{displayName}: {kvp.Value}", LabelVariant.Default);
                    guiHelper?.AddSpace(2);
                }
            });
            guiHelper?.EndCard();

            guiHelper?.AddSpace(12);

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Configuration Files");
            guiHelper?.CardContent(() =>
            {
                guiHelper?.MutedLabel("Main Config: UserData/Mimesis/config.cfg");
                guiHelper?.MutedLabel("Hotkeys: UserData/Mimesis/hotkeys.cfg");
                guiHelper?.MutedLabel("Format: FeatureName=Ctrl+Shift+Alt+KeyCode");

                guiHelper?.AddSpace(10);

                if (guiHelper?.Button("Save Configuration", ButtonVariant.Default, ButtonSize.Default) ?? false)
                {
                    configManager?.SaveMainConfig();
                    configManager?.SaveHotkeysConfig();
                    MelonLogger.Msg("Configuration saved");
                }

                guiHelper?.AddSpace(6);

                if (guiHelper?.Button("Reload Configuration", ButtonVariant.Default, ButtonSize.Default) ?? false)
                {
                    configManager?.LoadAllConfigs();
                    MelonLogger.Msg("Configuration reloaded");
                }

                guiHelper?.AddSpace(10);

                guiHelper?.MutedLabel("To rebind hotkeys:");
                guiHelper?.MutedLabel("1. Open hotkeys.cfg in UserData/Mimesis/");
                guiHelper?.MutedLabel("2. Edit the key bindings");
                guiHelper?.MutedLabel("3. Click Reload Configuration");
                guiHelper?.MutedLabel("4. Or set to None to disable");
            });
            guiHelper?.EndCard();

            guiHelper?.EndVerticalGroup();
        }

        #endregion

        #region Tab Misc

        void DrawMiscTab()
        {
            guiHelper?.BeginVerticalGroup(GUILayout.ExpandWidth(true));

            guiHelper?.BeginCard(width: -1, height: -1);
            guiHelper?.CardTitle("Mass Actions");
            guiHelper?.CardContent(() =>
            {
                if (guiHelper?.Button("Kill All Players", ButtonVariant.Destructive, ButtonSize.Default) ?? false)
                    KillAllPlayers();

                guiHelper?.AddSpace(10);

                if (guiHelper?.Button("Kill All Monsters", ButtonVariant.Destructive, ButtonSize.Default) ?? false)
                    KillAllMonsters();
            });
            guiHelper?.EndCard();

            guiHelper?.EndVerticalGroup();
        }

        private void KillAllPlayers()
        {
            try
            {
                ProtoActor[] allActors = PlayerAPI.GetAllPlayers();
                foreach (ProtoActor actor in allActors)
                {
                    if (actor != null && actor.ActorType == ActorType.Player && !actor.dead)
                    {
                        actor.OnActorDeath(
                            new ProtoActor.ActorDeathInfo
                            {
                                DeadActorID = actor.ActorID,
                                ReasonOfDeath = ReasonOfDeath.None,
                                AttackerActorID = 0,
                                LinkedMasterID = 0,
                            }
                        );
                    }
                }
                MelonLogger.Msg("Killed all players");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"KillAllPlayers error: {ex.Message}");
            }
        }

        private void KillAllMonsters()
        {
            try
            {
                ProtoActor[] allActors = PlayerAPI.GetAllPlayers();
                foreach (ProtoActor actor in allActors)
                {
                    if (actor != null && actor.ActorType == ActorType.Monster && !actor.dead)
                    {
                        actor.OnActorDeath(
                            new ProtoActor.ActorDeathInfo
                            {
                                DeadActorID = actor.ActorID,
                                ReasonOfDeath = ReasonOfDeath.None,
                                AttackerActorID = 0,
                                LinkedMasterID = 0,
                            }
                        );
                    }
                }
                MelonLogger.Msg("Killed all monsters");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"KillAllMonsters error: {ex.Message}");
            }
        }

        #endregion

        #region GUI Helpers

        private void DrawLabeledSlider(string label, ref float value, float min, float max, string suffix)
        {
            guiHelper?.BeginHorizontalGroup();
            guiHelper?.Label($"{label}: {value:F1}{suffix}", LabelVariant.Default);
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            guiHelper?.EndHorizontalGroup();
        }

        private void DrawToggleWithRestart(string label, Func<bool> getter, Action<bool> setter, string description = "")
        {
            bool oldValue = getter();
            bool newValue = guiHelper?.Switch(label, oldValue) ?? false;

            if (!string.IsNullOrEmpty(description))
                guiHelper?.MutedLabel(description);

            if (oldValue != newValue)
            {
                setter(newValue);
                SaveConfigDebounced();
                guiHelper?.AddSpace(6);
                guiHelper?.DestructiveLabel("Restart game to apply");
            }
        }

        private void DrawToggleWithSave(string label, Func<bool> getter, Action<bool> setter, string description = "")
        {
            bool oldValue = getter();
            bool newValue = guiHelper?.Switch(label, oldValue) ?? false;

            if (!string.IsNullOrEmpty(description))
                guiHelper?.MutedLabel(description);

            if (oldValue != newValue)
            {
                setter(newValue);
                SaveConfigDebounced();
            }
        }

        private void SaveConfigDebounced()
        {
            if (Time.time - lastConfigSaveTime > 0.5f)
            {
                SaveConfig();
                lastConfigSaveTime = Time.time;
            }
        }

        private void SaveConfig()
        {
            configManager?.SaveMainConfig();
            configManager?.SaveHotkeysConfig();
        }

        #endregion
    }
}
