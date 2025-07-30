using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AMDaemon;
using Apm.System.AbaasLink;
using Apm.System.Setting.Volatile;
using Apm.System.UnityUtil;
using Apm.System.Util.Log;
using Apm.System.Warning;
using HarmonyLib;
using static Apm.System.Daemon.Input;
using static Apm.System.Error.ErrorResource;
using SceneManager = Apm.System.GameIconList.SceneManager;

namespace APMCoreFixes {
    internal class MiscPatches {
        // Skip warning screen
        [HarmonyPrefix, HarmonyPatch(typeof(Warning), "StartAnimation")]
        static bool StartAnimation(AnimationController.AnimationEnd onEnd) {
            if (ApmCoreFixes.ConfigSkipWarning.Value) {
                onEnd();
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneManager), "GameStart")]
        public static bool GameStart(string subGameId, string version, AppAdditionalInfo info, SceneManager __instance) {
            if (!ApmCoreFixes.ConfigUseBatchLaunchSystem.Value) {
                return true;
            }

            ApmCoreFixes.Log.LogInfo("Launching " + subGameId + "...");
            AppInfo game = AppListManager.GetInstance().Info.List.Find(p => p.subGameId == subGameId);
            if (game == null) {
                ApmCoreFixes.Log.LogError("No such game entry: " + subGameId);
                Error.Set((int)ErrorNumber.ApmUnexpectedGameProgramFailure);
                return false;
            }

            string game_path = Path.GetDirectoryName(game.paths.images.Original);
            ApmCoreFixes.Log.LogInfo("Directory is: " + game_path);
            if (game_path.StartsWith("C:\\Mount\\Option")) {
                game_path = game_path.Replace("C:\\Mount\\Option", ApmCoreFixes.ConfigOptionDirectory.Value);
                ApmCoreFixes.Log.LogInfo("Optionified directory is: " + game_path);
            }

            if (!File.Exists(Path.Combine(game_path, "game.bat"))) {
                ApmCoreFixes.Log.LogWarning("No game.bat in root directory found, falling back to actual start routine!");
                return true;
            }

            Thread.Sleep(1000); // let sound effect finish

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
            } catch (Exception ex) {
                ApmCoreFixes.Log.LogError("Failed to set virtual drive: " + ex);
                Error.Set((int)ErrorNumber.CommonUnexpectedGameProgramFailure);
                return false;
            }

            __instance.isStartingGame = true;
            __instance.launchSubGameId = subGameId;
            __instance.launchVersion = version;
            if (!__instance.bootApplication) {
                __instance.isMountEnd = true;
                __instance.isStartGameEnd = true;
                return false;
            }

            SystemConfigManager.GetInstance().Info.EMoney = info.EMoney;
            SystemConfigManager.GetInstance().Info.Ui = info.Ui;
            SystemConfigManager.GetInstance().Info.GamePad = info.GamePad;
            PlayLogSender.Save("Launch " + subGameId + " Ver." + version);
            __instance.abaasGsController.GetComponent<Apm.System.AbaasGs.Main>().Cancel();
            __instance.daemonMain.StartGame(subGameId, __instance.OnStartGameEnd);
            __instance.OnMountEnd(true);
            return false;
        }

        private static void P_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            ApmCoreFixes.Log.LogInfo("External: " + e.Data);
        }

        private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            ApmCoreFixes.Log.LogError("External: " + e.Data);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.Warning.SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(Apm.System.Warning.SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                ApmCoreFixes.Log.LogError("Game start not successful");
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.Setup.SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(Apm.System.Setup.SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                ApmCoreFixes.Log.LogError("Game start not successful");
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                ApmCoreFixes.Log.LogError("Game start not successful");
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
            if (ApmCoreFixes.ConfigAMDAnalogInsteadOfButtons.Value) {
                if (__instance.sw == InputSwitch.up || __instance.sw == InputSwitch.right || __instance.sw == InputSwitch.down || __instance.sw == InputSwitch.left) {
                    UpdateAnalog(__instance);
                    return false;
                }
            }

            return true;
        }

        private static double Map(double x, double in_min, double in_max, double out_min, double out_max) {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        private static void UpdateAnalog(InputSystem input) {
            InputUnit unit = Input.Players[0];

            double deadzone = ApmCoreFixes.ConfigIO4StickDeadzone.Value / 100F;
            var ax = unit.GetAnalog(ApmCoreFixes.AnalogX).Value;
            var ay = unit.GetAnalog(ApmCoreFixes.AnalogY).Value;
            double x = Map(ax, 0, 1, -1, 1);
            double y = Map(ay, 0, 1, -1, 1);
            //APMCF.Log.LogDebug(ax + "/" + ay);

            if (ApmCoreFixes.ConfigIO4AxisXInvert.Value) {
                x = -x;
            }

            if (ApmCoreFixes.ConfigIO4AxisYInvert.Value) {
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

        [HarmonyPostfix, HarmonyPatch(typeof(Main), "Initialize")]
        static void Initialize(Main __instance) {
            __instance.abaasLink.SetDebugLevel(99);
            __instance.abaasLink.SetDebugOutputFunc(debugfunc);
            ApmCoreFixes.Log.LogInfo("Installed AbaasLink debugging");
            ApmCoreFixes.Log.LogInfo("Server URI: " + __instance.daemonMain.AllNet.AbaasLinkServerName);
        }

        private static void debugfunc(string message) {
            ApmCoreFixes.Log.LogInfo("AbaasLink: " + message);
        }
    }
}