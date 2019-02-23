using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;

namespace ApokPT.RocketPlugins
{
    class CommandVEDrop : IRocketCommand
    {

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "vedrop";

        public string Help => "Will cause the elements to drop of the car that you're looking at.";

        public string Syntax => "";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>() { "wreckingball.vedrop" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            Ray ray = new Ray(player.Player.look.aim.position, player.Player.look.aim.forward);
            RaycastInfo raycastInfo = DamageTool.raycast(ray, 10f, RayMasks.VEHICLE);
            if (raycastInfo.vehicle != null)
            {
                bool getPInfo = false;
                if (WreckingBall.Instance.Configuration.Instance.EnablePlayerInfo && WreckingBall.isPlayerInfoLibPresent && WreckingBall.isPlayerInfoLibLoaded)
                    getPInfo = true;
                if (!raycastInfo.vehicle.isDead)
                {
                    ulong signOwner = 0;
                    bool showSignOwner = false;
                    showSignOwner = DestructionProcessing.HasFlaggedElement(raycastInfo.transform, out signOwner);
                    string signmsg = getPInfo ? WreckingBall.Instance.PInfoGenerateMessage(signOwner) : signOwner.ToString();
                    string lockedmsg = raycastInfo.vehicle.isLocked ? (!getPInfo || raycastInfo.vehicle.lockedOwner == CSteamID.Nil ? raycastInfo.vehicle.lockedOwner.ToString() : WreckingBall.Instance.PInfoGenerateMessage((ulong)raycastInfo.vehicle.lockedOwner)) : "N/A";
                    string msg = string.Format("Dropping elements off of vehicle: {0}({1}), InstanceID: {2}, Locked Owner: {3}, SignOwner {4}.", raycastInfo.vehicle.asset.name, raycastInfo.vehicle.id, raycastInfo.vehicle.instanceID,
                        lockedmsg,
                        showSignOwner ? signmsg : "N/A");
                    UnturnedChat.Say(caller, msg);
                    Logger.Log(msg);
                    WreckingBall.Instance.VehicleElementDrop(raycastInfo.vehicle, true, raycastInfo.transform);
                }
            }
            else
            {
                UnturnedChat.Say(caller, "Couldn't find a vehicle in the direction you're looking, or too far away, within 10 units.");
            }
        }
    }
}
