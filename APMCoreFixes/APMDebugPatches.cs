using System.Diagnostics;
using System.IO;
using Apm.System.Daemon;
using Apm.System.Util.Image;
using HarmonyLib;

namespace APMCoreFixes {
    internal class APMDebugPatches {
        // WTF SEGA
        [HarmonyPrefix, HarmonyPatch(typeof(Analyzer), "ValidCombination")]
        public static bool ValidCombination(ref string[] __result, string[] names) {
            if (ApmCoreFixes.ConfigDisableNameChecks.Value) {
                __result = names;
                return false;
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Analyzer), "IsValidName")]
        public static void IsValidName(ref bool __result, string name) {
            if (ApmCoreFixes.ConfigDisableNameChecks.Value) {
                __result = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Analyzer), "GetInfo")]
        public static void GetInfo(ref Analyzer.ImageInfo __result, string path) {
            if (!ApmCoreFixes.ConfigDisableOPTPresenceCheck.Value) {
                return;
            }

            Analyzer.ImageInfo imageInfo = new Analyzer.ImageInfo();
            string fileName = Path.GetFileName(path);
            imageInfo.Type = Analyzer.GetType(fileName);
            foreach (string text in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)) {
                string fileName2 = Path.GetFileName(text);
                string text2 = fileName2?.ToLower();
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
            if (imageInfo.ImagePath != "" && (imageInfo.Type != Analyzer.ImageType.Original || (imageInfo.HasMovie && imageInfo.HasIcon && imageInfo.HasConfig))) {
                __result = imageInfo;
                return;
            }

            __result = null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Core), "SetError")]
        static void SetError(int number) {
            ApmCoreFixes.Log.LogError("SetError: " + number + ": " + new StackTrace());
        }
    }
}