using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;

namespace ApokPT.RocketPlugins
{
    public class WreckingBallCommand
    {
        public static void Execute(IRocketPlayer caller, string[] cmd)
        {

            string command = String.Join(" ", cmd);

            if (!caller.IsAdmin && !WreckingBall.Instance.Configuration.Instance.Enabled) return;

            if (!caller.IsAdmin && !caller.HasPermission("wreck")) return;

            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (String.IsNullOrEmpty(command.Trim()))
            {
                if (WreckingBall.processing)
                    WreckingBall.Instance.Wreck(player, "", 0);
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
                            WreckingBall.Instance.Confirm(player);
                            break;
                        case "abort":
                            UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_aborted"));
                            WreckingBall.Instance.Abort();
                            break;
                        case "scan":
                            if (oper.Length == 3)
                            {
                                WreckingBall.Instance.Scan(player, oper[1], Convert.ToUInt32(oper[2]));
                            }
                            else
                            {
                                UnturnedChat.Say(caller, WreckingBall.Instance.Translate("wreckingball_help_scan"));
                            }
                            break;
                        case "teleport":

                            if (oper.Length > 1)
                            {
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
                            try { WreckingBall.Instance.Wreck(player, oper[0], Convert.ToUInt32(oper[1])); }
                            catch { WreckingBall.Instance.Wreck(player, oper[0], 20); }
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
