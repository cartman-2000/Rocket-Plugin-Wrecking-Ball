using Rocket.API;
using Rocket.Core;
using Rocket.Core.RCON;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;

namespace ApokPT.RocketPlugins
{
    public class ElementDataManager
    {
        internal Dictionary<char, Category> categorys = new Dictionary<char, Category>();
        internal Dictionary<ushort, Element> elements = new Dictionary<ushort, Element>();

        internal Dictionary<BuildableType, Dictionary<char, uint>> reportLists = new Dictionary<BuildableType, Dictionary<char, uint>>();

        public ElementDataManager()
        {
            Load();
            reportLists.Add(BuildableType.Element, new Dictionary<char, uint>());
            reportLists.Add(BuildableType.VehicleElement, new Dictionary<char, uint>());
        }

        private void Load()
        {
            foreach(Category category in WreckingBall.Instance.Configuration.Instance.Categories)
            {
                if(category.Id == '*')
                {
                    Logger.LogError("Error: Invalid Category Id for this Category: " + category.Name + ", skipping");
                }
                if (categorys.ContainsKey(category.Id))
                {
                    Logger.LogWarning("Warning: Duplicate Category Id: '" + category.Id.ToString() + "' detected, skipping.");
                    continue;
                }
                categorys.Add(category.Id,category);
            }
            foreach(Element element in WreckingBall.Instance.Configuration.Instance.Elements)
            {
                if (element.CategoryId == '*')
                {
                    Logger.LogError("Error: Invalid Category Id for this Element ID: " + element.Id.ToString() + ", skipping");
                }
                if (elements.ContainsKey(element.Id))
                {
                    Logger.LogWarning("Warning: Duplicate Element ID: " + element.Id.ToString() + " detected, skipping.");
                    continue;
                }
                if(!categorys.ContainsKey(element.CategoryId))
                {
                    Logger.LogWarning("Warning: No Category was found for this Element ID: " + element.Id.ToString() + ", skipping.");
                    continue;
                }
                elements.Add(element.Id, element);
            }
        }

        internal bool filterItem(ushort itemId, List<char> userRequest)
        {
            return ((elements.ContainsKey(itemId) && userRequest.Contains(elements[itemId].CategoryId)) || (!elements.ContainsKey(itemId) && userRequest.Contains('!')));
        }

        internal void report(IRocketPlayer caller, ushort itemId, float range, bool printConsole, bool getPinfo, object data, BuildableType type = BuildableType.Element, int count = 0, ulong lockedOwner = 0, int vindex = 0)
        {
            Category cat;
            if (!elements.ContainsKey(itemId))
            {
                if (type == BuildableType.VehicleElement)
                {
                    if (reportLists[BuildableType.VehicleElement].ContainsKey('!'))
                        reportLists[BuildableType.VehicleElement]['!'] += 1;
                    else
                        reportLists[BuildableType.VehicleElement].Add('!', 1);
                    cat = categorys['!'];
                }
                else
                {
                    if (reportLists[BuildableType.Element].ContainsKey('!'))
                        reportLists[BuildableType.Element]['!'] += 1;
                    else
                        reportLists[BuildableType.Element].Add('!', 1);
                    cat = categorys['!'];
                }
            }
            else
            {
                if (type == BuildableType.VehicleElement)
                {
                    if (reportLists[BuildableType.VehicleElement].ContainsKey(elements[itemId].CategoryId))
                        reportLists[BuildableType.VehicleElement][elements[itemId].CategoryId] += 1;
                    else
                        reportLists[BuildableType.VehicleElement].Add(elements[itemId].CategoryId, 1);
                    cat = categorys[elements[itemId].CategoryId];
                }
                else
                {
                    if (reportLists[BuildableType.Element].ContainsKey(elements[itemId].CategoryId))
                        reportLists[BuildableType.Element][elements[itemId].CategoryId] += 1;
                    else
                        reportLists[BuildableType.Element].Add(elements[itemId].CategoryId, 1);
                    cat = categorys[elements[itemId].CategoryId];
                }
            }
            if (printConsole || !elements.ContainsKey(itemId))
            {
                string stype = type == BuildableType.VehicleElement ? "Vehicle Element: " : "Element: ";
                string msg = string.Empty;
                ulong owner = 0;
                InteractableVehicle vehicle = null;
                StructureData sData = null;
                BarricadeData bData = null;
                string eName = string.Empty;
                if (data is BarricadeData)
                {
                    bData = data as BarricadeData;
                    owner = bData.owner;
                    eName = bData.barricade.asset.itemName;
                }
                else if (data is StructureData)
                {
                    sData = data as StructureData;
                    owner = sData.owner;
                    eName = sData.structure.asset.itemName;
                }
                else if (data is InteractableVehicle)
                {
                    vehicle = data as InteractableVehicle;
                    itemId = vehicle.id;
                    eName = vehicle.asset.vehicleName;
                }
                if (type == BuildableType.Vehicle)
                {
                    ulong signOwner = 0;
                    DestructionProcessing.HasFlaggedElement(vindex > 0 ? vehicle.trainCars[vindex].root : vehicle.transform, out signOwner);
                    msg = string.Format("{0}{1} (Id: {6}:{2}, Instance ID: {8}) @ {3}m, Barricade Count: {4}, Sign by: {7}, Locked By: {5}", stype, cat.Name, itemId, Math.Round(range, 2), count, lockedOwner > 0 ? getPinfo ? WreckingBall.Instance.PInfoGenerateMessage(lockedOwner) : lockedOwner.ToString() : "N/A", vindex > 0 ? "Train car#" + vindex : eName, signOwner > 0 ? getPinfo ? WreckingBall.Instance.PInfoGenerateMessage(signOwner) : signOwner.ToString() : "N/A", vehicle.instanceID.ToString());
                }
                else
                {
                    // Generate the message in another method, as the Player info one requires special data types that have to be loaded before executing a method.
                    msg = string.Format("{0}{1} (Id: {5}:{2}) @ {3}m, Owner: {4}", stype, cat.Name, itemId, Math.Round(range, 2), owner > 0 ? getPinfo ? WreckingBall.Instance.PInfoGenerateMessage(owner) : owner.ToString() : "N/A", eName);
                }


                if (WreckingBall.Instance.Configuration.Instance.LogScans)
                    Logger.Log(msg, cat.Color);
                else
                {
                    Console.ForegroundColor = cat.Color;
                    Console.WriteLine(msg);
                    Console.ResetColor();
                    if (R.Settings.Instance.RCON.Enabled)
                        RCONServer.Broadcast(msg);
                }
                if (WreckingBall.Instance.Configuration.Instance.PrintToChat && !(caller is ConsolePlayer))
                    UnturnedChat.Say(caller, msg, Color.yellow);
            }
        }
    }
}