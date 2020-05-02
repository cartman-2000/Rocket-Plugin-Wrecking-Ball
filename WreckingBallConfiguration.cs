using Rocket.API;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ApokPT.RocketPlugins
{
    public class WreckingBallConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public bool Debug = false;
        public float DestructionRate = 10;
        public uint DestructionsPerInterval = 10;
        public bool LogScans = false;
        public bool PrintToChat = false;
        public bool EnablePlayerInfo = false;
        public bool EnableCleanup = false;
        public bool BuildableCleanup = true;
        public float BuildableWaitTime = 30;
        public bool CleanupLockedCars = true;
        public bool PlayerDataCleanup = true;
        public float PlayerDataWaitTime = 45;
        public float CleanupIntervalTime = 5;
        public byte CleanupPerInterval = 10;
        public bool EnableVehicleCap = false;
        public bool VCapDestroyByElementCount = false;
        public ushort MaxVehiclesAllowed = 70;
        public uint VCapCheckInterval = 600;
        public bool LowElementCountOnly = false;
        public ushort MinElementCount = 6;
        public bool KeepVehiclesWithSigns = true;
        public bool EnableVehicleElementDrop = true;
        public bool EnableDestroyedElementDrop = true;

        [XmlElement("VehicleSignFlag")]
        public string XmlVehicleSignFlag = "n";
        [XmlIgnore]
        public char VehicleSignFlag
        {
            get
            {
                if (!char.TryParse(XmlVehicleSignFlag, out char id))
                {
                    XmlVehicleSignFlag = "n";
                    WreckingBall.Instance.Configuration.Save();
                }
                return id;
            }
            set
            {
                XmlVehicleSignFlag = value.ToString();
            }
        }

        public bool LimitSafeGuards = false;
        public float LimitSafeGuardsRatio = .6f;
        public int PlayerElementListCutoff = 100;
        public uint CategoryListVersion = 0;
        public uint ElementListVersion = 0;

        [XmlArray("Categories"), XmlArrayItem(ElementName = "Category")]
        public List<Category> Categories = new List<Category>();

        [XmlArray("Elements"), XmlArrayItem(ElementName = "Element")]
        public List<Element> Elements = new List<Element>();

        public void LoadDefaults()
        {
            string logFormatStart = "Updating {0} list to version {1}";
            string logFormatEnd = "Finished updating {0}";
            string categories = "Categories";
            string elements = "Elements";
            // set categories list version, don't run through the primary update if there are records currently in the list.
            if (Categories.Count == 0)
                CategoryListVersion = 0;
            else if (Categories.Count != 0 && CategoryListVersion == 0)
                CategoryListVersion = 1;
            // set elements list version, don't run through the primary update if there are records currently in the list.
            if (Elements.Count == 0)
                ElementListVersion = 0;
            else if (Elements.Count != 0 && ElementListVersion == 0)
                ElementListVersion = 1;

            // Initial Category list setup.
            if(CategoryListVersion == 0)
            {
                CategoryListVersion = 1;
                Logger.Log(string.Format(logFormatStart, categories, CategoryListVersion));
                AddCategory('b', "Bed", ConsoleColor.DarkCyan);
                AddCategory('t', "Trap", ConsoleColor.DarkYellow);
                AddCategory('d', "Door", ConsoleColor.DarkMagenta);
                AddCategory('c', "Container", ConsoleColor.Blue);
                AddCategory('y', "Trophy Container", ConsoleColor.Blue);
                AddCategory('l', "Ladder", ConsoleColor.Magenta);
                AddCategory('w', "Wall", ConsoleColor.DarkMagenta);
                AddCategory('f', "Floor", ConsoleColor.DarkMagenta);
                AddCategory('p', "Pillar", ConsoleColor.DarkMagenta);
                AddCategory('r', "Roof", ConsoleColor.DarkMagenta);
                AddCategory('s', "Stair", ConsoleColor.DarkMagenta);
                AddCategory('m', "Freeform", ConsoleColor.DarkMagenta);
                AddCategory('n', "Sign", ConsoleColor.DarkBlue);
                AddCategory('g', "Guard", ConsoleColor.DarkBlue);
                AddCategory('o', "Protections", ConsoleColor.DarkBlue);
                AddCategory('i', "Illumination", ConsoleColor.Yellow);
                AddCategory('u', "Industrial", ConsoleColor.DarkYellow);
                AddCategory('a', "Agriculture", ConsoleColor.Green);
                AddCategory('D', "Decorations", ConsoleColor.Yellow);
                AddCategory('v', "Vehicles", ConsoleColor.DarkRed);
                AddCategory('z', "Zombies", ConsoleColor.DarkGreen);
                AddCategory('!', "Uncategorized", ConsoleColor.White);
                Logger.Log(string.Format(logFormatEnd, categories));
            }

            // Initial element list setup.
            if (ElementListVersion == 0)
            {
                ElementListVersion = 1;
                Logger.Log(string.Format(logFormatStart, elements, ElementListVersion));
                // Zombie
                AddElement(9998, 'z');

                // Vehicle
                AddElement(9999, 'v');

                // Bed
                AddElement(288, 'b');
                AddElement(289, 'b');
                AddElement(290, 'b');
                AddElement(291, 'b');
                AddElement(292, 'b');
                AddElement(293, 'b');
                AddElement(294, 'b');
                AddElement(295, 'b');
                AddElement(1243, 'b');
                AddElement(1309, 'b');
                AddElement(1310, 'b');
                AddElement(1311, 'b');
                AddElement(1312, 'b');
                AddElement(1313, 'b');
                AddElement(1314, 'b');
                // Trap
                AddElement(382, 't');
                AddElement(383, 't');
                AddElement(384, 't');
                AddElement(385, 't');
                AddElement(386, 't');
                AddElement(1101, 't');
                AddElement(1102, 't');
                AddElement(1113, 't');
                AddElement(1119, 't');
                AddElement(1130, 't');
                AddElement(1131, 't');
                AddElement(1227, 't');
                AddElement(1241, 't');
                AddElement(1244, 't');
                AddElement(1372, 't');
                AddElement(1373, 't');
                AddElement(1393, 't');
                // Door - door
                AddElement(281, 'd');
                AddElement(282, 'd');
                AddElement(283, 'd');
                AddElement(378, 'd');
                // Door - jail and vault
                AddElement(284, 'd');
                AddElement(286, 'd');
                // Door - gate
                AddElement(451, 'd');
                AddElement(455, 'd');
                AddElement(456, 'd');
                AddElement(457, 'd');
                AddElement(1235, 'd');
                AddElement(1236, 'd');
                AddElement(1237, 'd');
                AddElement(1238, 'd');
                // Door - Hatch
                AddElement(1329, 'd');
                AddElement(1330, 'd');
                AddElement(1331, 'd');
                AddElement(1332, 'd');
                // Storage
                AddElement(328, 'c');
                AddElement(366, 'c');
                AddElement(367, 'c');
                AddElement(368, 'c');
                AddElement(1374, 'c');
                // Trophy/Weapon rack Containers.
                AddElement(1202, 'y');
                AddElement(1203, 'y');
                AddElement(1204, 'y');
                AddElement(1205, 'y');
                AddElement(1206, 'y');
                AddElement(1207, 'y');
                AddElement(1220, 'y');
                AddElement(1221, 'y');
                AddElement(1408, 'y');
                AddElement(1409, 'y');
                AddElement(1410, 'y');
                AddElement(1411, 'y');
                AddElement(1412, 'y');
                AddElement(1413, 'y');
                // Ladder
                AddElement(325, 'l');
                AddElement(326, 'l');
                AddElement(327, 'l');
                AddElement(379, 'l');
                // Wall - wall
                AddElement(33, 'w');
                AddElement(57, 'w');
                AddElement(58, 'w');
                AddElement(371, 'w');
                AddElement(1215, 'w');
                AddElement(1414, 'w');
                AddElement(1415, 'w');
                AddElement(1416, 'w');
                AddElement(1417, 'w');
                AddElement(1418, 'w');
                // Wall - doorway
                AddElement(32, 'w');
                AddElement(49, 'w');
                AddElement(50, 'w');
                AddElement(370, 'w');
                AddElement(1210, 'w');
                // Wall - window
                AddElement(34, 'w');
                AddElement(59, 'w');
                AddElement(60, 'w');
                AddElement(372, 'w');
                AddElement(1216, 'w');
                // Wall - garage
                AddElement(450, 'w');
                AddElement(452, 'w');
                AddElement(453, 'w');
                AddElement(454, 'w');
                AddElement(1211, 'w');
                // Wall - rampart
                AddElement(442, 'w');
                AddElement(444, 'w');
                AddElement(445, 'w');
                AddElement(446, 'w');
                AddElement(1214, 'w');
                // Floor
                AddElement(31, 'f');
                AddElement(51, 'f');
                AddElement(52, 'f');
                AddElement(369, 'f');
                AddElement(1262, 'f');
                AddElement(1263, 'f');
                AddElement(1264, 'f');
                AddElement(1265, 'f');
                // Pillar - pillar
                AddElement(36, 'p');
                AddElement(53, 'p');
                AddElement(54, 'p');
                AddElement(374, 'p');
                AddElement(1212, 'p');
                // Pillar - post
                AddElement(443, 'p');
                AddElement(447, 'p');
                AddElement(448, 'p');
                AddElement(449, 'p');
                AddElement(1213, 'p');
                // Roof - roof
                AddElement(35, 'r');
                AddElement(55, 'r');
                AddElement(56, 'r');
                AddElement(373, 'r');
                AddElement(1266, 'r');
                AddElement(1267, 'r');
                AddElement(1268, 'r');
                AddElement(1269, 'r');
                // Roof - hole
                AddElement(319, 'r');
                AddElement(321, 'r');
                AddElement(320, 'r');
                AddElement(376, 'r');
                // Stairs - stairs
                AddElement(316, 's');
                AddElement(318, 's');
                AddElement(317, 's');
                AddElement(375, 's');
                // Stairs - ramps
                AddElement(322, 's');
                AddElement(323, 's');
                AddElement(324, 's');
                AddElement(377, 's');
                // Free Form Buildables
                AddElement(1058, 'm');
                AddElement(1059, 'm');
                AddElement(1060, 'm');
                AddElement(1061, 'm');
                AddElement(1062, 'm');
                AddElement(1063, 'm');
                AddElement(1064, 'm');
                AddElement(1065, 'm');
                AddElement(1066, 'm');
                AddElement(1067, 'm');
                AddElement(1068, 'm');
                AddElement(1069, 'm');
                AddElement(1070, 'm');
                AddElement(1071, 'm');
                AddElement(1072, 'm');
                AddElement(1073, 'm');
                AddElement(1074, 'm');
                AddElement(1075, 'm');
                AddElement(1083, 'm');
                AddElement(1084, 'm');
                AddElement(1085, 'm');
                AddElement(1086, 'm');
                AddElement(1087, 'm');
                AddElement(1088, 'm');
                AddElement(1089, 'm');
                AddElement(1090, 'm');
                AddElement(1091, 'm');
                AddElement(1092, 'm');
                AddElement(1093, 'm');
                AddElement(1094, 'm');
                AddElement(1144, 'm');
                AddElement(1145, 'm');
                AddElement(1146, 'm');
                AddElement(1147, 'm');
                AddElement(1148, 'm');
                AddElement(1149, 'm');
                AddElement(1150, 'm');
                AddElement(1151, 'm');
                AddElement(1152, 'm');
                AddElement(1153, 'm');
                AddElement(1154, 'm');
                AddElement(1155, 'm');
                AddElement(1217, 'm');
                AddElement(1218, 'm');
                AddElement(1239, 'm');
                AddElement(1396, 'm');
                AddElement(1397, 'm');
                // Signs
                AddElement(1095, 'n');
                AddElement(1096, 'n');
                AddElement(1097, 'n');
                AddElement(1098, 'n');
                AddElement(1231, 'n');
                AddElement(1232, 'n');
                AddElement(1233, 'n');
                AddElement(1234, 'n');
                // Guard
                AddElement(29, 'g');
                AddElement(30, 'g');
                AddElement(45, 'g');
                AddElement(46, 'g');
                AddElement(47, 'g');
                AddElement(48, 'g');
                AddElement(287, 'g');
                AddElement(365, 'g');
                AddElement(1223, 'g');
                AddElement(1224, 'g');
                AddElement(1225, 'g');
                AddElement(1226, 'g');
                AddElement(1297, 'g');
                AddElement(1298, 'g');
                AddElement(1299, 'g');
                // Protections
                AddElement(1050, 'o');
                AddElement(1158, 'o');
                AddElement(1261, 'o');
                // Light
                AddElement(359, 'i');
                AddElement(360, 'i');
                AddElement(361, 'i');
                AddElement(362, 'i');
                AddElement(459, 'i');
                AddElement(1049, 'i');
                AddElement(1222, 'i');
                AddElement(1255, 'i');
                AddElement(1272, 'i');
                AddElement(1273, 'i');
                AddElement(1274, 'i');
                AddElement(1275, 'i');
                AddElement(1276, 'i');
                AddElement(1277, 'i');
                // Industrial
                AddElement(458, 'u');
                AddElement(1219, 'u');
                AddElement(1208, 'u');
                AddElement(1228, 'u');
                AddElement(1229, 'u');
                AddElement(1230, 'u');
                // Agriculture
                AddElement(330, 'a');
                AddElement(331, 'a');
                AddElement(336, 'a');
                AddElement(339, 'a');
                AddElement(341, 'a');
                AddElement(343, 'a');
                AddElement(345, 'a');
                AddElement(1045, 'a');
                AddElement(1104, 'a');
                AddElement(1105, 'a');
                AddElement(1106, 'a');
                AddElement(1107, 'a');
                AddElement(1108, 'a');
                AddElement(1109, 'a');
                AddElement(1110, 'a');
                AddElement(1345, 'a');
                // Decorations
                AddElement(1245, 'D');
                AddElement(1246, 'D');
                AddElement(1247, 'D');
                AddElement(1248, 'D');
                AddElement(1249, 'D');
                AddElement(1250, 'D');
                AddElement(1251, 'D');
                AddElement(1252, 'D');
                AddElement(1253, 'D');
                AddElement(1254, 'D');
                AddElement(1256, 'D');
                AddElement(1257, 'D');
                AddElement(1258, 'D');
                AddElement(1259, 'D');
                AddElement(1260, 'D');
                AddElement(1278, 'D');
                AddElement(1279, 'D');
                AddElement(1280, 'D');
                AddElement(1281, 'D');
                AddElement(1282, 'D');
                AddElement(1283, 'D');
                AddElement(1284, 'D');
                AddElement(1285, 'D');
                AddElement(1286, 'D');
                AddElement(1287, 'D');
                AddElement(1288, 'D');
                AddElement(1289, 'D');
                AddElement(1290, 'D');
                AddElement(1291, 'D');
                AddElement(1292, 'D');
                AddElement(1293, 'D');
                AddElement(1294, 'D');
                AddElement(1295, 'D');
                AddElement(1296, 'D');
                AddElement(1303, 'D');
                AddElement(1304, 'D');
                AddElement(1305, 'D');
                AddElement(1306, 'D');
                AddElement(1307, 'D');
                AddElement(1308, 'D');
                AddElement(1315, 'D');
                AddElement(1316, 'D');
                AddElement(1317, 'D');
                AddElement(1318, 'D');
                AddElement(1319, 'D');
                AddElement(1320, 'D');
                AddElement(1321, 'D');
                AddElement(1322, 'D');
                AddElement(1323, 'D');
                AddElement(1324, 'D');
                AddElement(1325, 'D');
                AddElement(1326, 'D');
                AddElement(1327, 'D');
                AddElement(1328, 'D');
                AddElement(1466, 'D');
                Logger.Log(string.Format(logFormatEnd, elements));
            }

            // Transfer sentries to the protections category.
            if (ElementListVersion == 1)
            {
                ElementListVersion = 2;
                Logger.Log(string.Format(logFormatStart, elements, ElementListVersion));
                AddElement(1244, 'o', true);
                AddElement(1372, 'o', true);
                AddElement(1373, 'o', true);
                Logger.Log(string.Format(logFormatEnd, elements));
            }
            // Add new elements to the list.
            if (ElementListVersion == 2)
            {
                ElementListVersion = 3;
                Logger.Log(string.Format(logFormatStart, elements, ElementListVersion));
                AddElement(1500, 'm');
                Logger.Log(string.Format(logFormatEnd, elements));
            }
            // Add the clock to the list with the Decorations flag.
            if (ElementListVersion == 3)
            {
                ElementListVersion = 4;
                Logger.Log(string.Format(logFormatStart, elements, ElementListVersion));
                AddElement(1509, 'D');
                Logger.Log(string.Format(logFormatEnd, elements));
            }
            // Update for new features, Change case for the special categories for the vehicles/zombies to upper case, and add a Animals category.
            if (CategoryListVersion == 1)
            {
                CategoryListVersion = 2;
                Logger.Log(string.Format(logFormatStart, categories, CategoryListVersion));
                AddCategory('A', "Animals", ConsoleColor.Cyan);
                AddCategory('V', "Vehicles", ConsoleColor.DarkRed, true, 'v');
                AddCategory('Z', "Zombies", ConsoleColor.DarkGreen, true, 'z');
                Logger.Log(string.Format(logFormatEnd, categories));
            }
            if (ElementListVersion == 4)
            {
                ElementListVersion = 5;
                Logger.Log(string.Format(logFormatStart, elements, ElementListVersion));
                AddElement(999, 'V', true, 9999);
                AddElement(998, 'Z', true, 9998);
                AddElement(997, 'A');
                Logger.Log(string.Format(logFormatEnd, elements));
            }
        }

        public void AddCategory(char catflag, string name, ConsoleColor color, bool update = false, char? oldID = null)
        {
            Category trimmedcat = Categories.FirstOrDefault(catf => oldID != null ? catf.Id == oldID : catf.Id == catflag);
            if (trimmedcat != null)
            {
                if (!update)
                    Logger.LogWarning(string.Format("Can't add Category flag {0} to category list, it's already taken by a category named: {1}, with flag: {2}.", catflag, trimmedcat.Name, trimmedcat.Id));
                else
                {
                    if (oldID == null)
                        Categories[Categories.FindIndex(catf => catf.Id == catflag)] = new Category(catflag, name, color);
                    else
                    {
                        Category trimmedcat2 = Categories.FirstOrDefault(catf => catf.Id == catflag);
                        if (trimmedcat2 != null)
                            Logger.LogWarning(string.Format("Can't update Category flag {0} to {1} on category list, it's already taken by a category named: {2}, with flag: {3}.", oldID, catflag, trimmedcat2.Name, trimmedcat2.Id));
                        else
                            Categories[Categories.FindIndex(catf => catf.Id == oldID)] = new Category(catflag, name, color);
                    }
                }
            }
            else
                Categories.Add(new Category(catflag, name, color));
        }

        public void AddElement(ushort id, char catflag, bool update = false, ushort? oldID = null)
        {
            Element trimmedelement = Elements.FirstOrDefault(elid => oldID != null ? elid.Id == oldID : elid.Id == id);
            Category trimmedcat = Categories.FirstOrDefault(catf => catf.Id == catflag);
            if (trimmedelement != null)
            {
                if (!update)
                    Logger.LogWarning(string.Format("Can't add Element ID {0} to category list, it's already set, with a category flag id of: {1}.", id, trimmedelement.CategoryId));
                else
                {
                    if (trimmedcat != null)
                    {
                        if (oldID == null)
                            Elements[Elements.FindIndex(elid => elid.Id == id)] = new Element(id, catflag);
                        else
                        {
                            Element trimmedElement2 = Elements.FirstOrDefault(elid => elid.Id == id);
                            if (trimmedElement2 != null)
                                Logger.LogWarning(string.Format("Can't update Element ID {0} to {1}, an element with this id is already set, with a category flag id of: {2}.", oldID, id, trimmedElement2.CategoryId));
                            else
                                Elements[Elements.FindIndex(elid => elid.Id == oldID)] = new Element(id, catflag);    
                            }
                    }
                    else
                        Logger.LogWarning(string.Format("Can't update Element ID: {0}, to another category, the category for flag: {1} doesn't exist.", id, catflag));
                }
            }
            else
                Elements.Add(new Element(id, catflag));
        }

    }
}
