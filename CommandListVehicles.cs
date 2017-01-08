using Rocket.API;
using Rocket.API.Extensions;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;

namespace ApokPT.RocketPlugins
{
    class CommandListVehicles : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>() { "listv" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return "lists positions and barricade counts on cars on a map."; }
        }

        public string Name
        {
            get { return "listvehicles"; }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "wreckingball.listvehicles" }; }
        }

        public string Syntax
        {
            get { return "In-game only: <radius>"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            float radius = 0;
            UnturnedPlayer player = null;
            if (!(caller is ConsolePlayer))
            {
                if (command.GetFloatParameter(0) == null)
                {
                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_lv_help"));
                    return;
                }
                player = (UnturnedPlayer)caller;
                radius = (float)command.GetFloatParameter(0);
            }
            foreach (InteractableVehicle vehicle in VehicleManager.vehicles)
            {
                byte x = 0;
                byte y = 0;
                ushort plant = 0;
                int count = 0;
                BarricadeRegion barricadeRegion;
                if (caller is ConsolePlayer || Vector3.Distance(vehicle.transform.position, player.Position) < radius)
                {
                    bool getPInfo = false;
                    if (WreckingBall.Instance.Configuration.Instance.EnablePlayerInfo)
                        getPInfo = WreckingBall.IsPInfoLibLoaded();
                    string locked = getPInfo ? WreckingBall.Instance.PInfoGenerateMessage((ulong)vehicle.lockedOwner) : vehicle.lockedOwner.ToString();
                    string msg = string.Empty;
                    if (vehicle)
                        if (BarricadeManager.tryGetPlant(vehicle.transform, out x, out y, out plant, out barricadeRegion))
                            count = barricadeRegion.drops.Count;
                    if (!vehicle.isLocked)
                        msg = WreckingBall.Instance.Translate("wreckingball_lv_vehicle", vehicle.transform.position.ToString(), vehicle.instanceID, count);
                    else
                        msg = WreckingBall.Instance.Translate("wreckingball_lv_vehicle_locked", vehicle.transform.position.ToString(), vehicle.instanceID, count, locked);
                    if (!(caller is ConsolePlayer))
                        UnturnedChat.Say(caller, msg, Color.yellow);
                    Logger.Log(msg, ConsoleColor.Yellow);
                }
            }
        }
    }
}
