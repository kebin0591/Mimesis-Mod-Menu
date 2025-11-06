using BepInEx;
using fishmods;
using System;
using UnityEngine;


namespace fishmods
{
    [BepInPlugin("com.fishmods.plugin", "FishMods Plugin", "1.0.0")]
    public class FishModsPlugin : BaseUnityPlugin
    {
        private GameObject gui;

        void Awake()
        {
            AssemblyLoader.Init();
            if (gui != null)
                Destroy(gui);

            gui = new GameObject(nameof(MainGUI));
            gui.AddComponent<MainGUI>();
            DontDestroyOnLoad(gui);

        }
    }
}