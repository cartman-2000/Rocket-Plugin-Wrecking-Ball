﻿using PlayerInfoLibrary;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;
using Math = System.Math;

namespace ApokPT.RocketPlugins
{
    internal class DestructionProcessing
    {
        internal static List<Destructible> destroyList = new List<Destructible>();
        internal static int dIdx = 0;
        internal static int dIdxCount = 0;
        internal static bool processing = false;

        internal static List<Destructible> cleanupList = new List<Destructible>();
        internal static int cdIdx = 0;
        internal static int cdIdxCount = 0;
        internal static List<object[]> playersListBuildables = new List<object[]>();
        internal static int plbIdx = 0;
        internal static List<object[]> playersListFiles = new List<object[]>();
        internal static int plfIdx = 0;
        internal static bool cleanupProcessingBuildables = false;
        internal static bool cleanupProcessingFiles = false;
        internal static UnturnedPlayer originalCaller = null;
        internal static DateTime lastRunTimeWreck = DateTime.Now;
        private static DateTime lastGetCleanupInfo = DateTime.Now;
        private static DateTime lastVehiclesCapCheck = DateTime.Now;

        internal static Dictionary<ulong, int> pElementCounts = new Dictionary<ulong, int>();

        internal static bool syncError;

        internal static void Wreck(IRocketPlayer caller, string filter, float radius, Vector3 position, WreckType type, FlagType flagtype, ulong steamID, ushort itemID)
        {
            bool pInfoLibLoaded = false;
            syncError = false;
            if (type == WreckType.Wreck)
            {
                if (DestructionProcessing.processing)
                {
                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_processing", originalCaller != null ? originalCaller.CharacterName : "???", (dIdxCount - dIdx), CalcProcessTime()));
                    return;
                }
                Abort(WreckType.Wreck);
            }
            else if (type == WreckType.Scan)
            {
                WreckingBall.ElementData.reportLists[BuildableType.Element].Clear();
                WreckingBall.ElementData.reportLists[BuildableType.VehicleElement].Clear();
                if (WreckingBall.Instance.Configuration.Instance.EnablePlayerInfo && WreckingBall.isPlayerInfoLibPresent && WreckingBall.isPlayerInfoLibLoaded)
                {
                    pInfoLibLoaded = true;
                }
            }
            UnturnedPlayer Player = null;
            if (!(caller is ConsolePlayer) && type != WreckType.Cleanup)
            {
                Player = (UnturnedPlayer)caller;
                if (Player.IsInVehicle)
                    position = Player.CurrentVehicle.transform.position;
                else
                    position = Player.Position;
            }

            List<char> Filter = new List<char>();
            Filter.AddRange(filter.ToCharArray());

            float distance = 0;
            float vdistance = 0;
            StructureRegion structureRegion;
            BarricadeRegion barricadeRegion;

            for (int k = 0; k < StructureManager.regions.GetLength(0); k++)
            {
                for (int l = 0; l < StructureManager.regions.GetLength(1); l++)
                {
                    // check to see if the region is out of range, skip if it is.
                    if (!radius.IsNaN() && position.RegionOutOfRange(k, l, radius) && type != WreckType.Cleanup && type != WreckType.Counts)
                        continue;

                    structureRegion = StructureManager.regions[k, l];
                    ProcessElements(caller, itemID, radius, type, flagtype, Filter, pInfoLibLoaded, structureRegion, position, steamID, BuildableType.Element);
                }
            }

            for (int k = 0; k < BarricadeManager.BarricadeRegions.GetLength(0); k++)
            {
                for (int l = 0; l < BarricadeManager.BarricadeRegions.GetLength(1); l++)
                {
                    // check to see if the region is out of range, skip if it is.
                    if (!radius.IsNaN() && position.RegionOutOfRange(k, l, radius) && type != WreckType.Cleanup && type != WreckType.Counts)
                        continue;

                    barricadeRegion = BarricadeManager.BarricadeRegions[k, l];
                    ProcessElements(caller, itemID, radius, type, flagtype, Filter, pInfoLibLoaded, barricadeRegion, position, steamID, BuildableType.Element);
                }
            }


            foreach (InteractableVehicle vehicle in VehicleManager.vehicles)
            {
                bool validVehicleElements = BarricadeManager.tryGetPlant(vehicle.transform, out byte x, out byte y, out ushort plant, out barricadeRegion);
                // Process Vehicles.
                if ((Filter.Contains('V') || Filter.Contains('*')) && type != WreckType.Cleanup && type != WreckType.Counts && (flagtype == FlagType.Normal || (flagtype == FlagType.SteamID && vehicle.isLocked && vehicle.lockedOwner == (CSteamID)steamID)))
                {
                    vdistance = Vector3.Distance(vehicle.transform.position, position);
                    if ((!radius.IsNaN() && vdistance <= radius) || (radius.IsNaN() && (vehicle.transform.position.x.IsNaN() || vehicle.transform.position.y.IsNaN() || vehicle.transform.position.z.IsNaN())))
                        WreckProcess(caller, 999, vdistance, pInfoLibLoaded, BuildableType.Vehicle, type, vehicle, vehicle.transform, !validVehicleElements ? 0 : barricadeRegion.drops.Count, vehicle.isLocked ? (ulong)vehicle.lockedOwner : 0);
                    if (vehicle.asset.engine == EEngine.TRAIN && vehicle.trainCars != null && vehicle.trainCars.Length > 1)
                    {
                        for (int i = 1; i < vehicle.trainCars.Length; i++)
                        {
                            if (BarricadeManager.tryGetPlant(vehicle.trainCars[i].root, out x, out y, out plant, out BarricadeRegion barricadeRegion2))
                            {
                                float tcdistance = Vector3.Distance(vehicle.trainCars[i].root.position, position);
                                if (tcdistance <= radius)
                                    WreckProcess(caller, 999, tcdistance, pInfoLibLoaded, BuildableType.Vehicle, type, vehicle, vehicle.transform, barricadeRegion2 == null ? 0 : barricadeRegion2.drops.Count, 0, i);
                            }
                        }
                    }
                }

                if (type == WreckType.Cleanup && vehicle.asset.engine != EEngine.TRAIN && WreckingBall.Instance.Configuration.Instance.CleanupLockedCars && vehicle.isLocked && vehicle.lockedOwner == (CSteamID)steamID)
                {
                    cleanupList.Add(new Destructible(vehicle.transform, ElementType.Vehicle, vehicle.asset.id, vehicle));
                }
                // Add Locked vehicles to the top players count, if the cleanup locked vehicles feature is active.
                if (type == WreckType.Counts && vehicle.asset.engine != EEngine.TRAIN && WreckingBall.Instance.Configuration.Instance.CleanupLockedCars && vehicle.isLocked)
                {
                    ulong vOwner = (ulong)vehicle.lockedOwner;
                    if (pElementCounts.ContainsKey(vOwner))
                        pElementCounts[vOwner]++;
                    else
                        pElementCounts.Add(vOwner, 1);
                }
                // Process vehicles elements, remove distance limiting on vehicle placement for element scanning, to handle massively misplaced elements on vehicles.
                if (validVehicleElements)
                    ProcessElements(caller, itemID, radius, type, flagtype, Filter, pInfoLibLoaded, barricadeRegion, position, steamID, BuildableType.VehicleElement);

                if (vehicle.asset.engine == EEngine.TRAIN && vehicle.trainCars != null && vehicle.trainCars.Length > 1)
                {
                    for (int i = 1; i < vehicle.trainCars.Length; i++)
                    {
                        if (BarricadeManager.tryGetPlant(vehicle.trainCars[i].root, out x, out y, out plant, out BarricadeRegion barricadeRegion2))
                            ProcessElements(caller, itemID, radius, type, flagtype, Filter, pInfoLibLoaded, barricadeRegion2, position, steamID, BuildableType.VehicleElement);
                    }
                }
            }

            if (Filter.Contains('Z'))
            {
                for (int z = 0; z < ZombieManager.regions.Length; z++)
                {

                    foreach (Zombie zombie in ZombieManager.regions[z].zombies)
                    {
                        distance = Vector3.Distance(zombie.transform.position, position);
                        if (distance < radius)
                        {
                            WreckProcess(caller, 998, distance, pInfoLibLoaded, BuildableType.Element, type, zombie, zombie.transform);

                        }
                    }
                }
            }

            if (Filter.Contains('A'))
            {
                foreach (Animal animal in AnimalManager.animals)
                {
                    distance = Vector3.Distance(animal.transform.position, position);
                    if (distance <= radius)
                        WreckProcess(caller, 997, distance, pInfoLibLoaded, BuildableType.Element, type, animal, animal.transform);
                }
            }


            if (type == WreckType.Scan)
            {
                uint totalCount = 0;
                if (WreckingBall.ElementData.reportLists[BuildableType.Element].Count > 0 || WreckingBall.ElementData.reportLists[BuildableType.VehicleElement].Count > 0)
                {
                    foreach (KeyValuePair<BuildableType, Dictionary<char, uint>> reportDictionary in WreckingBall.ElementData.reportLists)
                    {
                        if (reportDictionary.Value.Count == 0)
                            continue;
                        foreach (KeyValuePair<char, uint> reportFilter in reportDictionary.Value)
                            totalCount += reportFilter.Value;
                    }
                }
                Logger.Log(string.Format("Player: {0}, ran scan at: {1}, with Radius: {7}, with Flag type: {2}, with Flags: {3}, with ItemID: {4}, with SteamID: {5}, number of elements scanned: {6}", caller is ConsolePlayer ? "Console" : Player.CharacterName + " [" + Player.SteamName + "] (" + Player.CSteamID.ToString() + ")", caller is ConsolePlayer ? "N/A" : Player.Position.ToString(), flagtype.ToString(), Filter.Count > 0 ? string.Join("", Filter.Select(i => i.ToString()).ToArray()) : "N/A", itemID, steamID, totalCount, radius.IsNaN() ? "NaN(NaN Check)" : radius.ToString()));
                return;
            }
            if (destroyList.Count >= 1 && type == WreckType.Wreck)
            {
                Logger.Log(string.Format("Player {0}, queued wreck at: {1}, with Radius: {7}, with Flag type: {2}, with Flags: {3}, with itemID: {4}, with StermID: {5}, number of elements queued: {6}", caller is ConsolePlayer ? "Console" : Player.CharacterName + " [" + Player.SteamName + "] (" + Player.CSteamID.ToString() + ")", caller is ConsolePlayer ? "N/A" : Player.Position.ToString(), flagtype.ToString(), Filter.Count > 0 ? string.Join("", Filter.Select(i => i.ToString()).ToArray()) : "N/A", itemID, steamID, destroyList.Count, radius.IsNaN() ? "NaN(NaN Check)" : radius.ToString()));
                dIdxCount = destroyList.Count;
                WreckingBall.Instance.Instruct(caller);
            }
            else if (type == WreckType.Cleanup)
            {
                cdIdxCount = cleanupList.Count;
            }
            else if (type != WreckType.Counts)
                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_not_found", radius));
        }

