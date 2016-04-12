using Rocket.API;
using Rocket.API.Extensions;
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
                if (WreckingBall.processing)
                    WreckingBall.Instance.Wreck(caller, "", 0, position);
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
                            WreckingBall.Instance.Abort();
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
                                WreckingBall.Instance.Scan(caller, oper[1], Convert.ToUInt32(oper[2]), position);
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
                                        WreckingBall.Instance.Teleport(player, true);
                                        break;
                                    case "s":
                                        WreckingBall.Instance.Teleport(player, false);
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
                            if (((oper.Length == 2 || oper.Length == 3) && !(caller is ConsolePlayer)) || (oper.Length == 5 && caller is ConsolePlayer))
                            {
                                if (caller is ConsolePlayer)
                                {
                                    if (!cmd.GetVectorFromCmd(2, out position))
                                    {
                                        UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_console"));
                                        break;
                                    }
                                }
                                uint? radius = cmd.GetUInt32Parameter(1);
                                WreckingBall.Instance.Wreck(caller, oper[0], radius.HasValue ? (uint)radius : 20, position);
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
