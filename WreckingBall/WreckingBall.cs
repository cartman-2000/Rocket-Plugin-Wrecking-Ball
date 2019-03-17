using DynShop;
using fr34kyn01535.Uconomy;
using PlayerInfoLibrary;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.API.Extensions;
using Rocket.Core.Commands;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using DatabaseManager = PlayerInfoLibrary.DatabaseManager;
using Logger = Rocket.Core.Logging.Logger;

namespace ApokPT.RocketPlugins
{
    public class WreckingBall : RocketPlugin<WreckingBallConfiguration>
    {
        // Singleton
        public static WreckingBall Instance;
        public static ElementDataManager ElementData;
        internal static bool isDynShopPresent = false;
        internal static bool isUconomyPresent = false;
        internal static bool isPlayerInfoLibPresent = false;
        internal static bool isDynShopLoaded = false;
        internal static bool isUconomyLoaded = false;
        internal static bool isPlayerInfoLibLoaded = false;
        internal static bool debug = false;


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
            if (Instance.Configuration.Instance.EnableCleanup)
            {
                CheckCleanup();

            }
            Instance.Configuration.Save();
            debug = Instance.Configuration.Instance.Debug;
            // Register plugin dependency check.
            Level.onPostLevelLoaded += _PostLevelLoaded;

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
            Level.onPostLevelLoaded -= _PostLevelLoaded;
        }

        private void _PostLevelLoaded(int level)
        {
            OnPluginLoading += OnPluginsLoading;
            OnPluginUnloading += OnPluginsUnloading;
            if (IsDependencyLoaded("DynShop"))
                isDynShopPresent = true;
            if (IsDependencyLoaded("Uconomy"))
                isUconomyPresent = true;
            if (IsDependencyLoaded("PlayerInfoLib"))
                isPlayerInfoLibPresent = true;
            if (isDynShopPresent)
                isDynShopLoaded = IsDynShopLoaded();
            if (isUconomyPresent)
                isUconomyLoaded = IsUconomyLoaded();
            if (isPlayerInfoLibPresent)
                isPlayerInfoLibLoaded = IsPlayerInfoLibLoaded();
        }

        private void OnPluginsLoading(IRocketPlugin plugin, ref bool cancelLoading)
        {
            // Check final plugin load state.
            new Thread((ThreadStart)(() =>
            {
                Thread.Sleep(2000);
                if (debug)
                    Logger.LogWarning("Checking plugin dependencies for WB plugin.");
                if (plugin.Name == "DynShop")
                    isDynShopLoaded = IsDynShopLoaded();
                if (plugin.Name == "Uconomy")
                    isUconomyLoaded = IsUconomyLoaded();
                if (plugin.Name == "PlayerInfoLib")
                    isPlayerInfoLibLoaded = IsPlayerInfoLibLoaded();
                if (debug)
                {
                    Logger.Log("DynShop: " + isDynShopLoaded.ToString());
                    Logger.Log("Uconomy: " + isUconomyLoaded.ToString());
                    Logger.Log("PlayerInfoLib: " + isPlayerInfoLibLoaded.ToString());
                }
            }))
            { IsBackground = true }.Start();

        }
        private void OnPluginsUnloading(IRocketPlugin plugin)
        {
            if (plugin.Name == "DynShop")
                isDynShopLoaded = false;
            if (plugin.Name == "Uconomy")
                isUconomyLoaded = false;
            if (plugin.Name == "PlayerInfoLib")
                isPlayerInfoLibLoaded = false;
        }

        private bool IsPlayerInfoLibLoaded()
        {
            return PlayerInfoLib.Instance.State == PluginState.Loaded && PlayerInfoLib.Database.Initialized;
        }

        private bool IsDynShopLoaded()
        {
            return DShop.Instance.State == PluginState.Loaded && DShop.Instance.Database.IsLoaded;
        }

        private bool IsUconomyLoaded()
        {
            return Uconomy.Instance.State == PluginState.Loaded;
        }

        private static void CheckCleanup()
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

