using PlayerInfoLibrary;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.RCON;
using Rocket.Unturned.Chat;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        internal void report(IRocketPlayer caller, ushort itemId, float range, bool printConsole, bool getPinfo, BuildableType type = BuildableType.Element, ulong owner = 0)
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
                if (owner == 0 && type != BuildableType.Vehicle)
                    msg = string.Format("{0}{1} (Id: {2}) @ {3}m", stype, cat.Name, itemId, Math.Round(range, 2));
                else if (type == BuildableType.Vehicle)
                    msg = string.Format("{0}{1} (Id: {2}) @ {3}m, Barricade Count: {4}", stype, cat.Name, itemId, Math.Round(range, 2), owner);
                else
                {
                    if (getPinfo)
                        // Generate the message in another method, as the Player info one requires special data types that have to be loaded before executing a method.
                        msg = PInfoGenerateMessage(stype, type, cat, itemId, range, owner);
                    else
                        msg = string.Format("{0}{1} (Id: {2}) @ {3}m, Owner: {4}", stype, cat.Name, itemId, Math.Round(range, 2), owner);
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

        private string PInfoGenerateMessage(string stype, BuildableType type, Category cat, ushort itemId, float range, ulong owner)
        {
            PlayerData pData = PlayerInfoLib.Database.QueryById((CSteamID)owner);
            string msg = string.Empty;
            if (pData.IsValid())
                msg = string.Format("{0}{1} (Id: {2}) @ {3}m, Owner: {4} {5} [{6}], Seen: {7}:{8}", stype, cat.Name, itemId, Math.Round(range, 2), owner, pData.CharacterName, pData.SteamName, pData.IsLocal() ? "L" : "G", pData.IsLocal() ? pData.LastLoginLocal : pData.LastLoginGlobal);
            else
                msg = string.Format("{0}{1} (Id: {2}) @ {3}m, Owner: {4}, {5}", stype, cat.Name, itemId, Math.Round(range, 2), owner, "No Player Info.");
            return msg;
        }
    }
}