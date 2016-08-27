using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using UnityEngine;

namespace ApokPT.RocketPlugins
{
    public class WreckingBallCommand
    {
        public static void Execute(IRocketPlayer caller, string[] cmd)
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
                            WreckingBall.Instance.Confirm(caller);
                            break;
                        case "abort":
                            UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_aborted"));
                            DestructionProcessing.Abort(WreckType.Wreck);
                            break;
                        case "scan":
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
                                ushort itemID = 0;
                                if (ushort.TryParse(oper[1], out itemID))
                                    WreckingBall.Instance.Scan(caller, oper[1], Convert.ToUInt32(oper[2]), position, FlagType.ItemID, 0, itemID);
                                else
                                    WreckingBall.Instance.Scan(caller, oper[1], Convert.ToUInt32(oper[2]), position, FlagType.Normal, 0, 0);
                            }
                            else if ((oper.Length == 4 && !(caller is ConsolePlayer)) || (oper.Length == 7 && caller is ConsolePlayer))
                            {
                                ulong steamID = 0;
                                if (caller is ConsolePlayer)
                                {
                                    if (!cmd.GetVectorFromCmd(4, out position))
                                    {
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan_console"));
                                        break;
                                    }
                                }
                                if (oper[1].isCSteamID(out steamID))
                                    WreckingBall.Instance.Scan(caller, oper[2], Convert.ToUInt32(oper[3]), position, FlagType.SteamID, (ulong)steamID, 0);
                                else
                                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan"));
                            }
                            else
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan"));
                            }

                            break;
                        case "teleport":

                            if (oper.Length > 1)
                            {
                                if (caller is ConsolePlayer)
                                {
                                    UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_teleport_not_allowed"));
                                    break;
                                }
                                switch (oper[1])
                                {
                                    case "b":
                                        WreckingBall.Instance.Teleport(player, TeleportType.Barricades);
                                        break;
                                    case "s":
                                        WreckingBall.Instance.Teleport(player, TeleportType.Structures);
                                        break;
                                    case "v":
                                        WreckingBall.Instance.Teleport(player, TeleportType.Vehicles);
                                        break;
                                    default:
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_teleport"));
                                        break;
                                }
                            }
                            else
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_teleport"));
                                break;
                            }
                            break;
                        default:
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
                                ushort itemID = 0;
                                if (ushort.TryParse(oper[0], out itemID))
                                    DestructionProcessing.Wreck(caller, oper[0], Convert.ToUInt32(oper[1]), position, WreckType.Wreck, FlagType.ItemID, 0, itemID);
                                else
                                    DestructionProcessing.Wreck(caller, oper[0], Convert.ToUInt32(oper[1]), position, WreckType.Wreck, FlagType.Normal, 0, 0);
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
                                ulong steamID = 0;
                                if (oper[0].isCSteamID(out steamID))
                                    DestructionProcessing.Wreck(caller, oper[1], Convert.ToUInt32(oper[2]), position, WreckType.Wreck, FlagType.SteamID, steamID, 0);
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
