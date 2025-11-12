using System;
using Mimic.Actors;
using MimicAPI.GameAPI;
using UnityEngine;

namespace Mimesis_Mod_Menu.Core.Features
{
    public class MovementManager : FeatureManager
    {
        private const float TELEPORT_OFFSET = 1.5f;

        public override void Update() { }

        public void TeleportForward(float distance)
        {
            try
            {
                ProtoActor localPlayer = PlayerAPI.GetLocalPlayer();
                if (localPlayer == null)
                    return;

                Vector3 newPos = localPlayer.transform.position + localPlayer.transform.forward * distance;
                localPlayer.Teleport(newPos, localPlayer.transform.eulerAngles, false);
                LogMessage($"Teleported forward {distance}u");
            }
            catch (Exception ex)
            {
                LogError(nameof(TeleportForward), ex);
            }
        }

        public void TeleportToPlayer(ProtoActor targetPlayer)
        {
            try
            {
                if (targetPlayer == null)
                    return;

                ProtoActor localPlayer = PlayerAPI.GetLocalPlayer();
                if (localPlayer == null)
                    return;

                Vector3 targetPos = targetPlayer.transform.position + Vector3.back * TELEPORT_OFFSET;
                localPlayer.Teleport(targetPos, targetPlayer.transform.eulerAngles, false);
                LogMessage($"Teleported to {targetPlayer.nickName}");
            }
            catch (Exception ex)
            {
                LogError(nameof(TeleportToPlayer), ex);
            }
        }

        public void TeleportPlayerToSelf(ProtoActor targetPlayer)
        {
            try
            {
                if (targetPlayer == null)
                    return;

                ProtoActor localPlayer = PlayerAPI.GetLocalPlayer();
                if (localPlayer == null || targetPlayer.ActorID == localPlayer.ActorID)
                    return;

                Vector3 playerPos = localPlayer.transform.position + Vector3.back * TELEPORT_OFFSET;
                targetPlayer.Teleport(playerPos, localPlayer.transform.eulerAngles, false);
                LogMessage($"Teleported {targetPlayer.nickName} to you");
            }
            catch (Exception ex)
            {
                LogError(nameof(TeleportPlayerToSelf), ex);
            }
        }
    }
}
