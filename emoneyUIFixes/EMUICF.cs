using Apm.Emoney.Ui;
using Apm.Emoney.Ui.GamePad;
using BepInEx;
using BepInEx.Logging;
using Emoney.SharedMemory;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Emoney.SharedMemory.ShareMemoryAccessor;
using SceneManager = Apm.Emoney.Ui.SceneManager;

namespace emoneyUIFixes {
    [BepInPlugin("eu.haruka.gmg.apm.eui_fixes", "EMoneyUICoreFixes", "0.1")]
    [BepInProcess("emoneyUI")]
    public class EMUICF : BaseUnityPlugin {

        public static ManualLogSource Log;

        public void Awake() {
            Log = Logger;

            Harmony.CreateAndPatchAll(typeof(Patches), "eu.haruka.gmg.apm.fixes.emoneyui.main");

            Log.LogInfo("Loaded");

        }

        public void Update() {
        }
    }

    public class Patches {

        [HarmonyPostfix, HarmonyPatch(typeof(Path), "GetTempPath")]
        static void GetTempPath(ref string __result) {
            __result = "apm\\appdata\\";
            EMUICF.Log.LogDebug("Temp path changed to: " + __result);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ShareMemoryAccessor), "Open")]
        static void Open(ref ShareMemoryAccessor.Result __result) {
            EMUICF.Log.LogDebug("Shared memory access: " + __result);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ShareMemoryAccessor), "Create")]
        static void Create(UiSharedData data, ref ShareMemoryAccessor.Result __result) {
            EMUICF.Log.LogDebug("Shared memory access: " + __result);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ApmInputApi), "IsEqual")]
        static bool IsEqual(ref ApmInputApi.ApmGamepadConfig config1, ref ApmInputApi.ApmGamepadConfig config2, ref bool __result) {
            if (config1.Sw == null) {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneManager), "Start")]
        static bool Start(SceneManager __instance) {
            /*EMUICF.Log.LogDebug("Menu rect: X=" + __instance.entryMenuRect.x + ",Y=" + __instance.entryMenuRect.y);*/
            EMUICF.Log.LogDebug("Menu size: X=" + __instance.entryMenuSize.x + ",Y=" + __instance.entryMenuSize.y);
            EMUICF.Log.LogDebug("Icon alignment: " + __instance.iconAxis);
            if (__instance.entryMenuSize.x >= 230 && __instance.entryMenuSize.y >= 150) {
                if (__instance.iconAxis == UnityEngine.UI.GridLayoutGroup.Axis.Vertical) {
                    //__instance.entryMenuRect.x /= 2;
                    __instance.entryMenuSize.y /= 2;
                    EMUICF.Log.LogInfo("Shrinking X axis");
                } else if (__instance.iconAxis == UnityEngine.UI.GridLayoutGroup.Axis.Horizontal) {
                    //__instance.entryMenuRect.y /= 2;
                    __instance.entryMenuSize.x /= 2;
                    EMUICF.Log.LogInfo("Shrinking Y axis");
                }
            }
            return true;
        }

    }
}
