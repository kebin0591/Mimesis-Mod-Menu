using System;
using Mimic.Actors;
using MimicAPI.GameAPI;
using UnityEngine;

namespace Mimesis_Mod_Menu.Core.Features
{
    public class AutoLootManager : FeatureManager
    {
        private float distance = 50f;
        private const float MIN_DISTANCE = 10f;
        private const float MAX_DISTANCE = 200f;

        public float GetDistance() => distance;

        public void SetDistance(float value)
        {
            distance = Mathf.Clamp(value, MIN_DISTANCE, MAX_DISTANCE);
        }

        public override void Update()
        {
            if (!IsEnabled)
                return;

            try
            {
                ProtoActor localPlayer = PlayerAPI.GetLocalPlayer();
                if (localPlayer == null)
                    return;

                LootingLevelObject[] allLoot = LootAPI.GetAllLoot();
                Vector3 playerPos = localPlayer.transform.position;

                foreach (LootingLevelObject loot in allLoot)
                {
                    if (loot == null || !loot.gameObject.activeInHierarchy)
                        continue;

                    if (Vector3.Distance(playerPos, loot.transform.position) <= distance)
                        localPlayer.GrapLootingObject(loot.ActorID);
                }
            }
            catch (Exception ex)
            {
                LogError(nameof(Update), ex);
            }
        }
    }
}
