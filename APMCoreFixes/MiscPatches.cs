using am.abaas;
using AMDaemon;
using AMDaemon.Abaas;
using Apm.System.Setting.NonVolatile;
using Apm.System.Setting.Volatile;
using Apm.System.UnityUtil;
using Apm.System.Util;
using Apm.System.Warning;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine.UIElements;
using static Apm.System.Daemon.Input;
using static Apm.System.Error.ErrorResource;
using Version = AMDaemon.Version;

namespace APMCoreFixes {
    internal class MiscPatches {

        // Skip warning screen
        [HarmonyPrefix, HarmonyPatch(typeof(Warning), "StartAnimation")]
        static bool StartAnimation(AnimationController.AnimationEnd onEnd) {
            if (APMCF.ConfigSkipWarning.Value) {
                onEnd();
                return false;
            } else {
                return true;
            }
        }

        // Fake matching server online
        [HarmonyPrefix, HarmonyPatch(typeof(Link), "IsAvailable")]
        static bool IsAvailable(ref bool __result) {
            if (APMCF.ConfigFakeABaaSLinkOnline.Value) {
                __result = true;
                return false;
            } else {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AMDaemon.Apm), "StartGame")]
        public static bool StartGame(ref bool __result, Version appVersion, string gameId, bool withAime) {
            if (!APMCF.ConfigUseBatchLaunchSystem.Value) {
                return true;
            }
            __result = false;
            APMCF.Log.LogInfo("Launching " + gameId + "...");
            Thread.Sleep(1000); // let sound effect finish
            AppInfo game = AppListManager.GetInstance().Info.List.Find(p => p.subGameId == gameId);
            if (game == null) {
                APMCF.Log.LogError("No such game entry: " + gameId);
                Error.Set((int)ErrorNumber.ApmUnexpectedGameProgramFailure);
                return false;
            }
            string game_path = Path.GetDirectoryName(game.paths.images.Original);
            APMCF.Log.LogInfo("Directory is: " + game_path);
            try {
                Process p = Process.Start(new ProcessStartInfo("subst.exe", "W: " + game_path) {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                p.ErrorDataReceived += P_ErrorDataReceived;
                p.OutputDataReceived += P_OutputDataReceived;
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0) {
                    throw new Exception("Return code of subst is " + p.ExitCode);
                }
                __result = true;
            } catch (Exception ex) {
                APMCF.Log.LogError("Failed to set virtual drive: " + ex);
                Error.Set((int)ErrorNumber.CommonUnexpectedGameProgramFailure);
                return false;
            }
            return false;
        }

        private static void P_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            APMCF.Log.LogInfo("External: " + e.Data);
        }

        private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            APMCF.Log.LogError("External: " + e.Data);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.Warning.SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(Apm.System.Warning.SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                APMCF.Log.LogError("Game start not successful");
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.Setup.SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(Apm.System.Setup.SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                APMCF.Log.LogError("Game start not successful");
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.GameIconList.SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(Apm.System.GameIconList.SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                APMCF.Log.LogError("Game start not successful");
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.Daemon.Main), "IsRebootNeeded", MethodType.Getter)]
        static bool IsRebootNeeded(ref bool __result) {
            __result = false;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InputSystem), "Update")]
        static bool Update(InputSystem __instance) {
            if (APMCF.ConfigAMDAnalogInsteadOfButtons.Value) {
                if (__instance.sw == InputSwitch.up || __instance.sw == InputSwitch.right || __instance.sw == InputSwitch.down || __instance.sw == InputSwitch.left) {
                    UpdateAnalog(__instance);
                    return false;
                }
            }
            return true;
        }

        private static double map(double x, double in_min, double in_max, double out_min, double out_max) {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        private static void UpdateAnalog(InputSystem input) {
            InputUnit unit = Input.Players[0];

            double deadzone = APMCF.ConfigIO4StickDeadzone.Value / 100F;
            var ax = unit.GetAnalog(APMCF.AnalogX).Value;
            var ay = unit.GetAnalog(APMCF.AnalogY).Value;
            double x = map(ax, 0, 1, -1, 1);
            double y = map(ay, 0, 1, -1, 1);
            APMCF.Log.LogDebug(ax + "/" + ay);

            if (APMCF.ConfigIO4AxisXInvert.Value) {
                x = -x;
            }
            if (APMCF.ConfigIO4AxisYInvert.Value) {
                y = -y;
            }

            //APMCF.Log.LogDebug(x + "/" + y);

            bool on = (
                (input.sw == InputSwitch.up && y > deadzone) ||
                (input.sw == InputSwitch.right && x > deadzone) ||
                (input.sw == InputSwitch.down && y < -deadzone) ||
                (input.sw == InputSwitch.left && x < -deadzone)
            );

            if (on) {
                IsOn isOn = input.events.IsOn;
                if (isOn != null) {
                    isOn(input.sw);
                }
            } else {
                IsOff isOff = input.events.IsOff;
                if (isOff != null) {
                    isOff(input.sw);
                }
            }
        }
    }
}