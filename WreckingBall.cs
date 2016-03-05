using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Commands;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApokPT.RocketPlugins
{

    class Destructible
    {
        public Destructible(Transform transform, char type, InteractableVehicle vehicle = null, Zombie zombie = null)
        {
            Transform = transform;
            Type = type;
            Vehicle = vehicle;
            Zombie = zombie;
        }

        public Zombie Zombie { get; private set; }
        public InteractableVehicle Vehicle { get; private set; }
        public Transform Transform { get; private set; }
        public char Type { get; private set; }
    }

     class WreckingBall : RocketPlugin<WreckingBallConfiguration>
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
            Instance.Configuration.Save();
        }

        protected override void Unload()
        {
            if (processing)
            {
                if (originalCaller != null)
                    UnturnedChat.Say(originalCaller, Translate("wreckingball_reload_abort"), Color.yellow);
                Logger.LogWarning(Translate("wreckingball_reload_abort"));
                Abort();
            }
            ElementData = null;
        }

        [RocketCommand("wreck", "Destroy everything in a specific radius!", ".",AllowedCaller.Player)]
        [RocketCommandPermission("WreckingBall.wreck")]
        public void WreckExecute(IRocketPlayer caller, string[] cmd)
        {
            WreckingBallCommand.Execute(caller, cmd);
        }

        [RocketCommand("w", "Destroy everything in a specific radius!", ".", AllowedCaller.Player)]
        [RocketCommandPermission("WreckingBall.wreck")]
        public void WExecute(IRocketPlayer caller, string[] cmd)
        {
            WreckingBallCommand.Execute(caller, cmd);
        }

        private static List<Destructible> destroyList = new List<Destructible>();
        private static int dIdx = 0;
        private int dIdxCount = 0;
        internal static bool processing = false;
        private static UnturnedPlayer originalCaller;
        private DateTime lastRunTime;

        internal void Wreck(UnturnedPlayer player, string filter, uint radius, bool scan = false)
        {
            if (!scan)
            {
                if (processing)
                {
                    UnturnedChat.Say(player, Translate("wreckingball_processing", originalCaller != null ? originalCaller.CharacterName : "???", (dIdxCount - dIdx), CalcProcessTime()));
                    return;
                }
                Abort();
            }
            else
            {
                ElementData.reportList.Clear();
            }


            List<char> Filter = new List<char>();
            Filter.AddRange(filter.ToCharArray());

            ushort item = 0;
            float distance = 0;
            byte x;
            byte y;
            ushort index;
            ushort plant;
            StructureRegion structureRegion;
            BarricadeRegion barricadeRegion;

            for (int k = 0; k < StructureManager.StructureRegions.GetLength(0); k++)
            {
                for (int l = 0; l < StructureManager.StructureRegions.GetLength(1); l++)
                {
                    foreach (Transform current in StructureManager.StructureRegions[k, l].Structures)
                    {
                        distance = Vector3.Distance(current.position, player.Position);
                        if (distance < radius)
                        {
                            item = Convert.ToUInt16(current.name);
                            if (ElementData.filterItem(item, Filter) || Filter.Contains('*'))
                            {
                                if (scan)
                                {
                                    if (distance <= 10)
                                        try
                                        {
                                            ElementData.report(player, item, distance, true, StructureManager.tryGetInfo(current, out x, out y, out index, out structureRegion) ? structureRegion.structures[(int)index].owner : 0);
                                        }
                                        catch
                                        {
                                            UnturnedChat.Say(player, Translate("wreckingball_structure_array_sync_error"), Color.yellow);
                                            Logger.LogWarning(Translate("wreckingball_structure_array_sync_error"));
                                            ElementData.report(player, item, distance, false);
                                        }
                                    else
                                        ElementData.report(player, item, distance, false);
                                }
                                else
                                    destroyList.Add(new Destructible(current, 's'));
                            }
                        }
                    }
                }
            }
            
            for (int k = 0; k < BarricadeManager.BarricadeRegions.GetLength(0); k++)
            {
                for (int l = 0; l < BarricadeManager.BarricadeRegions.GetLength(1); l++)
                {
                    foreach (Transform current in BarricadeManager.BarricadeRegions[k, l].Barricades)
                    {
                        distance = Vector3.Distance(current.position, player.Position);
                        if (distance < radius)
                        {
                            item = Convert.ToUInt16(current.name);
                            if (ElementData.filterItem(item, Filter) || Filter.Contains('*'))
                            {
                                if (scan)
                                {
                                    if (distance <= 10)
                                        try
                                        {
                                            ElementData.report(player, item, distance, true, BarricadeManager.tryGetInfo(current, out x, out y, out plant, out index, out barricadeRegion) ? barricadeRegion.barricades[(int)index].owner : 0);
                                        }
                                        catch
                                        {
                                            UnturnedChat.Say(player, Translate("wreckingball_barricade_array_sync_error"), Color.yellow);
                                            Logger.LogWarning(Translate("wreckingball_barricade_array_sync_error"));
                                            ElementData.report(player, item, distance, false);
                                        }
                                    else
                                        ElementData.report(player, item, distance, false);
                                }
                                else
                                    destroyList.Add(new Destructible(current, 'b'));
                            }
                        }
                    }
                }
            }

            if (Filter.Contains('v') || Filter.Contains('*'))
            {
                foreach (InteractableVehicle vehicle in VehicleManager.Vehicles)
                {
                    distance = Vector3.Distance(vehicle.transform.position, player.Position);
                    if (distance < radius)
                    {
                        if (scan)
                            ElementData.report(player, 9999, (int)distance, false);
                        else
                            destroyList.Add(new Destructible(vehicle.transform, 'v', vehicle));
                    }
                }
            }

            if (Filter.Contains('z'))
            {
                for (int v = 0; v < ZombieManager.ZombieRegions.Length; v++)
                {

                    foreach (Zombie zombie in ZombieManager.ZombieRegions[v].Zombies)
                    {
                        distance = Vector3.Distance(zombie.transform.position, player.Position);
                        if (distance < radius)
                        {
                            if (scan)
                                ElementData.report(player, 9998, (int)distance, false);
                            else
                                destroyList.Add(new Destructible(zombie.transform, 'z', null, zombie));
                        }
                    }
                }
            }


            if (scan) return;

            if (destroyList.Count >= 1)
            {
                dIdxCount = destroyList.Count;
                Instruct(player);
            }
            else
                UnturnedChat.Say(player, Translate("wreckingball_not_found", radius));
        }

        internal void Scan(UnturnedPlayer caller, string filter, uint radius)
        {
            Wreck(caller, filter, radius, true);
            string report = "";
            uint totalCount = 0;
            if (ElementData.reportList.Count > 0)
            {

                foreach (KeyValuePair<char, uint> reportFilter in ElementData.reportList)
                {
                    report += " " + ElementData.categorys[reportFilter.Key].Name + ": " + reportFilter.Value + ",";
                    totalCount += reportFilter.Value;
                }
                if (report != "") report = report.Remove(report.Length - 1);
                UnturnedChat.Say(caller, Translate("wreckingball_scan", totalCount, radius, report));
                if (Instance.Configuration.Instance.LogScans)
                    Logger.Log(Translate("wreckingball_scan", totalCount, radius, report));
                else
                    UnturnedChat.Say(CSteamID.Nil, Translate("wreckingball_scan", totalCount, radius, report));
            }
            else
            {
                UnturnedChat.Say(caller, Translate("wreckingball_not_found", radius));
            }



        }

        internal void Teleport(UnturnedPlayer caller, bool toBarricades = false)
        {

            if (StructureManager.StructureRegions.LongLength == 0 && BarricadeManager.BarricadeRegions.LongLength == 0)
            {
                UnturnedChat.Say(caller, Translate("wreckingball_map_clear"));
                return;
            }

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

                    if (Vector3.Distance(current.position, caller.Position) > 20)
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

                    if (Vector3.Distance(current.position, caller.Position) > 20)
                        match = true;
                }
            }
            if(match)
            {
                tpVector = new Vector3(current.position.x, current.position.y + 2, current.position.z);
                caller.Teleport(tpVector, caller.Rotation);
                return;
            }
            UnturnedChat.Say(caller, Translate("wreckingball_teleport_not_found"));
        }

        private void Instruct(UnturnedPlayer caller)
        {
            UnturnedChat.Say(caller, Translate("wreckingball_queued", dIdxCount, CalcProcessTime()));
            UnturnedChat.Say(caller, Translate("wreckingball_prompt"));
        }

        internal void Confirm(UnturnedPlayer caller)
        {
            if (destroyList.Count <= 0)
            {
                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
            }
            else
            {
                processing = true;
                originalCaller = caller;
                UnturnedChat.Say(caller, Translate("wreckingball_initiated", CalcProcessTime()));
                dIdxCount = destroyList.Count;
                dIdx = 0;
            }
        }

        private double CalcProcessTime()
        {
            return Math.Round(((dIdxCount - dIdx) * (1 / (Instance.Configuration.Instance.DestructionRate * Instance.Configuration.Instance.DestructionsPerInterval))), 2);
        }

        internal void Abort()
        {
            processing = false;
            destroyList.Clear();
            dIdx = 0;
            dIdxCount = 0;

        }

        // Changed timer to Update(), to attempt to fix ghost objects bug by syncing the destructions to the game frame/tic.
        public void Update()
        {
            if (processing)
            {
                if ((DateTime.Now - lastRunTime).TotalSeconds > (1 / Instance.Configuration.Instance.DestructionRate))
                {
                    lastRunTime = DateTime.Now;
                    try
                    {
                        int i = 0;
                        while (dIdx < dIdxCount && i < Instance.Configuration.Instance.DestructionsPerInterval)
                        {

                            if (destroyList[dIdx].Type == 's')
                            {
                                try { StructureManager.damage(destroyList[dIdx].Transform, destroyList[dIdx].Transform.position, 65535, 1, false); }
                                catch { }
                            }

                            else if (destroyList[dIdx].Type == 'b')
                            {
                                try { BarricadeManager.damage(destroyList[dIdx].Transform, 65535, 1, false); }
                                catch { }
                            }

                            else if (destroyList[dIdx].Type == 'v')
                            {
                                try { destroyList[dIdx].Vehicle.askDamage(65535, true); }
                                catch { }
                            }
                            else if (destroyList[dIdx].Type == 'z')
                            {
                                EPlayerKill pKill;
                                try
                                {
                                    for (int j = 0; j < 100 && !destroyList[dIdx].Zombie.isDead; j++)
                                        destroyList[dIdx].Zombie.askDamage(255, destroyList[dIdx].Zombie.transform.up, out pKill);
                                }
                                catch { }
                            }
                            dIdx++;
                            i++;
                        }
                        if (destroyList.Count == dIdx)
                        {
                            if (originalCaller != null)
                                UnturnedChat.Say(originalCaller, Translate("wreckingball_complete", dIdx));
                            StructureManager.save();
                            BarricadeManager.save();
                            Abort();
                        }
                    }
                    catch
                    {
                        throw;
                    }
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
                    { "wreckingball_scan", "Found {0} elements @ {1}m:{2}" },
                    { "wreckingball_map_clear", "Map has no elements!" },
                    { "wreckingball_not_found", "No elements found in a {0} radius!" },
                    { "wreckingball_complete", "Wrecking Ball complete! {0} elements(s) Destroyed!" },
                    { "wreckingball_initiated", "Wrecking Ball initiated: ~{0} sec(s) left." },
                    { "wreckingball_processing", "Wrecking Ball started by: {0}, {1} element(s) left to destroy, ~{2} sec(s) left." },
                    { "wreckingball_aborted", "Wrecking Ball Aborted! Destruction queue cleared!" },
                    { "wreckingball_help", "Please define filter and radius: /wreck <filter> <radius> or /wreck teleport b|s" },
                    { "wreckingball_help_teleport", "Please define type for teleport: /wreck teleport s|b" },
                    { "wreckingball_help_scan", "Please define a scan filter and radius: /wreck scan <filter> <radius>" },
                    { "wreckingball_queued", "{0} elements(s) found, ~{1} sec(s) to complete run." },
                    { "wreckingball_prompt", "Type '/wreck confirm' or '/wreck abort'" },
                    { "wreckingball_structure_array_sync_error", "Warning: Structure arrays out of sync, need to restart server." },
                    { "wreckingball_barricade_array_sync_error", "Warning: Barricade arrays out of sync, need to restart server." },
                    { "wreckingball_teleport_not_found", "Couldn't find any elements to teleport to, try to run the command again." },
                    { "wreckingball_reload_abort", "Warning: Current wreck job in progress has been aborted from a plugin reload." }
                };
            }
        }
    }
}
