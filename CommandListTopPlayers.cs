using Rocket.API;
using Rocket.Unturned.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;

namespace ApokPT.RocketPlugins
{
    class CommandListTopPlayers : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>() { "listtp" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return "Gets the elements counts for players on the server, displays the top counts."; }
        }

        public string Name
        {
            get { return "listtopplayers"; }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "wreckingball.listtopplayers" }; }
        }

        public string Syntax
        {
            get { return ""; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            // Get player elements list.
            DestructionProcessing.Wreck(caller, "", 0, Vector3.zero, WreckType.Counts, FlagType.SteamID, 0, 0);
            // Grab what we need from the list.
            Dictionary<ulong, int> shortenedList = DestructionProcessing.pElementCounts.Where(r => r.Value >= WreckingBall.Instance.Configuration.Instance.PlayerElementListCutoff).OrderBy(v => v.Value).ToDictionary(k => k.Key, v => v.Value);
            DestructionProcessing.pElementCounts.Clear();

            bool getPInfo = false;
            if (WreckingBall.Instance.Configuration.Instance.EnablePlayerInfo)
                getPInfo = WreckingBall.IsPInfoLibLoaded();

            foreach (KeyValuePair<ulong, int> value in shortenedList)
            {
                string msg = string.Format("Element count: {0}, Player: {1}", value.Value, !getPInfo || value.Key == 0 ? value.Key.ToString() : WreckingBall.Instance.PInfoGenerateMessage(value.Key));
                if (caller is ConsolePlayer)
                    Logger.Log(msg, ConsoleColor.Yellow);
                else
                    UnturnedChat.Say(caller, msg, Color.yellow);
            }
        }
    }
}
