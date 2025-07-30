using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Apm.System.AbaasGs;
using Apm.System.Setting.NonVolatile;
using HarmonyLib;
using static ABaaSGs;

namespace APMCoreFixes {
    public class ABaaSGsPatches {
        [HarmonyPrefix, HarmonyPatch(typeof(Main), "Initialize")]
        static void Initialize() {
            ApmCoreFixes.Log.LogInfo("Setting Debug Level");
            Instance.debugLevel(10);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ABaaSGs), "gsGetResult")]
        static void gsGetResult(ref int __result, byte requestNo, ref SERVER_STATUS status) {
            if (__result != 1) {
                ApmCoreFixes.Log.LogInfo("gsGetResult = " + __result + ", http:" + status.httpStatus + ", abaas:" + status.abaasGsStatus);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ABaaSGs), "returnResult")]
        static bool returnResult(byte requestNo, Action<ABAASGS_STATUS, string> callback) {
            ABAASGS_STATUS abaasgsStatus = default(ABAASGS_STATUS);
            abaasgsStatus.serverStatus.httpStatus = 200;
            abaasgsStatus.serverStatus.abaasGsStatus = 0;
            if (abaasgsStatus.libStatus == LIBAPI_STATUS.STATUS_OK) {
                int sz = gsGetRecvJsonSize(requestNo);
                StringBuilder stringBuilder = new StringBuilder(sz);
                abaasgsStatus.libStatus = (LIBAPI_STATUS)gsGetRecvJson(requestNo, stringBuilder, stringBuilder.Capacity);
                ApmCoreFixes.Log.LogDebug("gsGetRecvJson (" + sz + "): http: " + abaasgsStatus.serverStatus.httpStatus + ", abaas: " + abaasgsStatus.serverStatus.abaasGsStatus + ", data: " + stringBuilder);
                callback(abaasgsStatus, stringBuilder.ToString());
                return false;
            }

            ApmCoreFixes.Log.LogError("gsGetRecvJson: LIB_STATUS not ok: " + abaasgsStatus.libStatus);
            callback(abaasgsStatus, "");
            return false;
        }

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