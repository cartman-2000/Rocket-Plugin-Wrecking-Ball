using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

namespace ApokPT.RocketPlugins
{

    class Destructible
    {
        public Destructible(Transform transform, string type, InteractableVehicle vehicle = null, Zombie zombie = null)
        {
            Transform = transform;
            Type = type;
            Vehicle = vehicle;
            Zombie = zombie;
        }

        public Zombie Zombie { get; private set; }
        public InteractableVehicle Vehicle { get; private set; }
        public Transform Transform { get; private set; }
        public string Type { get; private set; }


    }

    class WreckingBall : RocketPlugin<WreckingBallConfiguration>
    {

        // Singleton

        public static WreckingBall Instance;

        protected override void Load()
        {
            Instance = this;
        }

        private static List<Destructible> destroyList = new List<Destructible>();
        private static int dIdx = 0;
        private static Timer aTimer;
        private uint delSpeed = 10;
        private bool processing = false;


        internal void Wreck(UnturnedPlayer player, string filter, uint radius, bool scan = false)
        {
            if (!scan)
            {
                if (processing)
                {
                    UnturnedChat.Say(player, Translate("wreckingball_progress", (destroyList.Count - dIdx), (Math.Ceiling((double)(destroyList.Count * delSpeed) / 1000))));
                    return;
                }
                Abort();
            }
            else
            {
                WreckCategories.Instance.reportList = new Dictionary<char, uint>();
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
                            if (WreckCategories.Instance.filterItem(item, Filter) || Filter.Contains('*'))
                            {
                                if (scan)
                                {
                                    if (distance <= 10)
                                        WreckCategories.Instance.report(item, (int)distance, true, StructureManager.tryGetInfo(current, out x, out y, out index, out structureRegion) ? structureRegion.structures[(int)index].owner : 0);
                                    else
                                        WreckCategories.Instance.report(item, (int)distance, false);
                                }
                                else
                                    destroyList.Add(new Destructible(current, "s"));
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
                            if (WreckCategories.Instance.filterItem(item, Filter) || Filter.Contains('*'))
                            {
                                if (scan)
                                {
                                    if (distance <= 10)
                                        WreckCategories.Instance.report(item, (int)distance, true, BarricadeManager.tryGetInfo(current, out x, out y, out plant, out index, out barricadeRegion) ? barricadeRegion.barricades[(int)index].owner : 0);
                                    else
                                        WreckCategories.Instance.report(item, (int)distance, false);
                                }
                                else
                                    destroyList.Add(new Destructible(current, "b"));
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
                            WreckCategories.Instance.report(9999, (int)distance, false);
                        else
                            destroyList.Add(new Destructible(vehicle.transform, "v", vehicle));
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
                                WreckCategories.Instance.report(9998, (int)distance, false);
                            else
                                destroyList.Add(new Destructible(zombie.transform, "z", null, zombie));
                        }
                    }
                }
            }


            if (scan) return;

            if (destroyList.Count >= 1)
                Instruct(player);
            else
                UnturnedChat.Say(player, Translate("wreckingball_not_found", radius));
        }