        private static void WreckProcess(IRocketPlayer caller, ushort itemID, float distance, bool pInfoLibLoaded, BuildableType buildType, WreckType type, object data, Transform transform, int count = 0, ulong lockedOwner = 0, int vindex = 0)
        {

            if (type == WreckType.Scan)
            {
                if (distance <= 10)
                    WreckingBall.ElementData.Report(caller, itemID, distance, true, pInfoLibLoaded, data, buildType, count, lockedOwner, vindex);
                else
                    WreckingBall.ElementData.Report(caller, itemID, distance, false, pInfoLibLoaded, data, buildType);
            }
            else
            {
                if (data is StructureData)
                    destroyList.Add(new Destructible(transform, ElementType.Structure, itemID));
                if (data is BarricadeData)
                    destroyList.Add(new Destructible(transform, ElementType.Barricade, itemID));
                // Add the vehicle to the destruction list, as long as it's not a train. Allow a special case where you could destroy a train from using wreck on the console, only.
                if (data is InteractableVehicle && (((InteractableVehicle)data).asset.engine != EEngine.TRAIN || caller is ConsolePlayer && vindex == 0 && type != WreckType.Counts && type != WreckType.Cleanup))
                    destroyList.Add(new Destructible(transform, ElementType.Vehicle, itemID, data as InteractableVehicle));
                if (data is Zombie)
                    destroyList.Add(new Destructible(transform, ElementType.Zombie, itemID, null, data as Zombie));
                if (data is Animal)
                    destroyList.Add(new Destructible(transform, ElementType.Animal, itemID, null, null, data as Animal));
            }
        }

