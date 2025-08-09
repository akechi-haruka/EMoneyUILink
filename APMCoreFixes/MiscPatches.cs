using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AMDaemon;
using Apm.System.AbaasGs;
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

            if (game_path.StartsWith(@"C:\Mount\Option")) {
                ApmCoreFixes.Log.LogDebug("Default option path detected: " + game_path);
                IniFile segatools = new IniFile("segatools.ini");
                bool vfs_disabled = "0".Equals(segatools.Read("enabled", "vfs"));
                string vfs_option = segatools.Read("option", "vfs");
                if (!vfs_disabled && !String.IsNullOrWhiteSpace(vfs_option)) {
                    game_path = game_path.Replace(@"C:\Mount\Option", vfs_option);
                    ApmCoreFixes.Log.LogInfo("Path adjusted to " + game_path);
                }
            }

            if (!File.Exists(Path.Combine(game_path, "game.bat"))) {
                ApmCoreFixes.Log.LogWarning("No game.bat in root directory found, falling back to actual start routine!");
                return true;
            }

            Thread.Sleep(1000); // let sound effect finish

            ApmCoreFixes.Log.LogDebug("Setting virtual drive");
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

            ApmCoreFixes.Log.LogDebug("Virtual drive set");

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
            ApmCoreFixes.Log.LogDebug("Cancel Network");
            __instance.abaasGsController.GetComponent<Main>().Cancel();
            ApmCoreFixes.Log.LogDebug("OnMountEnd");
            __instance.OnMountEnd(true);
            ApmCoreFixes.Log.LogDebug("OnStartGameEnd");
            __instance.OnStartGameEnd(true);
            ApmCoreFixes.Log.LogDebug("OK");
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
                ApmCoreFixes.Log.LogError("Game start not successful (Warning)");
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Apm.System.Setup.SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(Apm.System.Setup.SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                ApmCoreFixes.Log.LogError("Game start not successful (Setup)");
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneManager), "OnStartGameEnd")]
        static bool OnStartGameEnd(SceneManager __instance, bool isSucceeded) {
            if (isSucceeded) {
                __instance.isStartGameEnd = true;
            } else {
                ApmCoreFixes.Log.LogError("Game start not successful (GameList)");
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
            if (ApmCoreFixes.ConfigAmdAnalogInsteadOfButtons.Value) {
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
    }
}