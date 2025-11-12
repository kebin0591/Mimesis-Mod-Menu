using System;
using System.Collections.Generic;
using Mimic.Actors;
using MimicAPI.GameAPI;
using UnityEngine;

namespace Mimesis_Mod_Menu.Core.Features
{
    public class PickupManager : FeatureManager
    {
        private Queue<LootingLevelObject> pickupQueue = new Queue<LootingLevelObject>();
        public bool isActive => IsEnabled;
        private float pickupCooldown;
        private const float PICKUP_COOLDOWN = 0.05f;

        public override void Update()
        {
            if (!IsEnabled || pickupQueue.Count == 0)
                return;

            pickupCooldown -= Time.deltaTime;
            if (pickupCooldown <= 0f)
                ProcessNextPickup();
        }

        public void StartPickupAll()
        {
            try
            {
                pickupQueue.Clear();
                LootingLevelObject[] allLoot = LootAPI.GetAllLoot();

                if (allLoot.Length == 0)
                {
                    LogMessage("No items to pick up");
                    return;
                }

                foreach (var loot in allLoot)
                    pickupQueue.Enqueue(loot);

                IsEnabled = true;
                pickupCooldown = PICKUP_COOLDOWN;
                LogMessage($"Starting pickup of {pickupQueue.Count} items");
            }
            catch (Exception ex)
            {
                LogError(nameof(StartPickupAll), ex);
            }
        }

        public void Stop()
        {
            IsEnabled = false;
            pickupQueue.Clear();
            LogMessage("Pickup stopped");
        }

        private void ProcessNextPickup()
        {
            if (pickupQueue.Count == 0)
            {
                IsEnabled = false;
                LogMessage("Pickup complete");
                return;
            }

            try
            {
                ProtoActor localPlayer = PlayerAPI.GetLocalPlayer();
                if (localPlayer == null)
                {
                    Stop();
                    return;
                }

                LootingLevelObject loot = pickupQueue.Dequeue();
                if (loot != null && loot.gameObject.activeInHierarchy)
                    localPlayer.GrapLootingObject(loot.ActorID);

                pickupCooldown = PICKUP_COOLDOWN;
            }
            catch (Exception ex)
            {
                LogError(nameof(ProcessNextPickup), ex);
                pickupCooldown = PICKUP_COOLDOWN;
            }
        }
    }
}
