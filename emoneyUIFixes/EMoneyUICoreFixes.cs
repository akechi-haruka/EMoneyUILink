using System;
using System.Diagnostics;
using Apm.Emoney.Ui;
using Apm.Emoney.Ui.GamePad;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Haruka.Arcade.SegAPI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using SceneManager = Apm.Emoney.Ui.SceneManager;

namespace Haruka.Arcade.EMUICF {
    [BepInPlugin("eu.haruka.gmg.apm.eui_fixes", "EMoneyUICoreFixes", "0.2.2")]
    [BepInProcess("emoneyUI")]
    public class EMoneyUICoreFixes : BaseUnityPlugin {
        public static ConfigEntry<bool> ConfigShrinkHitbox;
        public static ConfigEntry<bool> ConfigSegatoolsAddGameExitButton;
        public static ConfigEntry<bool> ConfigProcessAddGameExitButton;
        public static ConfigEntry<string> ConfigProcessExitList;
        public static ConfigEntry<int> ConfigSegatoolsGroupId;
        public static ConfigEntry<int> ConfigSegatoolsDeviceId;
        public static ConfigEntry<string> ConfigSegatoolsBroadcast;

        public static ManualLogSource Log;

        public static SegApi Api;

        public void Awake() {
            Log = Logger;

            SegApi.OnLogMessage += SegAPI_OnLogMessage;

            ConfigShrinkHitbox = Config.Bind("General", "Shrink Window Hitbox", true, "Shrinks the (invisible) window hitbox for less risk of touch swallowing if being close to eMoneyUI. This can also be passed with the \"-shrink-hitbox\" command line argument, so only some games can be affected.");
            ConfigProcessAddGameExitButton = Config.Bind("Segatools API", "Add Exit Game Button", true, "Adds a button that kills all processes from the process termination list.");
            ConfigProcessExitList = Config.Bind("General", "Process Termination List", "", "Comma-seperated list of processes to terminate.");

            ConfigSegatoolsAddGameExitButton = Config.Bind("Segatools API", "Add Segatools Exit Game Button", true, "Adds a button that sends a Segatools API \"Stop Game\" request.");
            ConfigSegatoolsGroupId = Config.Bind("Segatools API", "Group ID", 1, "Segatools API Group ID");
            ConfigSegatoolsDeviceId = Config.Bind("Segatools API", "Device ID", 1, "Segatools API Device ID");
            ConfigSegatoolsBroadcast = Config.Bind("Segatools API", "Broadcast Address", "255.255.255.255", "Segatools API Broadcast Address");

            Harmony.CreateAndPatchAll(typeof(Patches), "eu.haruka.gmg.apm.fixes.emoneyui.main");

            if (ConfigSegatoolsAddGameExitButton.Value) {
                Api = new SegApi((byte)ConfigSegatoolsGroupId.Value, (byte)ConfigSegatoolsDeviceId.Value, ConfigSegatoolsBroadcast.Value);
            }

            Log.LogInfo("Loaded");
        }

        private static void SegAPI_OnLogMessage(string obj) {
            Log.LogDebug(obj);
        }
    }

    public class Patches {
        // crashfix
        [HarmonyPrefix, HarmonyPatch(typeof(ApmInputApi), "IsEqual")]
        static bool IsEqual(ref ApmInputApi.ApmGamepadConfig config1, ref ApmInputApi.ApmGamepadConfig config2, ref bool __result) {
            if (config1.Sw == null) {
                __result = false;
                return false;
            }

            return true;
        }

        // Reduce hitbox of UI in minimized state
        [HarmonyPrefix, HarmonyPatch(typeof(SceneManager), "Start")]
        static bool Start(SceneManager __instance) {
            if (EMoneyUICoreFixes.ConfigShrinkHitbox.Value || Environment.CommandLine.Contains("-shrink-hitbox")) {
                /*EMUICF.Log.LogDebug("Menu rect: X=" + __instance.entryMenuRect.x + ",Y=" + __instance.entryMenuRect.y);*/
                EMoneyUICoreFixes.Log.LogDebug("Menu size: X=" + __instance.entryMenuSize.x + ",Y=" + __instance.entryMenuSize.y);
                EMoneyUICoreFixes.Log.LogDebug("Icon alignment: " + __instance.iconAxis);
                if (__instance.entryMenuSize.x >= 230 && __instance.entryMenuSize.y >= 150) {
                    if (__instance.iconAxis == GridLayoutGroup.Axis.Vertical) {
                        //__instance.entryMenuRect.x /= 2;
                        __instance.entryMenuSize.y /= 2;
                        EMoneyUICoreFixes.Log.LogInfo("Shrinking X axis");
                    } else if (__instance.iconAxis == GridLayoutGroup.Axis.Horizontal) {
                        //__instance.entryMenuRect.y /= 2;
                        __instance.entryMenuSize.x /= 2;
                        EMoneyUICoreFixes.Log.LogInfo("Shrinking Y axis");
                    }
                }
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EmoneyMenu), "ChangeState")]
        static void ChangeState(EmoneyMenu __instance, EmoneyMenu.State next) {
            if (next == EmoneyMenu.State.Item) {
                if (EMoneyUICoreFixes.ConfigSegatoolsAddGameExitButton.Value || EMoneyUICoreFixes.ConfigProcessAddGameExitButton.Value) {
                    GameObject exitButton = Object.Instantiate(__instance.buttonPrefab, __instance.itemButtons.transform);
                    exitButton.GetComponent<Button>().interactable = true;

                    ItemButton ib = exitButton.GetComponent<ItemButton>();
                    ib.Click.AddListener(SegatoolsSendExitGame);
                    ib.Price = 0;
                    ib.ItemName = "EXIT GAME";
                    ib.Controller = __instance.emoneyController.GetComponent<EmoneyController>();
                    ib.Index = 0;
                    __instance.items.Add("EXIT GAME", exitButton);
                    exitButton.transform.SetAsLastSibling();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ItemButton), "Price", MethodType.Setter)]
        static bool set_Price(ItemButton __instance, int value) {
            if (value > 0) {
                return true;
            }

            __instance.transform.Find("Price").gameObject.GetComponent<Text>().text = "";
            __instance.price = value;
            return false;
        }

        private static void SegatoolsSendExitGame(int _) {
            if (EMoneyUICoreFixes.Api != null) {
                EMoneyUICoreFixes.Api.SendExitGame();
            }

            if (EMoneyUICoreFixes.ConfigProcessAddGameExitButton.Value) {
                string[] proclist = EMoneyUICoreFixes.ConfigProcessExitList.Value.Split(',');
                foreach (string proc in proclist) {
                    foreach (Process p in Process.GetProcessesByName(proc)) {
                        EMoneyUICoreFixes.Log.LogInfo("Killing: " + p.ProcessName);
                        p.Kill();
                    }
                }
            }
        }
    }
}