        public void VehicleBuyBack(InteractableVehicle vehicle)
        {
            try
            {
                if (vehicle.isLocked && vehicle.lockedOwner != CSteamID.Nil)
                {
                    VehicleInfo info = DShop.Instance.Database.GetVehicleInfo((ulong)vehicle.lockedOwner, vehicle.id);
                    ShopObject svehicle = DShop.Instance.Database.GetItem(ItemType.Vehicle, vehicle.id);
                    if (info != null && svehicle.ItemID == vehicle.id && svehicle.RestrictBuySell != RestrictBuySell.BuyOnly)
                    {
                        Uconomy.Instance.Database.CheckSetupAccount(vehicle.lockedOwner);
                        Uconomy.Instance.Database.IncreaseBalance(vehicle.lockedOwner.ToString(), Math.Round(decimal.Multiply(svehicle.BuyCost, svehicle.SellMultiplier), 2));
                        DShop.Instance.Database.DeleteVehicleInfo(info);
                        bool getPInfo = WreckingBall.isPlayerInfoLibLoaded;
                        Logger.Log(string.Format("Vehicle buyback successfull for: InstanceID: {0}, and Type: {1}({2}), at position: {3} , Sign By: {4}, Locked By: {5}.",
                            vehicle.instanceID,
                            vehicle.asset.vehicleName,
                            vehicle.id, vehicle.transform.ToString(),
                            DestructionProcessing.HasFlaggedElement(vehicle.transform, out ulong vFlagOwner) ? (getPInfo ? Instance.PInfoGenerateMessage(vFlagOwner) : vFlagOwner.ToString()) : "N/A",
                            vehicle.isLocked ? (getPInfo ? Instance.PInfoGenerateMessage((ulong)vehicle.lockedOwner) : vehicle.lockedOwner.ToString()) : "N/A"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "There was an error with trying to process a vehicle buyback.");
            }
        }

        public void VehicleElementDrop(InteractableVehicle vehicle, bool shouldDestroy = true, Transform trainCarTransform = null)
        {
            if ((vehicle.asset.engine == EEngine.TRAIN && trainCarTransform == null) || vehicle.isDead)
                return;
            if (BarricadeManager.tryGetPlant(vehicle.asset.engine == EEngine.TRAIN ? trainCarTransform : vehicle.transform, out byte x, out byte y, out ushort plant, out BarricadeRegion vregion))
            {
                for (int i = vregion.drops.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        Item item = new Item(vregion.barricades[i].barricade.id, true);
                        ItemManager.dropItem(item, vregion.drops[i].model.position, false, true, true);
                    }
                    catch (Exception ex)
                    {
                        // You can get an error, if there's a mismatch in the barricades and drops lists for the region(should rarely ever happen, mostly ever happens when elements are destroyed by a thread outside of the main thread, eg by plugin.).
                        Logger.LogException(ex, "Error in dropping an element off of the vehicle.");
                    }
                    if (shouldDestroy)
                    {
                        try
                        {
                            BarricadeManager.damage(vregion.drops[i].model, ushort.MaxValue, 1, false);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex, "Error in destroying vehicle barricade.");
                        }
                    }
                }
            }
        }

        public string PInfoGenerateMessage(ulong owner)
        {
            PlayerData pData = PlayerInfoLib.Database.QueryById((CSteamID)owner);
            string msg = string.Empty;
            if (pData.IsValid())
                msg = string.Format("{0} {1} [{2}], Seen: {3}:{4}{5}", owner, pData.CharacterName, pData.SteamName, pData.IsLocal() ? "L" : "G", pData.IsLocal() ? pData.LastLoginLocal : pData.LastLoginGlobal, pData.IsLocal() ? string.Format(", IsVip: {0}", pData.IsVip()) : string.Empty);
            else
                msg = string.Format("{0}, No Player Info.", owner);
            return msg;
        }

        internal void DCUSet(IRocketPlayer caller, string[] cmd)
        {
            if (cmd.Length == 0 || cmd.Length > 1)
            {
                UnturnedChat.Say(caller, Translate("werckingball_dcu_help"));
                return;
            }
            else
            {
                ulong steamID = 0;
                UnturnedPlayer player = null;
                if (!cmd[0].isCSteamID(out steamID))
                {
                    player = UnturnedPlayer.FromName(cmd[0]);
                    if (player == null)
                    {
                        UnturnedChat.Say(caller, Translate("wreckingball_dcu_player_not_found"), Color.red);
                        return;
                    }
                    steamID = (ulong)player.CSteamID;
                }
                PlayerData pData = PlayerInfoLib.Database.QueryById((CSteamID)steamID, false);
                if (!pData.IsLocal())
                {
                    UnturnedChat.Say(caller, Translate("wreckingball_dcu_hasnt_played"), Color.red);
                    return;
                }
                if (pData.CleanedBuildables && pData.CleanedPlayerData)
                {
                    PlayerInfoLib.Database.SetOption(pData.SteamID, OptionType.Buildables, false);
                    PlayerInfoLib.Database.SetOption(pData.SteamID, OptionType.PlayerFiles, false);
                    UnturnedChat.Say(caller, Translate("wreckingball_dcu_cleanup_enabled", pData.CharacterName, pData.SteamName, pData.SteamID));

                }
                else
                {
                    PlayerInfoLib.Database.SetOption(pData.SteamID, OptionType.Buildables, true);
                    PlayerInfoLib.Database.SetOption(pData.SteamID, OptionType.PlayerFiles, true);
                    UnturnedChat.Say(caller, Translate("wreckingball_dcu_cleanup_disabled", pData.CharacterName, pData.SteamName, pData.SteamID));
                }
            }
        }

        internal void Scan(IRocketPlayer caller, string filter, float radius, Vector3 position, FlagType flagType, ulong steamID, ushort itemID)
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
                    if (radius.IsNaN())
                        UnturnedChat.Say(caller, Translate("wreckingball_scan_nan_check"));
                    UnturnedChat.Say(caller, Translate("wreckingball_scan", totalCount, type, radius.ToString(), report));
                    if (Instance.Configuration.Instance.LogScans && !(caller is ConsolePlayer))
                        Logger.Log(Translate("wreckingball_scan", totalCount, type, radius.ToString(), report));
                }
            }
            else
            {
                UnturnedChat.Say(caller, Translate("wreckingball_not_found", radius));
            }



        }

        internal void Teleport(IRocketPlayer caller, TeleportType teleportType, ulong ulSteamID)
        {

            if (StructureManager.regions.LongLength == 0 && BarricadeManager.BarricadeRegions.LongLength == 0)
            {
                UnturnedChat.Say(caller, Translate("wreckingball_map_clear"));
                return;
            }

            UnturnedPlayer player = (UnturnedPlayer)caller;

            Vector3 tpVector;
            bool match = false;
            int tries = 0;
            int x = 0;
            int xCount = 0;
            int z = 0;
            int zCount = 0;
            int idx = 0;
            int idxCount = 0;

            Transform current = null;
            List<Destructible> items = new List<Destructible>();
            // Steam id matching.
            if (ulSteamID != 0)
            {
                switch (teleportType)
                {
                    case TeleportType.Structures:
                        xCount = StructureManager.regions.GetLength(0);
                        zCount = StructureManager.regions.GetLength(1);
                        for (x = 0; x < xCount; x++)
                        {
                            for (z = 0; z < zCount; z++)
                            {
                                idxCount = StructureManager.regions[x, z].drops.Count;
                                for (int k = 0; k < idxCount; k++)
                                {
                                    if (StructureManager.regions[x, z].structures[k].owner == ulSteamID)
                                        items.Add(new Destructible(StructureManager.regions[x, z].drops[k].model, ElementType.Structure, StructureManager.regions[x, z].structures[k].structure.id));
                                }
                            }
                        }
                        if (items.Count > 0)
                        {
                            idx = UnityEngine.Random.Range(0, items.Count - 1);
                            try
                            {
                                current = items[idx].Transform;
                                match = true;
                            }
                            catch
                            { }
                        }
                        break;
                    case TeleportType.Barricades:
                        xCount = BarricadeManager.regions.GetLength(0);
                        zCount = BarricadeManager.regions.GetLength(1);
                        for (x = 0; x < xCount; x++)
                        {
                            for (z = 0; z < zCount; z++)
                            {
                                idxCount = BarricadeManager.regions[x, z].drops.Count;
                                for (int k = 0; k < idxCount; k++)
                                {
                                    if (BarricadeManager.regions[x, z].barricades[k].owner == ulSteamID)
                                        items.Add(new Destructible(BarricadeManager.regions[x, z].drops[k].model, ElementType.Barricade, BarricadeManager.regions[x, z].barricades[k].barricade.id));
                                }
                            }
                        }
                        if (items.Count > 0)
                        {
                            idx = UnityEngine.Random.Range(0, items.Count - 1);
                            try
                            {
                                current = items[idx].Transform;
                                match = true;
                            }
                            catch
                            { }
                        }
                        break;
                    case TeleportType.Vehicles:
                        int vCount = VehicleManager.vehicles.Count;
                        for (x = 0; x < vCount; x++)
                        {
                            if (VehicleManager.vehicles[x].isLocked && (ulong)VehicleManager.vehicles[x].lockedOwner == ulSteamID)
                            {
                                Logger.Log(VehicleManager.vehicles[x].lockedOwner.ToString() + ":"+ VehicleManager.vehicles[x].instanceID.ToString());
                                items.Add(new Destructible(VehicleManager.vehicles[x].transform, ElementType.Vehicle, VehicleManager.vehicles[x].id, VehicleManager.vehicles[x]));
                            }
                        }
                        if (items.Count > 0)
                        {
                            idx = UnityEngine.Random.Range(0, items.Count - 1);
                            Logger.Log(idx.ToString());
                            try
                            {
                                current = items[idx].Transform;
                                match = true;
                            }
                            catch
                            { }
                        }
                        break;
                }
            }
            // Standard random search.
            else
            {
                while (tries < 2000 && !match)
                {
                    tries++;
                    switch (teleportType)
                    {
                        case TeleportType.Structures:
                            xCount = StructureManager.regions.GetLength(0);
                            zCount = StructureManager.regions.GetLength(1);
                            if (xCount == 0)
                                continue;
                            x = UnityEngine.Random.Range(0, xCount - 1);
                            if (zCount == 0)
                                continue;
                            z = UnityEngine.Random.Range(0, zCount - 1);
                            idxCount = StructureManager.regions[x, z].structures.Count;
                            if (idxCount == 0)
                                continue;
                            idx = UnityEngine.Random.Range(0, idxCount - 1);

                            try
                            {
                                current = StructureManager.regions[x, z].drops[idx].model;
                            }
                            catch
                            {
                                continue;
                            }

                            if (Vector3.Distance(current.position, player.Position) > 20)
                                match = true;
                            break;
                        case TeleportType.Barricades:
                            xCount = BarricadeManager.BarricadeRegions.GetLength(0);
                            zCount = BarricadeManager.BarricadeRegions.GetLength(1);
                            if (xCount == 0)
                                continue;
                            x = UnityEngine.Random.Range(0, xCount - 1);
                            if (zCount == 0)
                                continue;
                            z = UnityEngine.Random.Range(0, zCount - 1);
                            idxCount = BarricadeManager.BarricadeRegions[x, z].drops.Count;
                            if (idxCount == 0)
                                continue;
                            idx = UnityEngine.Random.Range(0, idxCount - 1);

                            try
                            {
                                current = BarricadeManager.BarricadeRegions[x, z].drops[idx].model;
                            }
                            catch
                            {
                                continue;
                            }

                            if (Vector3.Distance(current.position, player.Position) > 20)
                                match = true;
                            break;
                        case TeleportType.Vehicles:
                            int vCount = VehicleManager.vehicles.Count;
                            int vRand = UnityEngine.Random.Range(0, vCount - 1);
                            try
                            {
                                current = VehicleManager.vehicles[vRand].transform;
                            }
                            catch
                            {
                                continue;
                            }
                            if (Vector3.Distance(current.position, player.Position) > 20)
                                match = true;
                            break;
                        default:
                            return;
                    }
                }
            }
            if(match)
            {
                tpVector = new Vector3(current.position.x, teleportType == TeleportType.Vehicles ? current.position.y + 4 : current.position.y + 2, current.position.z);
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
                UnturnedChat.Say(caller, Instance.Translate("wreckingball_help"));
            }
            else
            {
                DestructionProcessing.processing = true;
                if (!(caller is ConsolePlayer))
                    DestructionProcessing.originalCaller = (UnturnedPlayer)caller;
                UnturnedChat.Say(caller, Translate("wreckingball_initiated", DestructionProcessing.CalcProcessTime()));
                Logger.Log(string.Format("Player {0} has initiated wreck.", caller is ConsolePlayer ? "Console" : ((UnturnedPlayer)caller).CharacterName + " [" + ((UnturnedPlayer)caller).SteamName + "] (" + ((UnturnedPlayer)caller).CSteamID.ToString() + ")"));
                DestructionProcessing.dIdxCount = DestructionProcessing.destroyList.Count;
                DestructionProcessing.dIdx = 0;
            }
        }

        // Changed timer to Update(), to attempt to fix ghost objects bug by syncing the destructions to the game frame/tic.
        public void Update()
        {
            if (Instance.State == PluginState.Loaded)
            {
                if ((DateTime.Now - DestructionProcessing.lastRunTimeWreck).TotalSeconds > (1 / Instance.Configuration.Instance.DestructionRate))
                {
                    DestructionProcessing.lastRunTimeWreck = DateTime.Now;
                    if (DestructionProcessing.processing)
                    {
                        try
                        {
                            DestructionProcessing.DestructionLoop(WreckType.Wreck);
                        }
                        catch
                        {
                            DestructionProcessing.Abort(WreckType.Wreck);
                        }
                    }
                    if (DestructionProcessing.cleanupProcessingBuildables)
                    {
                        try
                        {
                            DestructionProcessing.DestructionLoop(WreckType.Cleanup);
                        }
                        catch
                        {
                            DestructionProcessing.Abort(WreckType.Cleanup);
                        }
                    }
                }
                if (Instance.Configuration.Instance.EnableCleanup)
                    DestructionProcessing.HandleCleanup();
                if (Instance.Configuration.Instance.EnableVehicleCap)
                    DestructionProcessing.HandleVehicleCap();
            }
        }

        // Translations
        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    { "wreckingball_lv_help", "<radius> - distance to scan cars." },
                    { "wreckingball_lv2_vehicle", "Vehicle: {5}({6}) position: {0}, with InstanceID: {1}, Barricade count on car: {2}, Sign By: {3}, Locked By: {4}." },
                    { "wreckingball_lv2_traincar", "Vehicle: Train Car#{5} position: {0}, with InstanceID: {1}, Barricade count on Train Car: {2}, Sign By: {3}, Locked By: {4}." },
                    { "werckingball_dcu_help", "<\"playername\" | SteamID> - disables cleanup on a player." },
                    { "werckingball_dcu_not_enabled", "This command can only be used if the cleanup feature is enabled on the server." },
                    { "wreckingball_dcu_player_not_found", "Couldn't find a player by that name on the server." },
                    { "wreckingball_dcu_hasnt_played", "Player hasn't played on this server yet." },
                    { "wreckingball_dcu_cleanup_disabled", "Auto Cleanup has been disabled for player {0} [{1}] ({2})" },
                    { "wreckingball_dcu_cleanup_enabled", "Auto Cleanup has been enabled for player {0} [{1}] ({2})" },
                    { "wreckingball_scan", "Found {0} elements of type: {1}, @ {2}m:{3}" },
                    { "wreckingball_scan_nan_check", "Running NaN Position check!" },
                    { "wreckingball_map_clear", "Map has no elements!" },
                    { "wreckingball_not_found", "No elements found in a {0} radius!" },
                    { "wreckingball_complete", "Wrecking Ball complete! {0} elements(s) Destroyed!" },
                    { "wreckingball_initiated", "Wrecking Ball initiated: ~{0} sec(s) left." },
                    { "wreckingball_processing", "Wrecking Ball started by: {0}, {1} element(s) left to destroy, ~{2} sec(s) left." },
                    { "wreckingball_aborted", "Wrecking Ball Aborted! Destruction queue cleared!" },
                    { "wreckingball_help", "Please define filter and radius: /wreck <filter> <radius> or /wreck teleport b|s" },
                    { "wreckingball_help_console", "Please define filter, radius and position: /wreck <filter> <radius> <x> <y> <z>" },
                    { "wreckingball_help_teleport2", "Please define type for teleport: /wreck teleport <s|b|v>, or /w teleport <steamid> <s|b|v>" },
                    { "wreckingball_help_scan", "Please define a scan filter and radius: /wreck scan <filter> <radius>" },
                    { "wreckingball_help_scan_console", "Please define a scan filter, radius and position: /wreck scan <filter> <radius> <x> <y> <z>" },
                    { "wreckingball_queued", "{0} elements(s) found, ~{1} sec(s) to complete run." },
                    { "wreckingball_prompt", "Type '/wreck confirm' or '/wreck abort'" },
                    { "wreckingball_structure_array_sync_error", "Warning: Structure arrays out of sync, need to restart server." },
                    { "wreckingball_barricade_array_sync_error", "Warning: Barricade arrays out of sync, need to restart server." },
                    { "wreckingball_sync_error", "Warning: Element array sync error, not all elements will be cleaned up in range, server should be restarted." },
                    { "wreckingball_teleport_not_found", "Couldn't find any elements to teleport to, try to run the command again." },
                    { "wreckingball_teleport_not_allowed", "Not allowed to use wreck teleport from the console." },
                    { "wreckingball_reload_abort", "Warning: Current wreck job in progress has been aborted from a plugin reload." },
                    { "wreckingball_wreck_permission", "You need to have the permissions wreck.wreck, or wreck.* to be able to run a wreck." },
                    { "wreckingball_scan_permission", "You need to have the permissions wreck.scan, or wreck.* to be able to run a scan." },
                    { "wreckingball_teleport_permission", "You need to have the permissions wreck.teleport, or wreck.* to be able to run a teleport." },
                };
            }
        }
    }
}
