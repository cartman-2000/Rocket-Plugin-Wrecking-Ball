using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using UnityEngine;
using System.Collections.Generic;

using Logger = Rocket.Core.Logging.Logger;

namespace ApokPT.RocketPlugins
{
    public class CommandWreck : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>() { "w" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return "Destroy everything in a specific radius!"; }
        }

        public string Name
        {
            get { return "wreck"; }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "wreckingball.wreck" }; }
        }

        public string Syntax
        {
            get { return "Scan: /w scan <Flag|ItemID> <Radius> | /w scan <SteamID> <Flag|ItemID> <Radius>, Destruct: /w <Flag|ItemID> <Radius> | /w <SteamID> <Flag|ItemID> <Radius>, Teleport: /w teleport <b|s|v>"; }
        }

        public void Execute(IRocketPlayer caller, string[] cmd)
        {

            string command = String.Join(" ", cmd);

            if (!caller.IsAdmin && !WreckingBall.Instance.Configuration.Instance.Enabled) return;

            if (!caller.IsAdmin && !caller.HasPermission("wreck")) return;

            UnturnedPlayer player = null;
            Vector3 position = Vector3.zero;
            if (!(caller is ConsolePlayer))
            {
                player = (UnturnedPlayer)caller;
                position = player.Position;
            }


            if (String.IsNullOrEmpty(command.Trim()))
            {
                if (DestructionProcessing.processing)
                    DestructionProcessing.Wreck(caller, "", 0, position, WreckType.Wreck, FlagType.Normal, 0, 0);
                else
                {
                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
                    return;
                }
            }
            else
            {
                string[] oper = command.Split(' ');

                if (oper.Length >= 1)
                {
                    switch (oper[0])
                    {
                        case "confirm":
                            if (!(caller.HasPermission("wreck.wreck") || caller.HasPermission("wreck.*")) && !(caller is ConsolePlayer))
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_wreck_permission"), Color.red);
                                return;
                            }
                            WreckingBall.Instance.Confirm(caller);
                            break;
                        case "abort":
                            if (!(caller.HasPermission("wreck.wreck") || caller.HasPermission("wreck.*")) && !(caller is ConsolePlayer))
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_wreck_permission"), Color.red);
                                return;
                            }
                            if (!(caller is ConsolePlayer))
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_aborted"));
                            Logger.Log(WreckingBall.Instance.Translate("wreckingball_aborted"));
                            DestructionProcessing.Abort(WreckType.Wreck);
                            break;
                        case "scan":
                            if (!(caller.HasPermission("wreck.scan") || caller.HasPermission("wreck.*")) && !(caller is ConsolePlayer))
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_scan_permission"), Color.red);
                                return;
                            }
                            if ((oper.Length == 3 && !(caller is ConsolePlayer)) || (oper.Length == 6 && caller is ConsolePlayer))
                            {
                                if (caller is ConsolePlayer)
                                {
                                    if (!cmd.GetVectorFromCmd(3, out position))
                                    {
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan_console"));
                                        break;
                                    }
                                }
                                if (ushort.TryParse(oper[1], out ushort itemID))
                                    WreckingBall.Instance.Scan(caller, oper[1], Convert.ToSingle(oper[2]), position, FlagType.ItemID, 0, itemID);
                                else
                                {
                                    if (oper[2].ToLower() == "nan")
                                        WreckingBall.Instance.Scan(caller, oper[1], float.NaN, position, FlagType.Normal, 0, 0);
                                    else
                                        WreckingBall.Instance.Scan(caller, oper[1], Convert.ToSingle(oper[2]), position, FlagType.Normal, 0, 0);
                                }
                            }
                            else if ((oper.Length == 4 && !(caller is ConsolePlayer)) || (oper.Length == 7 && caller is ConsolePlayer))
                            {
                                if (caller is ConsolePlayer)
                                {
                                    if (!cmd.GetVectorFromCmd(4, out position))
                                    {
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan_console"));
                                        break;
                                    }
                                }
                                if (oper[1].IsCSteamID(out ulong steamID))
                                    WreckingBall.Instance.Scan(caller, oper[2], Convert.ToSingle(oper[3]), position, FlagType.SteamID, (ulong)steamID, 0);
                                else
                                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan"));
                            }
                            else
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan"));
                            }

                            break;
                        case "teleport":
                            if (!(caller.HasPermission("wreck.teleport") || caller.HasPermission("wreck.*")) && !(caller is ConsolePlayer))
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_teleport_permission"), Color.red);
                                return;
                            }
                            ulong ulSteamID = 0;
                            bool firstSteamID = false;
                            if (oper.Length > 1 && oper[1].IsCSteamID(out ulSteamID))
                                firstSteamID = true;
                            if (oper.Length > 1)
                            {

                                if (caller is ConsolePlayer)
                                {
                                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_teleport_not_allowed"));
                                    break;
                                }
                                string ch = string.Empty;
                                if (firstSteamID && oper.Length > 2)
                                    ch = oper[2];
                                else if (!firstSteamID)
                                    ch = oper[1];
                                switch (ch)
                                {
                                    case "b":
                                        WreckingBall.Instance.Teleport(player, TeleportType.Barricades, ulSteamID);
                                        break;
                                    case "s":
                                        WreckingBall.Instance.Teleport(player, TeleportType.Structures, ulSteamID);
                                        break;
                                    case "v":
                                        WreckingBall.Instance.Teleport(player, TeleportType.Vehicles, ulSteamID);
                                        break;
                                    default:
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_teleport2"));
                                        break;
                                }
                            }
                            else
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_teleport2"));
                                break;
                            }
                            break;
                        default:
                            if (!(caller.HasPermission("wreck.wreck") || caller.HasPermission("wreck.*")) && !(caller is ConsolePlayer))
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_wreck_permission"), Color.red);
                                return;
                            }
                            if ((oper.Length == 2 && !(caller is ConsolePlayer)) || (oper.Length == 5 && caller is ConsolePlayer))
                            {
                                if (caller is ConsolePlayer)
                                {
                                    if (!cmd.GetVectorFromCmd(2, out position))
                                    {
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_console"));
                                        break;
                                    }
                                }
                                if (ushort.TryParse(oper[0], out ushort itemID))
                                    DestructionProcessing.Wreck(caller, oper[0], Convert.ToSingle(oper[1]), position, WreckType.Wreck, FlagType.ItemID, 0, itemID);
                                else
                                {
                                    if (oper[1].ToLower() == "nan")
                                        DestructionProcessing.Wreck(caller, oper[0], float.NaN, position, WreckType.Wreck, FlagType.Normal, 0, 0);
                                    else
                                        DestructionProcessing.Wreck(caller, oper[0], Convert.ToSingle(oper[1]), position, WreckType.Wreck, FlagType.Normal, 0, 0);
                                }
                            }
                            else if ((oper.Length == 3 && !(caller is ConsolePlayer)) || (oper.Length == 6 && caller is ConsolePlayer))
                            {
                                if (caller is ConsolePlayer)
                                {
                                    if (!cmd.GetVectorFromCmd(3, out position))
                                    {
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_console"));
                                        break;
                                    }
                                }
                                if (oper[0].IsCSteamID(out ulong steamID))
                                    DestructionProcessing.Wreck(caller, oper[1], Convert.ToSingle(oper[2]), position, WreckType.Wreck, FlagType.SteamID, steamID, 0);
                                else
                                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
                            }
                            else
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
                                break;
                            }
                            break;
                    }
                    return;
                }
                else
                {
                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help"));
                }
            }
        }
    }
}
