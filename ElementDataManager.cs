using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.RCON;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApokPT.RocketPlugins
{
    public class ElementDataManager
    {
        internal Dictionary<char, Category> categorys = new Dictionary<char, Category>();
        internal Dictionary<ushort, Element> elements = new Dictionary<ushort, Element>();

        internal Dictionary<char, uint> reportList = new Dictionary<char, uint>();

        public ElementDataManager()
        {
            Load();
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

        internal void report(UnturnedPlayer caller, ushort itemId, float range, bool printConsole, ulong owner = 0)
        {
            Category cat;
            if (!elements.ContainsKey(itemId))
            {
                if (reportList.ContainsKey('!'))
                    reportList['!'] += 1;
                else
                    reportList.Add('!', 1);
                cat = categorys['!'];
            }
            else
            {
                if (reportList.ContainsKey(elements[itemId].CategoryId))
                    reportList[elements[itemId].CategoryId] += 1;
                else
                    reportList.Add(elements[itemId].CategoryId, 1);
                cat = categorys[elements[itemId].CategoryId];
            }
            if (printConsole || !elements.ContainsKey(itemId))
            {
                string msg = string.Empty;
                if (owner == 0)
                    msg = cat.Name + " (Id: " + itemId + ") @ " + Math.Round(range, 2) + "m";
                else
                    msg = cat.Name + " (Id: " + itemId + ") @ " + Math.Round(range, 2) + "m, Owner: " + owner;

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
                if (WreckingBall.Instance.Configuration.Instance.PrintToChat)
                    UnturnedChat.Say(caller, msg, Color.yellow);
            }
        }
    }
}