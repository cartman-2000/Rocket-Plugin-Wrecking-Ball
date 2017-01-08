using Rocket.API;
using Rocket.Unturned.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ApokPT.RocketPlugins
{
    class CommandDisableCleanup : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>() { "disablec" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return "Disables cleanup on a player."; }
        }

        public string Name
        {
            get { return "disablecleanup"; }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "wreckingball.disablecleanup" }; }
        }

        public string Syntax
        {
            get { return "<\"playername\" | SteamID>"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (!WreckingBall.Instance.Configuration.Instance.EnableCleanup)
            {
                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("werckingball_dcu_not_enabled"), Color.red);
                return;
            }
            else
            {
                WreckingBall.Instance.DCUSet(caller, command);
            }
        }
    }
}