        internal void Scan(UnturnedPlayer caller, string filter, uint radius)
        {
            Wreck(caller, filter, radius, true);
            string report = "";
            uint totalCount = 0;
            if (WreckCategories.Instance.reportList.Count > 0)
            {

                foreach (KeyValuePair<char, uint> reportFilter in WreckCategories.Instance.reportList)
                {
                    report += " " + WreckCategories.Instance.category[reportFilter.Key].Name + ": " + reportFilter.Value + ",";
                    totalCount += reportFilter.Value;
                }
                if (report != "") report = report.Remove(report.Length - 1);
                UnturnedChat.Say(caller, Translate("wreckingball_scan", totalCount, radius, report));
                UnturnedChat.Say((CSteamID)0, Translate("wreckingball_scan", totalCount, radius, report));
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

            if (!toBarricades)
            {
                for (int k = 0; k < StructureManager.StructureRegions.GetLength(0); k++)
                {
                    for (int l = 0; l < StructureManager.StructureRegions.GetLength(1); l++)
                    {
                        foreach (Transform current in StructureManager.StructureRegions[k, l].Structures)
                        {
                            if (Vector3.Distance(current.position, caller.Position) > 20)
                            {
                                tpVector = new Vector3(current.position.x, current.position.y + 3, current.position.z);
                                caller.Teleport(tpVector, caller.Rotation);
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int k = 0; k < BarricadeManager.BarricadeRegions.GetLength(0); k++)
                {
                    for (int l = 0; l < BarricadeManager.BarricadeRegions.GetLength(1); l++)
                    {
                        foreach (Transform current in BarricadeManager.BarricadeRegions[k, l].Barricades)
                        {
                            if (Vector3.Distance(current.position, caller.Position) > 20)
                            {
                                tpVector = new Vector3(current.position.x, current.position.y + 3, current.position.z + 2);
                                caller.Teleport(tpVector, caller.Rotation);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void Instruct(UnturnedPlayer caller)
        {
            UnturnedChat.Say(caller, Translate("wreckingball_queued", destroyList.Count, (Math.Ceiling((double)(destroyList.Count * delSpeed) / 1000))));
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
                if (aTimer == null)
                {
                    aTimer = new Timer(delSpeed);
                    aTimer.Elapsed += delegate { OnTimedEvent(caller); };
                    aTimer.AutoReset = true;
                }
                processing = true;
                UnturnedChat.Say(caller, Translate("wreckingball_initiated", (Math.Ceiling((double)(destroyList.Count * delSpeed) / 1000))));
                dIdx = 0;
                aTimer.Enabled = true;
            }
        }

        internal void Abort()
        {
            if (aTimer != null)
                aTimer.Enabled = false;
            destroyList = new List<Destructible>();
            dIdx = 0;
        }

        private void OnTimedEvent(UnturnedPlayer caller)
        {

            try
            {
                if (destroyList[dIdx].Type == "s")
                {
                    try { StructureManager.damage(destroyList[dIdx].Transform, destroyList[dIdx].Transform.position, 65535, 1); }
                    catch { }
                }

                else if (destroyList[dIdx].Type == "b")
                {
                    try { BarricadeManager.damage(destroyList[dIdx].Transform, 65535, 1); }
                    catch { }
                }

                else if (destroyList[dIdx].Type == "v")
                {
                    try { destroyList[dIdx].Vehicle.askDamage(65535,false); }
                    catch { }
                }
                else if (destroyList[dIdx].Type == "z")
                {
                    EPlayerKill pKill;
                    try
                    {
                        for (int i = 0; i < 100; i++)
                            destroyList[dIdx].Zombie.askDamage(255, destroyList[dIdx].Zombie.transform.up, out pKill); 
                    }
                    catch { }
                }

                dIdx++;
                if (destroyList.Count == dIdx)
                {
                    UnturnedChat.Say(caller, Translate("wreckingball_complete", dIdx));
                    StructureManager.save();
                    BarricadeManager.save();
                    Abort();
                    processing = false;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        // Translations

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    {"wreckingball_scan","Found {0} elements @ {1}m:{2}"},
                    {"wreckingball_map_clear","Map has no elements!"},
                    {"wreckingball_not_found","No elements found in a {0} radius!"},
                    {"wreckingball_complete","Wrecking Ball complete! {0} elements(s) Destroyed!"},
                    {"wreckingball_initiated","Wrecking Ball initiated : {0} sec(s)"},
                    {"wreckingball_processing","Wrecking Ball destroying {0} element(s): {1} sec(s) left"},
                    {"wreckingball_aborted","Wrecking Ball Aborted! Destruction queue cleared!"},
                    {"wreckingball_help","Please define filter and radius: /wreck <filter> <radius> or /wreck teleport b|s"},
                    {"wreckingball_help_teleport","Please define type for teleport: /wreck teleport s|b"},
                    {"wreckingball_help_scan","Please define a scan filter and radius: /wreck scan <filter> <radius>"},
                    {"wreckingball_queued","{0} elements(s) found - ({1} sec(s))"},
                    {"wreckingball_prompt","Type '/wreck confirm' or '/wreck abort'"}
                };
            }
        }
    }
}