        private static void ProcessElements(IRocketPlayer caller, ushort itemID, float radius, WreckType type, FlagType flagtype, List<char> Filter, bool pInfoLibLoaded, object region , Vector3 position, ulong steamID, BuildableType buildType)
        {
            StructureRegion sRegion = null;
            BarricadeRegion bRegion = null;
            BarricadeData bData = null;
            StructureData sData = null;
            bool isSRegion = region is StructureRegion;
            int DataCount;
            int transformCount;
            if (isSRegion)
            {
                sRegion = region as StructureRegion;
                transformCount = sRegion.drops.Count;
                DataCount = sRegion.structures.Count;
            }
            else
            {
                bRegion = region as BarricadeRegion;
                transformCount = bRegion.drops.Count;
                DataCount = bRegion.barricades.Count;
            }

            for (int i = 0; i < transformCount; i++)
            {
                Transform transform = isSRegion ? sRegion.drops[i].model : bRegion.drops[i].model;
                ulong owner;
                if (i < DataCount)
                    if (isSRegion)
                    {
                        sData = sRegion.structures[i];
                        owner = sData.owner;
                    }
                    else
                    {
                        bData = bRegion.barricades[i];
                        owner = bData.owner;
                    }
                else
                {
                    Logger.LogWarning(WreckingBall.Instance.Translate(isSRegion ? "wreckingball_structure_array_sync_error" : "wreckingball_barricade_array_sync_error"));
                    syncError = true;
                    break;
                }
                float distance = Vector3.Distance(transform.position, position);
                if (((!radius.IsNaN() && distance <= radius) || (radius.IsNaN() &&  (transform.position.x.IsNaN() || transform.position.y.IsNaN() || transform.position.z.IsNaN()))) && type != WreckType.Cleanup && type != WreckType.Counts)
                {
                    ushort item = isSRegion ? sData.structure.id : bData.barricade.id;
                    if (WreckingBall.ElementData.FilterItem(item, Filter) || Filter.Contains('*') || flagtype == FlagType.ItemID)
                    {
                        if (flagtype == FlagType.Normal)
                            WreckProcess(caller, item, distance, pInfoLibLoaded, buildType, type, isSRegion ? (object)sData : bData, transform);
                        else if (flagtype == FlagType.SteamID && owner == steamID)
                            WreckProcess(caller, item, distance, pInfoLibLoaded, buildType, type, isSRegion ? (object)sData : bData, transform);
                        else if (flagtype == FlagType.ItemID && itemID == item)
                            WreckProcess(caller, item, distance, pInfoLibLoaded, buildType, type, isSRegion ? (object)sData : bData, transform);
                    }
                }
                else if (type == WreckType.Cleanup && owner == steamID)
                {
                    cleanupList.Add(new Destructible(transform, isSRegion ? ElementType.Structure : ElementType.Barricade, isSRegion ? sData.structure.id : bData.barricade.id));
                }
                else if (type == WreckType.Counts)
                {
                    if (pElementCounts.ContainsKey(owner))
                        pElementCounts[owner]++;
                    else
                        pElementCounts.Add(owner, 1);
                }
            }
        }

