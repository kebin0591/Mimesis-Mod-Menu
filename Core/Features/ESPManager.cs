using System;
using System.Collections.Generic;
using Mimesis_Mod_Menu.Core;
using Mimic.Actors;
using MimicAPI.GameAPI;
using ReluProtocol.Enum;
using UnityEngine;

namespace Mimesis_Mod_Menu.Core.Features
{
    public static class ESPManager
    {
        private static Texture2D cachedLineTexture;
        private static Camera mainCamera;
        private static List<LootingLevelObject> cachedLootObjects = new List<LootingLevelObject>();
        private static List<ProtoActor> cachedActors = new List<ProtoActor>();
        private static float lastUpdateTime = 0f;
        private const float CACHE_UPDATE_INTERVAL = 0.5f;
        private const float ESP_TEXT_SIZE = 11f;
        private static bool isInitialized = false;

        private static readonly Dictionary<ActorType, string> ActorTypeLabels = new Dictionary<ActorType, string>
        {
            { ActorType.Player, "[PLAYER]" },
            { ActorType.Monster, "[MONSTER]" },
            { ActorType.Interactor, "[INTERACTOR]" },
            { ActorType.NPC, "[NPC]" },
            { ActorType.LootingObject, "[LOOT]" },
            { ActorType.FieldSkill, "[FIELD SKILL]" },
            { ActorType.Projectile, "[PROJECTILE]" },
            { ActorType.AuraSkill, "[AURA SKILL]" },
        };

        private static readonly Dictionary<ActorType, Color> ActorTypeColors = new Dictionary<ActorType, Color>
        {
            { ActorType.Player, Color.yellow },
            { ActorType.Monster, Color.red },
            { ActorType.Interactor, Color.cyan },
            { ActorType.NPC, Color.green },
            { ActorType.LootingObject, Color.yellow },
            { ActorType.FieldSkill, new Color(1f, 0.5f, 0f) },
            { ActorType.Projectile, Color.magenta },
            { ActorType.AuraSkill, new Color(0.5f, 0f, 1f) },
        };

        public static void Initialize()
        {
            if (isInitialized)
                return;

            try
            {
                cachedLineTexture = new Texture2D(1, 1);
                if (cachedLineTexture != null)
                    cachedLineTexture.filterMode = FilterMode.Point;

                isInitialized = true;
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"ESPManager.Initialize error: {ex.Message}");
            }
        }

        public static void UpdateESP()
        {
            if (!MainGUI.espEnabled || !isInitialized)
                return;

            mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastUpdateTime >= CACHE_UPDATE_INTERVAL)
            {
                UpdateActorCache();
                UpdateLootCache();
                lastUpdateTime = currentTime;
            }

            DrawActorESP();
            DrawLootESP();
        }

        private static void UpdateActorCache()
        {
            try
            {
                cachedActors.Clear();
                ProtoActor[] allActors = PlayerAPI.GetOtherPlayers();

                if (allActors == null || allActors.Length == 0)
                    return;

                Vector3 camPos = mainCamera.transform.position;

                foreach (ProtoActor actor in allActors)
                {
                    if (actor == null || !actor.gameObject.activeInHierarchy)
                        continue;

                    if (actor.ActorType == ActorType.None || actor.ActorType == ActorType.System)
                        continue;

                    float distance = Vector3.Distance(camPos, actor.transform.position);
                    if (distance <= MainGUI.espDistance)
                        cachedActors.Add(actor);
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"ESPManager.UpdateActorCache error: {ex.Message}");
            }
        }

        private static void UpdateLootCache()
        {
            try
            {
                cachedLootObjects.Clear();

                if (!MainGUI.espShowLoot)
                    return;

                LootingLevelObject[] allLoot = UnityEngine.Object.FindObjectsOfType<LootingLevelObject>();
                if (allLoot == null || allLoot.Length == 0)
                    return;

                Vector3 camPos = mainCamera.transform.position;

                foreach (LootingLevelObject loot in allLoot)
                {
                    if (loot == null || !loot.gameObject.activeInHierarchy)
                        continue;

                    float distance = Vector3.Distance(camPos, loot.transform.position);
                    if (distance <= MainGUI.espDistance)
                        cachedLootObjects.Add(loot);
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"ESPManager.UpdateLootCache error: {ex.Message}");
            }
        }

        private static void DrawActorESP()
        {
            foreach (ProtoActor actor in cachedActors)
            {
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    continue;

                if (!ShouldDisplayActorType(actor.ActorType))
                    continue;

                Vector3 screenPos = mainCamera.WorldToScreenPoint(actor.transform.position);
                if (screenPos.z <= 0)
                    continue;

                screenPos.y = Screen.height - screenPos.y;

                float distance = Vector3.Distance(mainCamera.transform.position, actor.transform.position);
                string label = ActorTypeLabels.ContainsKey(actor.ActorType) ? ActorTypeLabels[actor.ActorType] : "[UNKNOWN]";
                Color color = ActorTypeColors.ContainsKey(actor.ActorType) ? ActorTypeColors[actor.ActorType] : Color.white;
                string text = $"{label}\n{actor.nickName}\n[{distance:F0}m]";

                DrawESPText(screenPos, text, color);
            }
        }

        private static void DrawLootESP()
        {
            foreach (LootingLevelObject loot in cachedLootObjects)
            {
                if (loot == null || !loot.gameObject.activeInHierarchy)
                    continue;

                Vector3 screenPos = mainCamera.WorldToScreenPoint(loot.transform.position);
                if (screenPos.z <= 0)
                    continue;

                screenPos.y = Screen.height - screenPos.y;

                float distance = Vector3.Distance(mainCamera.transform.position, loot.transform.position);
                string itemName = CleanObjectName(loot.gameObject.name);
                string text = $"{itemName}\n[{distance:F0}m]";

                DrawESPText(screenPos, text, Color.white);
            }
        }

        private static bool ShouldDisplayActorType(ActorType type)
        {
            return type switch
            {
                ActorType.Player => MainGUI.espShowPlayers,
                ActorType.Monster => MainGUI.espShowMonsters,
                ActorType.Interactor => MainGUI.espShowInteractors,
                ActorType.NPC => MainGUI.espShowNPCs,
                ActorType.FieldSkill => MainGUI.espShowFieldSkills,
                ActorType.Projectile => MainGUI.espShowProjectiles,
                ActorType.AuraSkill => MainGUI.espShowAuraSkills,
                _ => false,
            };
        }

        private static void DrawESPText(Vector3 screenPos, string text, Color textColor)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)ESP_TEXT_SIZE,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                normal = { textColor = textColor },
            };

            Vector2 textSize = style.CalcSize(new GUIContent(text));
            Rect textRect = new Rect(screenPos.x - textSize.x / 2 - 5, screenPos.y - textSize.y / 2 - 3, textSize.x + 10, textSize.y + 6);

            GUI.Label(textRect, text, style);
        }

        private static string CleanObjectName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Item";

            name = System.Text.RegularExpressions.Regex.Replace(name, @"\(Clone\)", "");
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[Pp]refab", "");
            name = name.Replace("_", " ").Trim();

            return string.IsNullOrEmpty(name) ? "Item" : name;
        }

        public static void Cleanup()
        {
            cachedLootObjects.Clear();
            cachedActors.Clear();

            if (cachedLineTexture != null)
            {
                UnityEngine.Object.Destroy(cachedLineTexture);
                cachedLineTexture = null;
            }

            isInitialized = false;
        }
    }
}
