using System;
using System.Collections.Generic;
using UnityEngine;

public class ESPMain
{
    private static Texture2D cachedLineTexture;
    private static Camera mainCamera;
    private static List<LootingLevelObject> cachedLootObjects = new List<LootingLevelObject>();
    private static Dictionary<int, int> priceCache = new Dictionary<int, int>();
    private static float lastUpdateTime = 0f;
    private const float CACHE_UPDATE_INTERVAL = 0.5f;
    private static bool isInitialized = false;

    public static void Initialize()
    {
        if (isInitialized) return;

        cachedLineTexture = new Texture2D(1, 1);
        cachedLineTexture.filterMode = FilterMode.Point;
        UpdateColors();

        isInitialized = true;
    }

    public static void UpdateESP()
    {
        if (!MainGUI.espEnabled || !isInitialized) return;

        mainCamera = Camera.main;
        if (mainCamera == null) return;

        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - lastUpdateTime >= CACHE_UPDATE_INTERVAL)
        {
            UpdateLootCache();
            lastUpdateTime = currentTime;
        }

        DrawLootESP();
    }

    private static void UpdateLootCache()
    {
        cachedLootObjects.Clear();
        LootingLevelObject[] allLoot = UnityEngine.Object.FindObjectsOfType<LootingLevelObject>();

        if (allLoot == null || allLoot.Length == 0) return;

        Vector3 camPos = mainCamera.transform.position;

        foreach (var loot in allLoot)
        {
            if (loot == null || !loot.gameObject.activeInHierarchy) continue;

            float distance = Vector3.Distance(camPos, loot.transform.position);
            if (distance <= MainGUI.espDistance)
            {
                cachedLootObjects.Add(loot);
            }
        }
    }

    private static void DrawLootESP()
    {
        if (mainCamera == null) return;

        foreach (var loot in cachedLootObjects)
        {
            if (loot == null || !loot.gameObject.activeInHierarchy) continue;

            Vector3 worldPos = loot.transform.position;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0) continue;

            screenPos.y = Screen.height - screenPos.y;

            float distance = Vector3.Distance(mainCamera.transform.position, worldPos);
            string itemName = CleanObjectName(loot.gameObject.name);

            string priceText = ""; // not setup
            string displayText = $"{itemName}\n{priceText} [{distance:F0}m]";

            DrawESPText(screenPos, displayText);
        }
    }


    private static void DrawESPText(Vector3 screenPos, string text)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = MainGUI.espColor;
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = false;

        Vector2 textSize = style.CalcSize(new GUIContent(text));
        Rect textRect = new Rect(screenPos.x - textSize.x / 2 - 5, screenPos.y - textSize.y / 2 - 3, textSize.x + 10, textSize.y + 6);

        GUI.Label(textRect, text, style);
    }


    private static string CleanObjectName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Item";

        name = System.Text.RegularExpressions.Regex.Replace(name, @"\(Clone\)", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"prefab", "");
        name = name.Replace("_", " ");
        name = name.Trim();

        return string.IsNullOrEmpty(name) ? "Item" : name;
    }

    public static void UpdateColors()
    {
        if (cachedLineTexture != null)
        {
            cachedLineTexture.SetPixel(0, 0, MainGUI.espColor);
            cachedLineTexture.Apply();
        }
    }

    public static void Cleanup()
    {
        cachedLootObjects.Clear();
        priceCache.Clear();

        if (cachedLineTexture != null)
        {
            UnityEngine.Object.Destroy(cachedLineTexture);
            cachedLineTexture = null;
        }

        isInitialized = false;
    }
}