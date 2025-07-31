using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Apm.System.Setting.NonVolatile;
using HarmonyLib;
using static ABaaSGs;

namespace APMCoreFixes {
    public class ABaaSGsPatches {
        [HarmonyPostfix, HarmonyPatch(typeof(ABaaSGs), "downloadAppData", typeof(byte), typeof(string), typeof(Action<ABAASGS_STATUS, string>))]
        static IEnumerator downloadAppData(IEnumerator result, ABaaSGs __instance, byte requestNo, string requestJson, Action<ABAASGS_STATUS, string> callback) {
            if (!ApmCoreFixes.ConfigAddXFolders.Value) {
                return __instance.sendData(requestNo, requestJson, callback, __instance.downloadAppData);
            }

            StringBuilder folderstring = new StringBuilder();

            string[] array = Directory.EnumerateDirectories(GeneralSettingManager.manager.Info.SetupSetting.ImageRootPath, "*", SearchOption.TopDirectoryOnly).ToArray();
            for (int i = 0; i < array.Length; i++) {
                string fn = Path.GetFileName(array[i]);
                ApmCoreFixes.Log.LogInfo(fn);
                if (fn.Length > 2 && Int32.TryParse(fn.Substring(2), out int _)) {
                    folderstring.Append("\"").Append(fn).Append("\"");
                    if (i + 1 < array.Length) {
                        folderstring.Append(",");
                    }
                }
            }

            requestJson = requestJson.Substring(0, requestJson.Length - 1) + ",\"x_apmcf_folders\":[" + folderstring + "]}";

            return __instance.sendData(requestNo, requestJson, callback, __instance.downloadAppData);
        }
    }
}