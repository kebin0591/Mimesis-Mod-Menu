using MelonLoader;
using MIMESIS;
using UnityEngine;

[assembly: MelonInfo(typeof(MIMESIS.Loader), "ModMenu", "1.4.0", "notfishvr")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MIMESIS
{
    public class Loader : MelonMod
    {
        private GameObject gui;

        public override void OnInitializeMelon() { }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (gui == null)
            {
                gui = new GameObject(nameof(MainGUI));
                gui.AddComponent<MainGUI>();
                GameObject.DontDestroyOnLoad(gui);
            }
        }
    }
}
