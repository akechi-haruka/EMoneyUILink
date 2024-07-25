using Apm.System.Util;
using HarmonyLib;
using System.Threading.Tasks;

namespace APMCoreFixes {
    internal class AppMounterPatches {

        [HarmonyPrefix, HarmonyPatch(typeof(AppMounter), "Mount")]
        static bool Mount(string subGameId, AppMounter.IsEnd mountEnd) {
            if (APMCF.ConfigSkipVHDMount.Value) {
                APMCF.Log.LogInfo("Mount: " + subGameId);
                mountEnd(true);
                return false;
            } else {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AppMounter), "Unmount")]
        static bool Unmount(AppMounter.IsEnd mountEnd) {
            if (APMCF.ConfigSkipVHDMount.Value) {
                APMCF.Log.LogInfo("Unmount");
                mountEnd(true);
                return false;
            } else {
                return true;
            }
        }

    }
}