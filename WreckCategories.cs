using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApokPT.RocketPlugins
{

    class WreckCategories
    {
        private static WreckCategories i;

        internal Dictionary<ushort, char> items = new Dictionary<ushort, char>();
        internal Dictionary<char, Category> category = new Dictionary<char, Category>();

        public static WreckCategories Instance
        {
            get
            {
                if (i == null)
                {
                    i = new WreckCategories();
                    i.category.Add('b', new Category("Bed", ConsoleColor.DarkCyan));
                    i.category.Add('t', new Category("Trap", ConsoleColor.DarkYellow));
                    i.category.Add('d', new Category("Door", ConsoleColor.DarkMagenta));
                    i.category.Add('c', new Category("Container", ConsoleColor.Blue));
                    i.category.Add('l', new Category("Ladder", ConsoleColor.Magenta));
                    i.category.Add('w', new Category("Wall", ConsoleColor.DarkMagenta));
                    i.category.Add('f', new Category("Floor", ConsoleColor.DarkMagenta));
                    i.category.Add('p', new Category("Pillar", ConsoleColor.DarkMagenta));
                    i.category.Add('r', new Category("Roof", ConsoleColor.DarkMagenta));
                    i.category.Add('s', new Category("Stair", ConsoleColor.DarkMagenta));
                    i.category.Add('m', new Category("Freeform", ConsoleColor.DarkMagenta));
                    i.category.Add('n', new Category("Sign", ConsoleColor.DarkBlue));
                    i.category.Add('g', new Category("Guard", ConsoleColor.DarkBlue));
                    i.category.Add('i', new Category("Illumination", ConsoleColor.Yellow));
                    i.category.Add('a', new Category("Agriculture", ConsoleColor.Green));
                    i.category.Add('v', new Category("Vehicle", ConsoleColor.DarkRed));
                    i.category.Add('!', new Category("Uncategorized", ConsoleColor.White));
                    //i.category.Add('z', new Category("Zombie", ConsoleColor.DarkGreen));

                    // Zombie
                    //i.items.Add(9998, 'z');

                    // Vehicle
                    i.items.Add(9999, 'v');
                    
                    // Bed
                    i.items.Add(288, 'b');
                    i.items.Add(289, 'b');
                    i.items.Add(290, 'b');
                    i.items.Add(291, 'b');
                    i.items.Add(292, 'b');
                    i.items.Add(293, 'b');
                    i.items.Add(294, 'b');
                    i.items.Add(295, 'b');
                    // Trap
                    i.items.Add(382, 't');
                    i.items.Add(383, 't');
                    i.items.Add(384, 't');
                    i.items.Add(385, 't');
                    i.items.Add(386, 't');
                    i.items.Add(1101, 't');
                    i.items.Add(1102, 't');
                    i.items.Add(1113, 't');
                    i.items.Add(1119, 't');
                    i.items.Add(1130, 't');
                    i.items.Add(1131, 't');
                    // Door - door
                    i.items.Add(281, 'd');
                    i.items.Add(282, 'd');
                    i.items.Add(283, 'd');
                    i.items.Add(378, 'd');
                    // Door - jail and vault
                    i.items.Add(284, 'd');
                    i.items.Add(286, 'd');
                    // Door - gate
                    i.items.Add(451, 'd');
                    i.items.Add(455, 'd');
                    i.items.Add(456, 'd');
                    i.items.Add(457, 'd');
                    // Storage
                    i.items.Add(328, 'c');
                    i.items.Add(366, 'c');
                    i.items.Add(367, 'c');
                    i.items.Add(368, 'c');
                    // Ladder
                    i.items.Add(325, 'l');
                    i.items.Add(326, 'l');
                    i.items.Add(327, 'l');
                    i.items.Add(379, 'l');
                    // Wall - wall
                    i.items.Add(33, 'w');
                    i.items.Add(57, 'w');
                    i.items.Add(58, 'w');
                    i.items.Add(371, 'w');
                    // Wall - doorway
                    i.items.Add(32, 'w');
                    i.items.Add(49, 'w');
                    i.items.Add(50, 'w');
                    i.items.Add(370, 'w');
                    // Wall - window
                    i.items.Add(34, 'w');
                    i.items.Add(59, 'w');
                    i.items.Add(60, 'w');
                    i.items.Add(372, 'w');
                    // Wall - garage
                    i.items.Add(450, 'w');
                    i.items.Add(452, 'w');
                    i.items.Add(453, 'w');
                    i.items.Add(454, 'w');
                    // Wall - rampart
                    i.items.Add(442, 'w');
                    i.items.Add(444, 'w');
                    i.items.Add(445, 'w');
                    i.items.Add(446, 'w');
                    // Floor
                    i.items.Add(31, 'f');
                    i.items.Add(51, 'f');
                    i.items.Add(52, 'f');
                    i.items.Add(369, 'f');
                    // Pillar - pillar
                    i.items.Add(36, 'p');
                    i.items.Add(53, 'p');
                    i.items.Add(54, 'p');
                    i.items.Add(374, 'p');
                    // Pillar - post
                    i.items.Add(443, 'p');
                    i.items.Add(447, 'p');
                    i.items.Add(448, 'p');
                    i.items.Add(449, 'p');
                    // Roof - roof
                    i.items.Add(35, 'r');
                    i.items.Add(55, 'r');
                    i.items.Add(56, 'r');
                    i.items.Add(373, 'r');
                    // Roof - hole
                    i.items.Add(319, 'r');
                    i.items.Add(321, 'r');
                    i.items.Add(320, 'r');
                    i.items.Add(376, 'r');
                    // Stairs - stairs
                    i.items.Add(316, 's');
                    i.items.Add(318, 's');
                    i.items.Add(317, 's');
                    i.items.Add(375, 's');
                    // Stairs - ramps
                    i.items.Add(322, 's');
                    i.items.Add(323, 's');
                    i.items.Add(324, 's');
                    i.items.Add(377, 's');
                    // Free Form Buildables
                    i.items.Add(1058, 'm');
                    i.items.Add(1059, 'm');
                    i.items.Add(1060, 'm');
                    i.items.Add(1061, 'm');
                    i.items.Add(1062, 'm');
                    i.items.Add(1063, 'm');
                    i.items.Add(1064, 'm');
                    i.items.Add(1065, 'm');
                    i.items.Add(1066, 'm');
                    i.items.Add(1067, 'm');
                    i.items.Add(1068, 'm');
                    i.items.Add(1069, 'm');
                    i.items.Add(1070, 'm');
                    i.items.Add(1071, 'm');
                    i.items.Add(1072, 'm');
                    i.items.Add(1073, 'm');
                    i.items.Add(1074, 'm');
                    i.items.Add(1075, 'm');
                    i.items.Add(1083, 'm');
                    i.items.Add(1084, 'm');
                    i.items.Add(1085, 'm');
                    i.items.Add(1086, 'm');
                    i.items.Add(1087, 'm');
                    i.items.Add(1088, 'm');
                    i.items.Add(1089, 'm');
                    i.items.Add(1090, 'm');
                    i.items.Add(1091, 'm');
                    i.items.Add(1092, 'm');
                    i.items.Add(1093, 'm');
                    i.items.Add(1094, 'm');
                    i.items.Add(1144, 'm');
                    i.items.Add(1145, 'm');
                    i.items.Add(1146, 'm');
                    i.items.Add(1147, 'm');
                    i.items.Add(1148, 'm');
                    i.items.Add(1149, 'm');
                    i.items.Add(1150, 'm');
                    i.items.Add(1151, 'm');
                    i.items.Add(1152, 'm');
                    i.items.Add(1153, 'm');
                    i.items.Add(1154, 'm');
                    i.items.Add(1155, 'm');
                    // Signs
                    i.items.Add(1095, 'n');
                    i.items.Add(1096, 'n');
                    i.items.Add(1097, 'n');
                    i.items.Add(1098, 'n');
                    // Guard
                    i.items.Add(29, 'g');
                    i.items.Add(30, 'g');
                    i.items.Add(45, 'g');
                    i.items.Add(46, 'g');
                    i.items.Add(47, 'g');
                    i.items.Add(48, 'g');
                    i.items.Add(287, 'g');
                    i.items.Add(365, 'g');
                    i.items.Add(1050, 'g');
                    // Light
                    i.items.Add(359, 'i');
                    i.items.Add(360, 'i');
                    i.items.Add(361, 'i');
                    i.items.Add(362, 'i');
                    i.items.Add(458, 'i');
                    i.items.Add(459, 'i');
                    i.items.Add(1049, 'i');
                    // Agriculture
                    i.items.Add(330, 'a');
                    i.items.Add(331, 'a');
                    i.items.Add(336, 'a');
                    i.items.Add(339, 'a');
                    i.items.Add(341, 'a');
                    i.items.Add(343, 'a');
                    i.items.Add(345, 'a');
                    i.items.Add(1045, 'a');
                    i.items.Add(1104, 'a');
                    i.items.Add(1105, 'a');
                    i.items.Add(1106, 'a');
                    i.items.Add(1107, 'a');
                    i.items.Add(1108, 'a');
                    i.items.Add(1109, 'a');
                    i.items.Add(1110, 'a');
                };
                return i;
            }
        }

        internal bool filterItem(ushort itemId, List<char> userRequest)
        {
            return ((items.ContainsKey(itemId) && userRequest.Contains(items[itemId])) || (!items.ContainsKey(itemId) && userRequest.Contains('!')));
        }


        internal Dictionary<char, uint> reportList = new Dictionary<char, uint>();

        internal void report(UnturnedPlayer caller, ushort itemId, float range, bool printConsole, ulong owner = 0)
        {
            Category cat;
            if (!items.ContainsKey(itemId))
            {
                if (reportList.ContainsKey('!'))
                    reportList['!'] += 1;
                else
                    reportList.Add('!', 1);
                cat = category['!'];
            }
            else
            {
                if (reportList.ContainsKey(items[itemId]))
                    reportList[items[itemId]] += 1;
                else
                    reportList.Add(items[itemId], 1);
                cat = category[items[itemId]];
            }
            if (printConsole || !items.ContainsKey(itemId))
            {
                string msg = string.Empty;
                if (owner == 0)
                    msg = cat.Name + " (Id: " + itemId + ") @ " + Math.Round(range, 2) + "m";
                else
                    msg = cat.Name + " (Id: " + itemId + ") @ " + Math.Round(range, 2) + "m, Owner: " + owner;

                Console.ForegroundColor = cat.Color;
                Console.WriteLine(msg);
                Console.ResetColor();
                WreckingBall.RconPrint(msg);
                if (WreckingBall.Instance.Configuration.Instance.LogScans)
                    Logger.Log(msg, false);
                if (WreckingBall.Instance.Configuration.Instance.PrintToChat)
                    UnturnedChat.Say(caller, msg, Color.yellow);
            }
        }
    }
}