        internal static double CalcProcessTime()
        {
            return Math.Round(((dIdxCount - dIdx) * (1 / (WreckingBall.Instance.Configuration.Instance.DestructionRate * WreckingBall.Instance.Configuration.Instance.DestructionsPerInterval))), 2);
        }

        internal static void Abort(WreckType type)
        {
            if (type == WreckType.Wreck)
            {
                processing = false;
                destroyList.Clear();
                dIdx = 0;
                dIdxCount = 0;
                originalCaller = null;
            }
            else
            {
                cleanupProcessingBuildables = false;
                cleanupProcessingFiles = false;
                cleanupList.Clear();
                cdIdx = 0;
                cdIdxCount = 0;
                playersListBuildables.Clear();
                playersListFiles.Clear();
                plbIdx = 0;
                plfIdx = 0;
            }
        }

        internal static void HandleCleanup()
        {
            if (cleanupProcessingBuildables)
            {
                if (cleanupList.Count <= cdIdx)
                {
                    if (cdIdxCount != 0)
                        Logger.Log(WreckingBall.Instance.Translate("wreckingball_complete", cdIdx));
                    cleanupList.Clear();
                    cdIdx = 0;
                    cdIdxCount = 0;
                    try
                    {
                        if (!syncError && WreckingBall.isPlayerInfoLibLoaded)
                            PlayerInfoLib.Database.SetOption((CSteamID)((ulong)playersListBuildables[plbIdx][0]), OptionType.Buildables, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                    plbIdx++;
                    if (plbIdx >= WreckingBall.Instance.Configuration.Instance.CleanupPerInterval || plbIdx >= playersListBuildables.Count)
                    {
                        Logger.Log("Finished with cleaning up the player elements in this run.");
                        plbIdx = 0;
                        playersListBuildables.Clear();
                        cleanupProcessingBuildables = false;
                        SaveManager.save();
                    }
                    else
                    {
                        syncError = false;
                        // Skip the player buildables cleanup if they have the right permission.
                        if (R.Permissions.HasPermission(new RocketPlayer(playersListBuildables[plbIdx][0].ToString()), "wb.skipbuildables"))
                            Logger.Log(string.Format("Skipping buildables cleanup for player: {0} [{1}] ({2}).", playersListBuildables[plbIdx][1].ToString(), playersListBuildables[plbIdx][2].ToString(), (ulong)playersListBuildables[plbIdx][0]));
                        else
                        {
                            Wreck(new ConsolePlayer(), "*", 100000, new Vector3(0, 0, 0), WreckType.Cleanup, FlagType.SteamID, (ulong)playersListBuildables[plbIdx][0], 0);
                            if (cdIdxCount == 0)
                                Logger.Log(string.Format("No elements found for player: {0} [{1}] ({2}).", playersListBuildables[plbIdx][1].ToString(), playersListBuildables[plbIdx][2].ToString(), (ulong)playersListBuildables[plbIdx][0]));
                            else
                                Logger.Log(string.Format("Cleaning up {0} elements for player: {1} [{2}] ({3}).", cdIdxCount, playersListBuildables[plbIdx][1].ToString(), playersListBuildables[plbIdx][2].ToString(), (ulong)playersListBuildables[plbIdx][0]));
                        }
                    }
                }
            }
            if (cleanupProcessingFiles)
            {
                object[] pf = playersListFiles[plfIdx];
                bool found = false;
                bool skipped = false;

                try
                {
                    for (byte i = 0; i < Customization.FREE_CHARACTERS + Customization.PRO_CHARACTERS; i++)
                    {
                        // Skip the player data cleanup if they have the right permission.
                        if (R.Permissions.HasPermission(new RocketPlayer(pf[0].ToString()), "wb.skipplayerdata"))
                        {
                            skipped = true;
                            break;
                        }
                        else if (ServerSavedata.folderExists("/Players/" + (ulong)pf[0] + "_" + i))
                        {
                            ServerSavedata.deleteFolder("/Players/" + (ulong)pf[0] + "_" + i);
                            found = true;
                        }
                    }
                    if (WreckingBall.isPlayerInfoLibLoaded)
                        PlayerInfoLib.Database.SetOption((CSteamID)(ulong)pf[0], OptionType.PlayerFiles, true);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Error in removing player file data, possible files left behind.");
                }

                if (found)
                    Logger.Log(string.Format("Cleaning up player data folders for player: {0} [{1}] ({2}).", pf[1].ToString(), pf[2].ToString(), (ulong)pf[0]));
                else if (skipped)
                    Logger.Log(string.Format("Skipping player data cleanup for player: {0} [{1}] ({2}).", pf[1].ToString(), pf[2].ToString(), (ulong)pf[0]));
                else
                    Logger.Log(string.Format("Player data folders for player: {0} [{1}] ({2}) not found.", pf[1].ToString(), pf[2].ToString(), (ulong)pf[0]));
                plfIdx++;
                if (plfIdx >= WreckingBall.Instance.Configuration.Instance.CleanupPerInterval || plfIdx >= playersListFiles.Count)
                {
                    Logger.Log("Finished with cleaning up the player's data files in this run.");
                    plfIdx = 0;
                    playersListFiles.Clear();
                    cleanupProcessingFiles = false;
                }
            }

            if ((DateTime.Now - lastGetCleanupInfo).TotalSeconds > WreckingBall.Instance.Configuration.Instance.CleanupIntervalTime * 60)
            {
                lastGetCleanupInfo = DateTime.Now;
                if (WreckingBall.Instance.Configuration.Instance.BuildableCleanup)
                {
                    if (playersListBuildables.Count == 0 && WreckingBall.isPlayerInfoLibLoaded)
                    {
                        GetCleanupList(OptionType.Buildables, WreckingBall.Instance.Configuration.Instance.BuildableWaitTime, WreckingBall.Instance.Configuration.Instance.CleanupPerInterval);
                        if (playersListBuildables.Count != 0)
                        {
                            // Start cleanup sequence for the players elements.
                            cleanupProcessingBuildables = true;
                            syncError = false;
                            // Skip the player buildables cleanup if they have the right permission.
                            if (R.Permissions.HasPermission(new RocketPlayer(playersListBuildables[plbIdx][0].ToString()), "wb.skipbuildables"))
                                Logger.Log(string.Format("Skipping buildables cleanup for player: {0} [{1}] ({2}).", playersListBuildables[plbIdx][1].ToString(), playersListBuildables[plbIdx][2].ToString(), (ulong)playersListBuildables[plbIdx][0]));
                            else
                            {
                                Wreck(new ConsolePlayer(), "*", 100000, new Vector3(0, 0, 0), WreckType.Cleanup, FlagType.SteamID, (ulong)playersListBuildables[plbIdx][0], 0);
                                if (cdIdxCount == 0)
                                    Logger.Log(string.Format("No elements found for player: {0} [{1}] ({2}).", playersListBuildables[plbIdx][1].ToString(), playersListBuildables[plbIdx][2].ToString(), (ulong)playersListBuildables[plbIdx][0]));
                                else
                                    Logger.Log(string.Format("Cleaning up {0} elements for player: {1} [{2}] ({3}).", cdIdxCount, playersListBuildables[plbIdx][1].ToString(), playersListBuildables[plbIdx][2].ToString(), (ulong)playersListBuildables[plbIdx][0]));
                            }
                        }
                    }
                }
                if (WreckingBall.Instance.Configuration.Instance.PlayerDataCleanup)
                {
                    if (playersListFiles.Count == 0 && WreckingBall.isPlayerInfoLibLoaded)
                    {
                        GetCleanupList(OptionType.PlayerFiles, WreckingBall.Instance.Configuration.Instance.PlayerDataWaitTime, WreckingBall.Instance.Configuration.Instance.CleanupPerInterval);
                        if (playersListFiles.Count != 0)
                        {
                            // Start cleanup sequence for the players files.
                            cleanupProcessingFiles = true;
                        }
                    }
                }
            }
        }

        internal static bool HasFlaggedElement(Transform vehicleTransform, out ulong elementSteamID)
        {
            elementSteamID = 0;
            if (BarricadeManager.tryGetPlant(vehicleTransform, out _, out _, out _, out BarricadeRegion barricadeRegion))
            {
                int transformCount = barricadeRegion.drops.Count;
                int DataCount = barricadeRegion.barricades.Count;
                BarricadeData bData;
                bool match = false;
                if (transformCount == DataCount)
                {
                    for (int e = 0; e < DataCount; e++)
                    {
                        bData = barricadeRegion.barricades[e];
                        if (WreckingBall.ElementData.FilterItem(bData.barricade.id, new List<char> { WreckingBall.Instance.Configuration.Instance.VehicleSignFlag }))
                        {
                            match = true;
                            elementSteamID = bData.owner;
                            break;
                        }
                    }
                    if (match)
                        return true;
                }
            }
            return false;
        }

        internal static void HandleVehicleCap()
        {
            if ((DateTime.Now - lastVehiclesCapCheck).TotalSeconds > WreckingBall.Instance.Configuration.Instance.VCapCheckInterval)
            {
                lastVehiclesCapCheck = DateTime.Now;
                Dictionary<InteractableVehicle, int> vList = new Dictionary<InteractableVehicle, int>();
                byte x = 0;
                byte y = 0;
                ushort plant = 0;
                foreach (InteractableVehicle vehicle in VehicleManager.vehicles)
                {
                    if (vehicle.isDead)
                        continue;
                    // Don't process trains with the cap, this can bug things up on a server.
                    if (vehicle.asset.engine == EEngine.TRAIN)
                        continue;
                    if (BarricadeManager.tryGetPlant(vehicle.transform, out x, out y, out plant, out BarricadeRegion barricadeRegion))
                        vList.Add(vehicle, barricadeRegion.drops.Count);
                    else
                        vList.Add(vehicle, 0);
                }
                if (vList.Count > WreckingBall.Instance.Configuration.Instance.MaxVehiclesAllowed)
                {
                    int numToDestroy = vList.Count - WreckingBall.Instance.Configuration.Instance.MaxVehiclesAllowed;
                    int i = 0;
                    Logger.Log(string.Format("Vehicle Cap Check: Count over max by: {0} vehicles, starting cleanup process.", numToDestroy), ConsoleColor.Yellow);
                    if (WreckingBall.Instance.Configuration.Instance.VCapDestroyByElementCount)
                    {
                        var sort = vList.OrderBy(c => c.Value);
                        vList = sort.ToDictionary(d => d.Key, d => d.Value);
                    }
                    bool useSafeGuards = true;
                    bool getPInfo = false;
                    if (WreckingBall.Instance.Configuration.Instance.EnablePlayerInfo && WreckingBall.isPlayerInfoLibPresent && WreckingBall.isPlayerInfoLibLoaded)
                        getPInfo = true;
                    restart:
                    int v = 0;
                    foreach (KeyValuePair<InteractableVehicle, int> vehicle in vList)
                    {
                        v++;
                        if (vehicle.Key.isDead)
                            continue;
                        if (!vehicle.Key.isEmpty)
                            continue;
                        bool hasSign = HasFlaggedElement(vehicle.Key.transform, out ulong elementOwner);
                        if (useSafeGuards && (WreckingBall.Instance.Configuration.Instance.LowElementCountOnly || WreckingBall.Instance.Configuration.Instance.KeepVehiclesWithSigns))
                        {
                            if (WreckingBall.Instance.Configuration.Instance.LimitSafeGuards && v > Math.Round(WreckingBall.Instance.Configuration.Instance.MaxVehiclesAllowed * WreckingBall.Instance.Configuration.Instance.LimitSafeGuardsRatio + numToDestroy, 0))
                            {
                                useSafeGuards = false;
                                goto restart;
                            }
                            if (WreckingBall.Instance.Configuration.Instance.LowElementCountOnly && WreckingBall.Instance.Configuration.Instance.MinElementCount <= vehicle.Value)
                                continue;
                            if (WreckingBall.Instance.Configuration.Instance.KeepVehiclesWithSigns && hasSign)
                                continue;

                        }
                        // Current vehicle in check is the last vehicle added to the server, newest, skip.
                        if (vehicle.Key.transform == VehicleManager.vehicles.Last().transform)
                            continue;
                        if ((vehicle.Key.isLocked && R.Permissions.HasPermission(new RocketPlayer(vehicle.Key.lockedOwner.ToString()), "wb.bypassvehiclecap")) || (hasSign && R.Permissions.HasPermission(new RocketPlayer(elementOwner.ToString()), "wb.bypassvehiclecap")))
                            continue;
                        i++;
                        if (i > numToDestroy)
                            break;
                        Logger.Log(string.Format("Vehicle #{0}, with InstanceID: {1}, and Type: {5}({6}), at position: {2} destroyed, Element count: {3}, Sign By {7}, Locked By: {4}.", v, vehicle.Key.instanceID, vehicle.Key.transform.position.ToString(), vehicle.Value, vehicle.Key.isLocked ? (getPInfo ? WreckingBall.Instance.PInfoGenerateMessage((ulong)vehicle.Key.lockedOwner) : vehicle.Key.lockedOwner.ToString()) : "N/A", vehicle.Key.asset.vehicleName, vehicle.Key.asset.id, hasSign ? (getPInfo ? WreckingBall.Instance.PInfoGenerateMessage(elementOwner) : elementOwner.ToString()) : "N/A"));
                        if (WreckingBall.Instance.Configuration.Instance.EnableVehicleElementDrop)
                            WreckingBall.Instance.VehicleElementDrop(vehicle.Key);
                        vehicle.Key.askDamage(ushort.MaxValue, false);
                    }
                    Logger.Log("Vehicle cleanup finished.", ConsoleColor.Yellow);
                }
            }
        }

        private static void GetCleanupList(OptionType option, float waitTime, byte numberToProcess)
        {
            Logger.Log(string.Format("Getting list of {0} to cleanup.", option == OptionType.Buildables ? "player buildables" : "player files"));
            List<object[]> tmp = new List<object[]>();
            try
            {
                tmp = PlayerInfoLib.Database.GetCleanupList(option, (DateTime.Now.AddSeconds(-(waitTime * 86400)).ToTimeStamp()));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            if (tmp.Count == 0)
            {
                Logger.Log("No records found.");
                return;
            }
            else
            {
                Logger.Log(string.Format("Found {0} records, processing {1} records.", tmp.Count, tmp.Count < numberToProcess ? tmp.Count : numberToProcess));
                if (option == OptionType.Buildables)
                    playersListBuildables = tmp.GetRange(0, tmp.Count < numberToProcess ? tmp.Count : numberToProcess);
                else
                    playersListFiles = tmp.GetRange(0, tmp.Count < numberToProcess ? tmp.Count : numberToProcess);
            }
        }

        internal static void DestructionLoop(WreckType type)
        {
            try
            {
                int i = 0;
                EPlayerKill pKill;
                uint xp;
                while (((dIdx < dIdxCount && type == WreckType.Wreck) || (cdIdx < cdIdxCount && type == WreckType.Cleanup)) && i < WreckingBall.Instance.Configuration.Instance.DestructionsPerInterval)
                {
                    Destructible element = type == WreckType.Wreck ? destroyList[dIdx] : cleanupList[cdIdx];

                    if (element.Type == ElementType.Structure)
                    {
                        try
                        {
                            if (WreckingBall.Instance.Configuration.Instance.EnableDestroyedElementDrop)
                            {
                                try
                                {
                                    Item item = new Item(element.ItemID, true);
                                    ItemManager.dropItem(item, element.Transform.position, false, true, true);
                                }
                                catch (Exception ex) { Logger.LogException(ex, "Error in dropping an item for a destroyed structure."); }
                            }
                            StructureManager.damage(element.Transform, element.Transform.position, ushort.MaxValue, 1, false);
                        }
                        catch (Exception ex) { Logger.LogException(ex, "Error in destroying structure."); }
                    }

                    else if (element.Type == ElementType.Barricade || element.Type == ElementType.VehicleBarricade)
                    {
                        try
                        {
                            if ((element.Type == ElementType.VehicleBarricade && WreckingBall.Instance.Configuration.Instance.EnableVehicleElementDrop) || (element.Type == ElementType.Barricade && WreckingBall.Instance.Configuration.Instance.EnableDestroyedElementDrop))
                            {
                                try
                                {
                                    Item item = new Item(element.ItemID, true);
                                    ItemManager.dropItem(item, element.Transform.position, false, true, true);
                                }
                                catch (Exception ex) { Logger.LogException(ex, "Error in dropping an item for a destroyed" + (element.Type == ElementType.VehicleBarricade ? " vehicle" : string.Empty) + " barricade."); }
                            }
                            BarricadeManager.damage(element.Transform, ushort.MaxValue, 1, false);
                        }
                        catch (Exception ex) { Logger.LogException(ex, "Error in destroying barricade."); }
                    }

                    else if (element.Type == ElementType.Vehicle)
                    {
                        try {
                            // Output log entry if a vehicle is destroyed by a cleanup.
                            if (type == WreckType.Cleanup)
                            {
                                bool getPInfo = WreckingBall.isPlayerInfoLibLoaded;
                                Logger.Log(string.Format("Cleanup: Vehicle with InstanceID: {0}, and Type: {1}({2}), at position: {3} destroyed, Element count: {4}, Sign By: {5}, Locked By: {6}.",
                                    element.Vehicle.instanceID,
                                    element.Vehicle.asset.vehicleName,
                                    element.Vehicle.asset.id,
                                    element.Vehicle.transform.position.ToString(),
                                    BarricadeManager.tryGetPlant(element.Vehicle.transform, out byte x, out byte y, out ushort plant, out BarricadeRegion barricadeRegion) ? barricadeRegion.drops.Count : 0,
                                    HasFlaggedElement(element.Vehicle.transform, out ulong vFlagOwner) ? (getPInfo ? WreckingBall.Instance.PInfoGenerateMessage(vFlagOwner) : vFlagOwner.ToString()) : "N/A",
                                    element.Vehicle.isLocked ? (getPInfo ? WreckingBall.Instance.PInfoGenerateMessage((ulong)element.Vehicle.lockedOwner) : element.Vehicle.lockedOwner.ToString()) : "N/A")); 
                            }
                            if (WreckingBall.Instance.Configuration.Instance.EnableVehicleElementDrop)
                            {
                                if (element.Vehicle.asset.engine == EEngine.TRAIN && element.Vehicle.trainCars != null && element.Vehicle.trainCars.Count() > 1)
                                {
                                    foreach (TrainCar car in element.Vehicle.trainCars)
                                    {
                                        WreckingBall.Instance.VehicleElementDrop(element.Vehicle, true, car.root);
                                    }
                                }
                                else
                                    WreckingBall.Instance.VehicleElementDrop(element.Vehicle);
                            }
                            if (!element.Vehicle.isDead)
                                element.Vehicle.askDamage(ushort.MaxValue, false);
                        }
                        catch (Exception ex) { Logger.LogException(ex, "Error in destroying vehicle."); }
                    }
                    else if (element.Type == ElementType.Zombie)
                    {
                        try
                        {
                            for (int z = 0; z < 1000 && !element.Zombie.isDead; z++)
                                element.Zombie.askDamage(ushort.MaxValue, element.Zombie.transform.up, out pKill, out xp);
                        }
                        catch (Exception ex) { Logger.LogException(ex, "Error in killing zombie."); }
                    }
                    else if (element.Type == ElementType.Animal)
                    {
                        try
                        {
                            for (int a = 0; a < 1000 && !element.Animal.isDead; a++)
                                element.Animal.askDamage(ushort.MaxValue, element.Animal.transform.up, out pKill, out xp);
                        }
                        catch (Exception ex) { Logger.LogException(ex, "Error in killing animal."); }
                    }
                    if (type == WreckType.Wreck)
                        dIdx++;
                    else
                        cdIdx++;
                    i++;
                }
                if (destroyList.Count == dIdx && type == WreckType.Wreck)
                {
                    if (originalCaller != null)
                        UnturnedChat.Say(originalCaller, WreckingBall.Instance.Translate("wreckingball_complete", dIdx));
                    else
                        Logger.Log(WreckingBall.Instance.Translate("wreckingball_complete", dIdx));
                    SaveManager.save();
                    Abort(WreckType.Wreck);
                }
            }
            catch (Exception ex) { Logger.LogException(ex, "General destruction loop error."); }
        }
    }
}
