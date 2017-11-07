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
                bool doRun = (caller is ConsolePlayer || 
                    (vehicle.asset.engine == EEngine.TRAIN && vehicle.trainCars != null && vehicle.trainCars.Length > 1 && vehicle.trainCars.FirstOrDefault(car => Vector3.Distance(car.root.transform.position, player.Position) <= radius) != null) || 
                    Vector3.Distance(vehicle.transform.position, player.Position) <= radius);
                if (doRun)
                {
                    bool getPInfo = false;
                    if (WreckingBall.Instance.Configuration.Instance.EnablePlayerInfo)
                        getPInfo = WreckingBall.IsPInfoLibLoaded();
                    string lockedBy = getPInfo ? WreckingBall.Instance.PInfoGenerateMessage((ulong)vehicle.lockedOwner) : vehicle.lockedOwner.ToString();
                    ulong signOwner = 0;
                    string signBy = string.Empty;
                    bool showSignBy = false;
                    if (BarricadeManager.tryGetPlant(vehicle.transform, out x, out y, out plant, out barricadeRegion))
                        count = barricadeRegion.drops.Count;
                    if (caller is ConsolePlayer || Vector3.Distance(vehicle.transform.position, player.Position) <= radius)
                    {
                        showSignBy = DestructionProcessing.HasFlaggedElement(vehicle.transform, WreckingBall.Instance.Configuration.Instance.VehicleSignFlag, out signOwner);
                        if (showSignBy)
                            signBy = getPInfo ? WreckingBall.Instance.PInfoGenerateMessage(signOwner) : signOwner.ToString();
                        ProcessMessages(caller, vehicle.transform, vehicle.asset, vehicle.instanceID, count, lockedBy, vehicle.isLocked, signBy, showSignBy);
                    }
                    // Handle train cars too, if in range.
                    if (vehicle.asset.engine == EEngine.TRAIN && vehicle.trainCars != null && vehicle.trainCars.Length > 1)
                    {
                        for (int i = 1; i < vehicle.trainCars.Length; i++)
                        {
                            if (caller is ConsolePlayer || Vector3.Distance(vehicle.trainCars[i].root.transform.position, player.Position) <= radius)
                            {
                                if (BarricadeManager.tryGetPlant(vehicle.trainCars[i].root, out x, out y, out plant, out barricadeRegion))
                                    count = barricadeRegion.drops.Count;
                                showSignBy = DestructionProcessing.HasFlaggedElement(vehicle.trainCars[i].root, WreckingBall.Instance.Configuration.Instance.VehicleSignFlag, out signOwner);
                                if (showSignBy)
                                    signBy = getPInfo ? WreckingBall.Instance.PInfoGenerateMessage(signOwner) : signOwner.ToString();
                                ProcessMessages(caller, vehicle.trainCars[i].root, null, vehicle.instanceID, count, lockedBy, false, signBy, showSignBy, true, i);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessMessages(IRocketPlayer caller, Transform transform, Asset asset, uint instanceID, int count, string lockedBy, bool isLocked, string signBy, bool showSignBy, bool isTrainCar = false, int trainCarId = 0)
        {
            string msg = string.Empty;
            if (!isTrainCar)
                msg = WreckingBall.Instance.Translate("wreckingball_lv2_vehicle", transform.position.ToString(), instanceID, count, showSignBy ? signBy : "N/A", isLocked ? lockedBy : "N/A", ((VehicleAsset)asset).vehicleName, asset.id);
            else
                msg = WreckingBall.Instance.Translate("wreckingball_lv2_traincar", transform.position.ToString(), instanceID, count, showSignBy ? signBy : "N/A", "N/A", trainCarId);
            if (!(caller is ConsolePlayer))
                UnturnedChat.Say(caller, msg, Color.yellow);
            Logger.Log(msg, ConsoleColor.Yellow);
        }
    }
}
