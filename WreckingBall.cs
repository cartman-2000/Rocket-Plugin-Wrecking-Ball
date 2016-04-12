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
            if (Instance.Configuration.Instance.EnablePlayerInfo)
            {
                // Check to see whether the PlayerInfoLib plugin is present on this server.
                if (!CheckPlayerInfoLib())
                {
                    Logger.LogWarning("The Player Info Library plugin isn't on this server, setting option to false.");
                    Instance.Configuration.Instance.EnablePlayerInfo = false;
                }
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

        internal static bool CheckPlayerInfoLib()
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

        private static List<Destructible> destroyList = new List<Destructible>();
        private static int dIdx = 0;
        private int dIdxCount = 0;
        internal static bool processing = false;
        private static UnturnedPlayer originalCaller = null;
        private DateTime lastRunTime;

        internal void Wreck(IRocketPlayer caller, string filter, uint radius, Vector3 position, WreckType type = WreckType.Wreck)
        {
            bool pInfoLibLoaded = false;
            if (type == WreckType.Wreck)
            {
                if (processing)
                {
                    UnturnedChat.Say(caller, Translate("wreckingball_processing", originalCaller != null ? originalCaller.CharacterName : "???", (dIdxCount - dIdx), CalcProcessTime()));
                    return;
                }
                Abort();
            }
            else
            {
                ElementData.reportLists[BuildableType.Element].Clear();
                ElementData.reportLists[BuildableType.VehicleElement].Clear();
                if (Instance.Configuration.Instance.EnablePlayerInfo)
                {
                    pInfoLibLoaded = IsPInfoLibLoaded();
                }
            }
            UnturnedPlayer Player = null;
            if (!(caller is ConsolePlayer))
            {
                Player = (UnturnedPlayer)caller;
                if (Player.IsInVehicle)
                    position = Player.CurrentVehicle.transform.position;
                else
                    position = Player.Position;
            }

            List<char> Filter = new List<char>();
            Filter.AddRange(filter.ToCharArray());

            ushort item = 0;
            float distance = 0;
            byte x;
            byte y;
            ushort plant;

            Transform transform;
            int transformCount = 0;

            StructureRegion structureRegion;
            BarricadeRegion barricadeRegion;
            StructureData sData;
            BarricadeData bData;
            int DataCount = 0;

            for (int k = 0; k < StructureManager.StructureRegions.GetLength(0); k++)
            {
                for (int l = 0; l < StructureManager.StructureRegions.GetLength(1); l++)
                {
                    // check to see if the region is out of range, skip if it is.
                    if (position.RegionOutOfRange(k, l, radius))
                        continue;

                    structureRegion = StructureManager.StructureRegions[k, l];
                    transformCount = structureRegion.Structures.Count;
                    DataCount = structureRegion.StructureDatas.Count;
                    for (int i = 0; i < transformCount; i++)
                    {
                        transform = structureRegion.Structures[i];
                        if (i < DataCount)
                            sData = structureRegion.StructureDatas[i];
                        else
                        {
                            Logger.LogWarning(Translate("wreckingball_structure_array_sync_error"));
                            break;
                        }
                        distance = Vector3.Distance(transform.position, position);
                        if (distance < radius)
                        {
                            item = sData.structure.id;
                            if (ElementData.filterItem(item, Filter) || Filter.Contains('*'))
                            {
                                if (type == WreckType.Scan)
                                {
                                    if (distance <= 10)
                                    {
                                        ElementData.report(caller, item, distance, true, pInfoLibLoaded, BuildableType.Element, sData.owner);
                                    }
                                    else
                                        ElementData.report(caller, item, distance, false, pInfoLibLoaded, BuildableType.Element, sData.owner);
                                }
                                else
                                    destroyList.Add(new Destructible(transform, 's'));
                            }
                        }
                    } //
                }
            }
            
            for (int k = 0; k < BarricadeManager.BarricadeRegions.GetLength(0); k++)
            {
                for (int l = 0; l < BarricadeManager.BarricadeRegions.GetLength(1); l++)
                {
                    // check to see if the region is out of range, skip if it is.
                    if (position.RegionOutOfRange(k, l, radius))
                        continue;

                    barricadeRegion = BarricadeManager.BarricadeRegions[k, l];
                    transformCount = barricadeRegion.Barricades.Count;
                    DataCount = barricadeRegion.BarricadeDatas.Count;
                    for (int i = 0; i < transformCount; i++)
                    {
                        transform = barricadeRegion.Barricades[i];
                        if (i < DataCount)
                            bData = barricadeRegion.BarricadeDatas[i];
                        else
                        {
                            Logger.LogWarning(Translate("wreckingball_barricade_array_sync_error"));
                            break;
                        }
                        distance = Vector3.Distance(transform.position, position);
                        if (distance < radius)
                        {
                            item = bData.barricade.id;
                            if (ElementData.filterItem(item, Filter) || Filter.Contains('*'))
                            {
                                if (type == WreckType.Scan)
                                {
                                    if (distance <= 10)
                                    {
                                        ElementData.report(caller, item, distance, true, pInfoLibLoaded, BuildableType.Element, bData.owner);
                                    }
                                    else
                                        ElementData.report(caller, item, distance, false, pInfoLibLoaded, BuildableType.Element, bData.owner);
                                }
                                else
                                    destroyList.Add(new Destructible(transform, 'b'));
                            }
                        }
                    } //

                }
            }


            foreach (InteractableVehicle vehicle in VehicleManager.Vehicles)
            {
                distance = Vector3.Distance(vehicle.transform.position, position);
                if (distance < radius)
                {
                    if (BarricadeManager.tryGetPlant(vehicle.transform, out x, out y, out plant, out barricadeRegion))
                    {
                        transformCount = barricadeRegion.Barricades.Count;
                        DataCount = barricadeRegion.BarricadeDatas.Count;
                        for (int i = 0; i < transformCount; i++)
                        {
                            transform = barricadeRegion.Barricades[i];
                            if (i < DataCount)
                                bData = barricadeRegion.BarricadeDatas[i];
                            else
                            {
                                Logger.LogWarning(Translate("wreckingball_barricade_array_sync_error"));
                                break;
                            }
                            distance = Vector3.Distance(transform.position, position);
                            if (distance < radius)
                            {
                                item = bData.barricade.id;
                                if (ElementData.filterItem(item, Filter) || Filter.Contains('*'))
                                {
                                    if (type == WreckType.Scan)
                                    {
                                        if (distance <= 10)
                                        {
                                            ElementData.report(caller, item, distance, true, pInfoLibLoaded, BuildableType.VehicleElement, bData.owner);
                                        }
                                        else
                                            ElementData.report(caller, item, distance, false, pInfoLibLoaded, BuildableType.VehicleElement, bData.owner);
                                    }
                                    else
                                        destroyList.Add(new Destructible(transform, 'b'));
                                }
                            }
                        } //

                    }
                    else
                    {
                        barricadeRegion = null;
                    }
                    if (Filter.Contains('v') || Filter.Contains('*'))
                    {
                        if (type == WreckType.Scan)
                        {
                            if (distance <= 10)
                                ElementData.report(caller, 9999, distance, true, pInfoLibLoaded, BuildableType.Vehicle, (ulong)(barricadeRegion == null ? 0 : barricadeRegion.Barricades.Count));
                            else
                                ElementData.report(caller, 9999, distance, false, pInfoLibLoaded, BuildableType.Vehicle);
                        }
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
                        distance = Vector3.Distance(zombie.transform.position, position);
                        if (distance < radius)
                        {
                            if (type == WreckType.Scan)
                                ElementData.report(caller, 9998, (int)distance, false, pInfoLibLoaded);
                            else
                                destroyList.Add(new Destructible(zombie.transform, 'z', null, zombie));
                        }
                    }
                }
            }


            if (type == WreckType.Scan) return;

            if (destroyList.Count >= 1)
            {
                dIdxCount = destroyList.Count;
                Instruct(caller);
            }
            else
                UnturnedChat.Say(caller, Translate("wreckingball_not_found", radius));
        }

        internal void Scan(IRocketPlayer caller, string filter, uint radius, Vector3 position)
        {
            Wreck(caller, filter, radius, position, WreckType.Scan);
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

        private void Instruct(IRocketPlayer caller)
        {
            UnturnedChat.Say(caller, Translate("wreckingball_queued", dIdxCount, CalcProcessTime()));
            UnturnedChat.Say(caller, Translate("wreckingball_prompt"));
        }

        internal void Confirm(IRocketPlayer caller)
        {
            if (destroyList.Count <= 0)
            {
                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
            }
            else
            {
                processing = true;
                if (!(caller is ConsolePlayer))
                    originalCaller = (UnturnedPlayer)caller;
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
            originalCaller = null;
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
                            else
                                Logger.Log(Translate("wreckingball_complete", dIdx));
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
                    { "wreckingball_teleport_not_found", "Couldn't find any elements to teleport to, try to run the command again." },
                    { "wreckingball_teleport_not_allowed", "Not allowed to use wreck teleport from the console." },
                    { "wreckingball_reload_abort", "Warning: Current wreck job in progress has been aborted from a plugin reload." }
                };
            }
        }
    }
}
