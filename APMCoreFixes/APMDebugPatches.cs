using AMDaemon;
using AMDaemon.Abaas;
using Apm.System.AbaasGs;
using Apm.System.Daemon;
using Apm.System.Setting.NonVolatile;
using Apm.System.Setting.Volatile;
using Apm.System.Util;
using Apm.System.Util.Image;
using Apm.System.Util.Instruction;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace APMCoreFixes {
    internal class APMDebugPatches {

        // WTF SEGA
        [HarmonyPrefix, HarmonyPatch(typeof(Analyzer), "ValidCombination")]
        public static bool ValidCombination(ref string[] __result, string[] names) {
            if (APMCF.ConfigDisableNameChecks.Value) {
                __result = names;
                return false;
            } else {
                return true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Analyzer), "IsValidName")]
        public static void IsValidName(ref bool __result, string name) {
            if (APMCF.ConfigDisableNameChecks.Value) {
                __result = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Analyzer), "GetInfo")]
        public static void GetInfo(ref Analyzer.ImageInfo __result, string path) {
            if (!APMCF.ConfigDisableOPTPresenceCheck.Value) {
                return;
            }
            Analyzer.ImageInfo imageInfo = new Analyzer.ImageInfo();
            string fileName = Path.GetFileName(path);
            imageInfo.Type = Analyzer.GetType(fileName);
            foreach (string text in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)) {
                string fileName2 = Path.GetFileName(text);
                string text2 = ((fileName2 != null) ? fileName2.ToLower() : null);
                if (text2 == "icon.png") {
                    imageInfo.HasIcon = true;
                } else if (text2 == "movie.wmv") {
                    imageInfo.HasMovie = true;
                } else if (text2 == "app.json") {
                    imageInfo.HasConfig = true;
                }
            }
            imageInfo.ImagePath = Path.Combine(path, "dummy.opt");
            if (imageInfo.HasConfig) {
                ConfigParser configParser = new ConfigParser(Path.Combine(path, "app.json"));
                if (!configParser.Valid) {
                    __result = null;
                    return;
                }
                imageInfo.TitleName = configParser.TitleName;
                imageInfo.SubGameId = configParser.SubGameId;
                imageInfo.Aime = configParser.Aime;
                imageInfo.EMoney = configParser.EMoneyInfo;
                imageInfo.Ui = configParser.UiInfo;
                imageInfo.GamePad = configParser.GamePadInfo;
            }
            imageInfo.Name = fileName;
            if (!(imageInfo.ImagePath == "") && (imageInfo.Type != Analyzer.ImageType.Original || (imageInfo.HasMovie && imageInfo.HasIcon && imageInfo.HasConfig))) {
                __result = imageInfo;
                return;
            }
            __result = null;
            return;
        }

        /*[HarmonyPostfix]
        [HarmonyPatch(typeof(Exception), MethodType.Constructor, typeof(string))]
        static void exceptionReporter(Exception __instance) {
            exceptionReporterL(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Exception), MethodType.Constructor, typeof(string), typeof(Exception))]
        static void exceptionReporter2(Exception __instance) {
            exceptionReporterL(__instance);
        }

        private static void exceptionReporterL(Exception ex) {
            String es = ex.ToString();
            if (es.Contains("AESUtil.Header.Read") || es.Contains("PC_CRC_Check") || es.Contains("Sharing violation") || es.Contains("Specified method is not supported")) {
                return;
            }
            APMCoreFixesBehaviour.Log.LogDebug("Exception created: " + ex);
            APMCoreFixesBehaviour.Log.LogDebug(new StackTrace());
        }*/

        [HarmonyPostfix, HarmonyPatch(typeof(Apm.System.Daemon.Core), "SetError")]
        static void SetError(int number) {
            APMCF.Log.LogError("SetError: " + number + ": " + new StackTrace());
        }

        /*[HarmonyPostfix, HarmonyPatch(typeof(FileStream), MethodType.Constructor, typeof(string), typeof(FileMode), typeof(FileAccess))]
        static void FileStream(string path, FileMode mode, FileAccess access) {
            APMCF.Log.LogDebug("FileStream " + mode + ": " + path);
        }*/

        [HarmonyPostfix, HarmonyPatch(typeof(Path), "GetTempPath")]
        static void GetTempPath(ref string __result) {
            __result = GeneralSettingManager.manager.Info.setupSetting.imageRootPath;
            APMCF.Log.LogDebug("Temp path changed to: " + __result);
        }
    }
}