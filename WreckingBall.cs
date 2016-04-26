using PlayerInfoLibrary;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.API.Extensions;
using Rocket.Core.Commands;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApokPT.RocketPlugins
{
    public class WreckingBall : RocketPlugin<WreckingBallConfiguration>
    {
        // Singleton
        public static WreckingBall Instance;
        public static ElementDataManager ElementData;

        protected override void Load()
        {
            Instance = this;
            Instance.Configuration.Instance.LoadDefaults();
            ElementData = new ElementDataManager();
            if (Instance.Configuration.Instance.DestructionRate <= 0)
            {
                Instance.Configuration.Instance.DestructionRate = 1;
                Logger.LogWarning("Error: DestructionRate config value must be above 0.");
            }
            if (Instance.Configuration.Instance.DestructionsPerInterval < 1)
            {
                Instance.Configuration.Instance.DestructionsPerInterval = 1;
                Logger.LogWarning("Error: DestructionsPerInterval config value must be at or above 1.");
            }
            if (Instance.Configuration.Instance.EnablePlayerInfo || Instance.Configuration.Instance.EnableCleanup)
            {
                // Check to see whether the PlayerInfoLib plugin is present on this server.
                if (!CheckPlayerInfoLib())
                {
                    Logger.LogWarning("The Player Info Library plugin isn't loaded on this server, setting related options to false.");
                    Instance.Configuration.Instance.EnablePlayerInfo = false;
                    Instance.Configuration.Instance.EnableCleanup = false;
                }
                else
                {
                    CheckCleanup();
                }
            }
            Instance.Configuration.Save();
        }

        protected override void Unload()
        {
            if (DestructionProcessing.processing)
            {
                if (DestructionProcessing.originalCaller != null)
                    UnturnedChat.Say(DestructionProcessing.originalCaller, Translate("wreckingball_reload_abort"), Color.yellow);
                Logger.LogWarning(Translate("wreckingball_reload_abort"));
                DestructionProcessing.Abort(WreckType.Wreck);
            }
            if (DestructionProcessing.cleanupProcessingBuildables || DestructionProcessing.cleanupProcessingFiles)
            {
                DestructionProcessing.Abort(WreckType.Cleanup);
            }
            ElementData = null;
        }

        private static void CheckCleanup()
        {
            if (Instance.Configuration.Instance.EnableCleanup && DatabaseManager.DatabaseInterfaceVersion < 2)
            {
                Logger.LogWarning("The Player Info Library is outdated, the WreckingBall cleanup feature in this plugin will be disabled.");
                Instance.Configuration.Instance.EnableCleanup = false;
            }
            else if (Instance.Configuration.Instance.EnableCleanup)
            {
                if (Instance.Configuration.Instance.BuildableWaitTime < 1)
                {
                    Instance.Configuration.Instance.BuildableWaitTime = 30;
                }
                if (Instance.Configuration.Instance.PlayerDataWaitTime < 1)
                {
                    Instance.Configuration.Instance.PlayerDataWaitTime = 45;
                }
                if (Instance.Configuration.Instance.CleanupIntervalTime < 1)
                {
                    Instance.Configuration.Instance.CleanupIntervalTime = 5;
                }
                if (Instance.Configuration.Instance.CleanupPerInterval < 1)
                {
                    Instance.Configuration.Instance.CleanupPerInterval = 10;
                }
            }
        }

        private static bool CheckPlayerInfoLib()
        {
            return Type.GetType("PlayerInfoLibrary.DatabaseManager,PlayerInfoLib") != null;
        }

        internal static bool IsPInfoLibLoaded()
        {
            return (PlayerInfoLib.Instance.State == PluginState.Loaded && PlayerInfoLib.Database.Initialized);
        }

        [RocketCommand("wreck", "Destroy everything in a specific radius!", ".",AllowedCaller.Both)]
        [RocketCommandPermission("wreck")]
        public void WreckExecute(IRocketPlayer caller, string[] cmd)
        {
            WreckingBallCommand.Execute(caller, cmd);
        }

        [RocketCommand("w", "Destroy everything in a specific radius!", ".", AllowedCaller.Both)]
        [RocketCommandPermission("wreck")]
        public void WExecute(IRocketPlayer caller, string[] cmd)
        {
            WreckingBallCommand.Execute(caller, cmd);
        }

        [RocketCommand("listvehicles", "lists positions and barricade counts on cars on a map.", ".", AllowedCaller.Both)]
        [RocketCommandPermission("listvehicles")]
        public void LVExecute(IRocketPlayer caller, string[] cmd)
        {
            foreach (InteractableVehicle vehicle in VehicleManager.Vehicles)
            {
                byte x = 0;
                byte y = 0;
                ushort plant = 0;
                BarricadeRegion barricadeRegion;
                UnturnedPlayer player = null;
                float radius = 0;
                if (!(caller is ConsolePlayer))
                {
                    if (cmd.GetFloatParameter(0) == null)
                    {
                        UnturnedChat.Say(caller, "<radius> - distance to scan cars.");
                        return;
                    }
                    player = (UnturnedPlayer)caller;
                    radius = (float)cmd.GetFloatParameter(0);
                }
                if (caller is ConsolePlayer || Vector3.Distance(vehicle.transform.position, player.Position) < radius)
                    if (BarricadeManager.tryGetPlant(vehicle.transform, out x, out y, out plant, out barricadeRegion))
                    {
                        if (!(caller is ConsolePlayer))
                            UnturnedChat.Say(caller, "Vehicle position: " + vehicle.transform.position.ToString() + ", Barricade count on car: " + barricadeRegion.Barricades.Count + ".", Color.yellow);
                        Logger.Log("Vehicle position: " + vehicle.transform.position.ToString() + ", Barricade count on car: " + barricadeRegion.Barricades.Count + ".", ConsoleColor.Yellow);
                    }
                    else
                    {
                        if (!(caller is ConsolePlayer))
                            UnturnedChat.Say(caller, "Vehicle position: " + vehicle.transform.position.ToString() + ", Barricade count on car: 0.", Color.yellow);
                        Logger.Log("Vehicle position: " + vehicle.transform.position.ToString() + ", Barricade count on car: 0.", ConsoleColor.Yellow);
                    }
            }
        }

        internal void Scan(IRocketPlayer caller, string filter, uint radius, Vector3 position, FlagType flagType, ulong steamID, ushort itemID)
        {
            DestructionProcessing.Wreck(caller, filter, radius, position, WreckType.Scan, flagType, steamID, itemID);
            if (ElementData.reportLists[BuildableType.Element].Count > 0 || ElementData.reportLists[BuildableType.VehicleElement].Count > 0)
            {
                foreach (KeyValuePair<BuildableType, Dictionary<char, uint>> reportDictionary in ElementData.reportLists)
                {
                    if (reportDictionary.Value.Count == 0)
                        continue;
                    string report = "";
                    uint totalCount = 0;
                    foreach (KeyValuePair<char, uint> reportFilter in reportDictionary.Value)
                    {
                        report += " " + ElementData.categorys[reportFilter.Key].Name + ": " + reportFilter.Value + ",";
                        totalCount += reportFilter.Value;
                    }
                    if (report != "") report = report.Remove(report.Length - 1);
                    string type = reportDictionary.Key == BuildableType.VehicleElement ? "Vehicle Element" : "Element";
                    UnturnedChat.Say(caller, Translate("wreckingball_scan", totalCount, type, radius, report));
                    if (Instance.Configuration.Instance.LogScans && !(caller is ConsolePlayer))
                        Logger.Log(Translate("wreckingball_scan", totalCount, type, radius, report));
                }
            }
            else
            {
                UnturnedChat.Say(caller, Translate("wreckingball_not_found", radius));
            }



        }

        internal void Teleport(IRocketPlayer caller, bool toBarricades = false)
        {

            if (StructureManager.StructureRegions.LongLength == 0 && BarricadeManager.BarricadeRegions.LongLength == 0)
            {
                UnturnedChat.Say(caller, Translate("wreckingball_map_clear"));
                return;
            }

            UnturnedPlayer player = (UnturnedPlayer)caller;

            Vector3 tpVector;
            bool match = false;
            int tries = 0;

            Transform current = null;

            while (tries < 2000 && !match)
            {
                tries++;
                int x = 0;
                int xCount = 0;
                int z = 0;
                int zCount = 0;
                int idx = 0;
                int idxCount = 0;
                if (!toBarricades)
                {
                    xCount = StructureManager.StructureRegions.GetLength(0);
                    zCount = StructureManager.StructureRegions.GetLength(1);
                    if (xCount == 0)
                        continue;
                    x = UnityEngine.Random.Range(0, xCount - 1);
                    if (zCount == 0)
                        continue;
                    z = UnityEngine.Random.Range(0, zCount - 1);
                    idxCount = StructureManager.StructureRegions[x, z].Structures.Count;
                    if (idxCount == 0)
                        continue;
                    idx = UnityEngine.Random.Range(0, idxCount - 1);

                    try
                    {
                        current = StructureManager.StructureRegions[x, z].Structures[idx];
                    }
                    catch
                    {
                        continue;
                    }

                    if (Vector3.Distance(current.position, player.Position) > 20)
                        match = true;
                }
                else
                {
                    xCount = BarricadeManager.BarricadeRegions.GetLength(0);
                    zCount = BarricadeManager.BarricadeRegions.GetLength(1);
                    if (xCount == 0)
                        continue;
                    x = UnityEngine.Random.Range(0, xCount - 1);
                    if (zCount == 0)
                        continue;
                    z = UnityEngine.Random.Range(0, zCount - 1);
                    idxCount = BarricadeManager.BarricadeRegions[x, z].Barricades.Count;
                    if (idxCount == 0)
                        continue;
                    idx = UnityEngine.Random.Range(0, idxCount - 1);

                    try
                    {
                        current = BarricadeManager.BarricadeRegions[x, z].Barricades[idx];
                    }
                    catch
                    {
                        continue;
                    }

                    if (Vector3.Distance(current.position, player.Position) > 20)
                        match = true;
                }
            }
            if(match)
            {
                tpVector = new Vector3(current.position.x, current.position.y + 2, current.position.z);
                player.Teleport(tpVector, player.Rotation);
                return;
            }
            UnturnedChat.Say(caller, Translate("wreckingball_teleport_not_found"));
        }

        internal void Instruct(IRocketPlayer caller)
        {
            UnturnedChat.Say(caller, Translate("wreckingball_queued", DestructionProcessing.dIdxCount, DestructionProcessing.CalcProcessTime()));
            if (DestructionProcessing.syncError)
                UnturnedChat.Say(caller, Translate("wreckingball_sync_error"));
            UnturnedChat.Say(caller, Translate("wreckingball_prompt"));
        }

        internal void Confirm(IRocketPlayer caller)
        {
            if (DestructionProcessing.destroyList.Count <= 0)
            {
                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
            }
            else
            {
                DestructionProcessing.processing = true;
                if (!(caller is ConsolePlayer))
                    DestructionProcessing.originalCaller = (UnturnedPlayer)caller;
                UnturnedChat.Say(caller, Translate("wreckingball_initiated", DestructionProcessing.CalcProcessTime()));
                DestructionProcessing.dIdxCount = DestructionProcessing.destroyList.Count;
                DestructionProcessing.dIdx = 0;
            }
        }

        // Changed timer to Update(), to attempt to fix ghost objects bug by syncing the destructions to the game frame/tic.
        public void Update()
        {
            if (Instance.State == PluginState.Loaded)
            {
                DestructionProcessing.DestructionLoop(WreckType.Wreck);
                DestructionProcessing.DestructionLoop(WreckType.Cleanup);
                if (Instance.Configuration.Instance.EnableCleanup)
                {
                    DestructionProcessing.HandleCleanup();
                }
            }
        }

        // Translations
        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    { "wreckingball_scan", "Found {0} elements of type: {1}, @ {2}m:{3}" },
                    { "wreckingball_map_clear", "Map has no elements!" },
                    { "wreckingball_not_found", "No elements found in a {0} radius!" },
                    { "wreckingball_complete", "Wrecking Ball complete! {0} elements(s) Destroyed!" },
                    { "wreckingball_initiated", "Wrecking Ball initiated: ~{0} sec(s) left." },
                    { "wreckingball_processing", "Wrecking Ball started by: {0}, {1} element(s) left to destroy, ~{2} sec(s) left." },
                    { "wreckingball_aborted", "Wrecking Ball Aborted! Destruction queue cleared!" },
                    { "wreckingball_help", "Please define filter and radius: /wreck <filter> <radius> or /wreck teleport b|s" },
                    { "wreckingball_help_console", "Please define filter, radius and position: /wreck <filter> <radius> <x> <y> <z>" },
                    { "wreckingball_help_teleport", "Please define type for teleport: /wreck teleport s|b" },
                    { "wreckingball_help_scan", "Please define a scan filter and radius: /wreck scan <filter> <radius>" },
                    { "wreckingball_help_scan_console", "Please define a scan filter, radius and position: /wreck scan <filter> <radius> <x> <y> <z>" },
                    { "wreckingball_queued", "{0} elements(s) found, ~{1} sec(s) to complete run." },
                    { "wreckingball_prompt", "Type '/wreck confirm' or '/wreck abort'" },
                    { "wreckingball_structure_array_sync_error", "Warning: Structure arrays out of sync, need to restart server." },
                    { "wreckingball_barricade_array_sync_error", "Warning: Barricade arrays out of sync, need to restart server." },
                    { "wreckingball_sync_error", "Warning: Element array sync error, not all elements will be cleaned up in range, server should be restarted." },
                    { "wreckingball_teleport_not_found", "Couldn't find any elements to teleport to, try to run the command again." },
                    { "wreckingball_teleport_not_allowed", "Not allowed to use wreck teleport from the console." },
                    { "wreckingball_reload_abort", "Warning: Current wreck job in progress has been aborted from a plugin reload." }
                };
            }
        }
    }
}
