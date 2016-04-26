using Rocket.API.Extensions;
using SDG.Unturned;
using Steamworks;
using System;
using UnityEngine;

namespace ApokPT.RocketPlugins
{
    public static class Extensions
    {
        public static bool RegionOutOfRange(this Vector3 point, int x, int y, uint radius)
        {
            Vector3 regionPoint;
            Regions.tryGetPoint((byte)x, (byte)y, out regionPoint);
            // region center point.
            regionPoint += new Vector3(64, 0, 64);
            if (Vector3.Distance(regionPoint, new Vector3(point.x, 0f, point.z)) > radius + 92)
                return true;
            return false;
        }

        public static bool GetVectorFromCmd(this string[] cmd, int idxStart, out Vector3 position)
        {
            position = Vector3.zero;
            float? x = cmd.GetFloatParameter(idxStart);
            float? y = cmd.GetFloatParameter(idxStart + 1);
            float? z = cmd.GetFloatParameter(idxStart + 2);
            if (x.HasValue && y.HasValue && z.HasValue)
            {
                position = new Vector3((float)x, (float)y, (float)z);
                return true;
            }
            return false;
        }

        // Returns a ulong from a string on out, and returns true if it is a valid SteamID.
        public static bool isCSteamID(this string sCSteamID, out ulong ulCSteamID)
        {
            ulCSteamID = 0;
            if (ulong.TryParse(sCSteamID, out ulCSteamID))
            {
                if ((ulCSteamID >= 0x0110000100000000 && ulCSteamID <= 0x0170000000000000) || ulCSteamID == 0)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
