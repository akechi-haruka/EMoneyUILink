using AMDaemon.Abaas;
using Apm.System.AbaasGs;
using Apm.System.Setting.NonVolatile;
using Apm.System.Util.Instruction;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static ABaaSGs;
using static Apm.System.Daemon.Network;

namespace APMCoreFixes {
    public class ABaaSGsPatches {

        [HarmonyPrefix, HarmonyPatch(typeof(Main), "Initialize")]
        static void Initialize() {

            APMCF.Log.LogInfo("Setting Debug Level");
            ABaaSGs.LIBAPI_STATUS status = ABaaSGs.Instance.debugLevel(10);

            if (APMCF.ConfigUnencryptedABaaSGs.Value) {
                APMCF.Log.LogInfo("Setting Config");
                CONNECTION_CONFIG cc = new CONNECTION_CONFIG() {
                    bCompress = false,
                    bEncrypt = false,
                    strDefaultUserAgent = "ABaaSAPMCoreFixes",
                    connectionTimeoutSec = 10,
                    bandLimitted = 0
                };
                status = ABaaSGs.Instance.setConnectionConfig(cc);
                APMCF.Log.LogInfo(status);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ABaaSGs), "gsGetResult")]
        static void gsGetResult(ref int __result, byte requestNo, ref ABaaSGs.SERVER_STATUS status) {
            if (__result != 1) {
                APMCF.Log.LogInfo("gsGetResult = " + __result + ", http:" + status.httpStatus + ", abaas:" + status.abaasGsStatus);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ABaaSGs), "returnResult")]
        static bool returnResult(byte requestNo, Action<ABaaSGs.ABAASGS_STATUS, string> callback) {
            ABaaSGs.ABAASGS_STATUS abaasgs_STATUS = default(ABaaSGs.ABAASGS_STATUS);
            abaasgs_STATUS.serverStatus.httpStatus = 200;
            abaasgs_STATUS.serverStatus.abaasGsStatus = 0;
            if (abaasgs_STATUS.libStatus == ABaaSGs.LIBAPI_STATUS.STATUS_OK) {
                int sz = ABaaSGs.gsGetRecvJsonSize(requestNo);
                StringBuilder stringBuilder = new StringBuilder(sz);
                abaasgs_STATUS.libStatus = (ABaaSGs.LIBAPI_STATUS)ABaaSGs.gsGetRecvJson(requestNo, stringBuilder, stringBuilder.Capacity);
                APMCF.Log.LogDebug("gsGetRecvJson ("+sz+"): http: " + abaasgs_STATUS.serverStatus.httpStatus + ", abaas: " + abaasgs_STATUS.serverStatus.abaasGsStatus + ", data: " + stringBuilder.ToString());
                callback(abaasgs_STATUS, stringBuilder.ToString());
                return false;
            }
            APMCF.Log.LogError("gsGetRecvJson: LIB_STATUS not ok: " + abaasgs_STATUS.libStatus);
            callback(abaasgs_STATUS, "");
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ABaaSGs), "downloadAppData", typeof(byte), typeof(string), typeof(Action<ABaaSGs.ABAASGS_STATUS, string>))]
        static IEnumerator downloadAppData(IEnumerator result, ABaaSGs __instance, byte requestNo, string requestJson, Action<ABaaSGs.ABAASGS_STATUS, string> callback) {
            if (!APMCF.ConfigAddXFolders.Value) {
                return __instance.sendData(requestNo, requestJson, callback, new Func<byte, string, ABaaSGs.LIBAPI_STATUS>(__instance.downloadAppData));
            }

            StringBuilder folderstring = new StringBuilder();

            string[] array = Directory.EnumerateDirectories(GeneralSettingManager.manager.Info.SetupSetting.ImageRootPath, "*", SearchOption.TopDirectoryOnly).ToArray();
            for (int i = 0; i < array.Length; i++) {
                string fn = Path.GetFileName(array[i]);
                APMCF.Log.LogInfo(fn);
                if (fn.Length > 2 && Int32.TryParse(fn.Substring(2), out int _)) {
                    folderstring.Append("\"").Append(fn).Append("\"");
                    if (i + 1 < array.Length) {
                        folderstring.Append(",");
                    }
                }
            }

            requestJson = requestJson.Substring(0, requestJson.Length - 1) + ",\"x_apmcf_folders\":["+folderstring+"]}";

            return __instance.sendData(requestNo, requestJson, callback, new Func<byte, string, ABaaSGs.LIBAPI_STATUS>(__instance.downloadAppData));
        }

    }
}
