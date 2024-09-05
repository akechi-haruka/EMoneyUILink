using Apm.Emoney.Ui;
using Apm.Emoney.Ui.GamePad;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Emoney.SharedMemory;
using HarmonyLib;
using Haruka.Arcade.SegatoolsAPI;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SceneManager = Apm.Emoney.Ui.SceneManager;

namespace emoneyUIFixes {
    [BepInPlugin("eu.haruka.gmg.apm.eui_fixes", "EMoneyUICoreFixes", "0.2")]
    [BepInProcess("emoneyUI")]
    public class EMUICF : BaseUnityPlugin {

        public static ConfigEntry<bool> ConfigShrinkHitbox;
        public static ConfigEntry<int> ConfigDelayStart;
        public static ConfigEntry<bool> ConfigAllowOptionsGreater500;
        public static ConfigEntry<bool> ConfigSegatoolsAddGameExitButton;
        public static ConfigEntry<bool> ConfigProcessAddGameExitButton;
        public static ConfigEntry<string> ConfigProcessExitList;
        public static ConfigEntry<int> ConfigSegatoolsGID;
        public static ConfigEntry<int> ConfigSegatoolsDID;
        public static ConfigEntry<string> ConfigSegatoolsBC;

        public static ManualLogSource Log;

        internal static SegatoolsAPI2 segatools;

        public void Awake() {
            Log = Logger;

            SegatoolsAPI2.OnLogMessage += SegatoolsAPI2_OnLogMessage;

            ConfigShrinkHitbox = Config.Bind("General", "Shrink Window Hitbox", true, "Shrinks the (invisible) window hitbox for less risk of touch swallowing if being close to eMoneyUI");
            ConfigDelayStart = Config.Bind("General", "Startup Delay", 2, "Time delay in seconds until eMoneyUI shows up");
            ConfigAllowOptionsGreater500 = Config.Bind("General", "Allow items more expensive than 500 yen", true, "Allows buttons to contain items up to 10000 yen");
            ConfigProcessAddGameExitButton = Config.Bind("Segatools API", "Add Exit Game Button", true, "Adds a button that kills all processes from the process termination list.");
            ConfigProcessExitList = Config.Bind("General", "Process Termination List", "", "Comma-seperated list of processes to terminate.");

            ConfigSegatoolsAddGameExitButton = Config.Bind("Segatools API", "Add Segatools Exit Game Button", true, "Adds a button that sends a Segatools API \"Stop Game\" request.");
            ConfigSegatoolsGID = Config.Bind("Segatools API", "Group ID", 1, "Segatools API Group ID");
            ConfigSegatoolsDID = Config.Bind("Segatools API", "Device ID", 1, "Segatools API Device ID");
            ConfigSegatoolsBC = Config.Bind("Segatools API", "Broadcast Address", "255.255.255.255", "Segatools API Broadcast Address");

            Harmony.CreateAndPatchAll(typeof(Patches), "eu.haruka.gmg.apm.fixes.emoneyui.main");

            if (ConfigSegatoolsAddGameExitButton.Value) {
                segatools = new SegatoolsAPI2((byte)ConfigSegatoolsGID.Value, (byte)ConfigSegatoolsDID.Value, ConfigSegatoolsBC.Value);
            }

            Log.LogInfo("Loaded");

            if (ConfigDelayStart.Value > 0) {
                Log.LogInfo("Waiting " +  ConfigDelayStart.Value + " second(s)...");
                Thread.Sleep(ConfigDelayStart.Value * 1000);
            }

        }

        private void SegatoolsAPI2_OnLogMessage(string obj) {
            Log.LogDebug(obj);
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
            if (EMUICF.ConfigShrinkHitbox.Value) {
                /*EMUICF.Log.LogDebug("Menu rect: X=" + __instance.entryMenuRect.x + ",Y=" + __instance.entryMenuRect.y);*/
                EMUICF.Log.LogDebug("Menu size: X=" + __instance.entryMenuSize.x + ",Y=" + __instance.entryMenuSize.y);
                EMUICF.Log.LogDebug("Icon alignment: " + __instance.iconAxis);
                if (__instance.entryMenuSize.x >= 230 && __instance.entryMenuSize.y >= 150) {
                    if (__instance.iconAxis == GridLayoutGroup.Axis.Vertical) {
                        //__instance.entryMenuRect.x /= 2;
                        __instance.entryMenuSize.y /= 2;
                        EMUICF.Log.LogInfo("Shrinking X axis");
                    } else if (__instance.iconAxis == GridLayoutGroup.Axis.Horizontal) {
                        //__instance.entryMenuRect.y /= 2;
                        __instance.entryMenuSize.x /= 2;
                        EMUICF.Log.LogInfo("Shrinking Y axis");
                    }
                }
                
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EmoneyMenu), "ChangeState")]
        static void ChangeState(EmoneyMenu __instance, EmoneyMenu.State next) {
            if (next == EmoneyMenu.State.Item) {
                
                if (EMUICF.ConfigSegatoolsAddGameExitButton.Value || EMUICF.ConfigProcessAddGameExitButton.Value) {

                    GameObject exitButton = UnityEngine.Object.Instantiate<GameObject>(__instance.buttonPrefab, __instance.itemButtons.transform);
                    exitButton.GetComponent<Button>().interactable = true;

                    ItemButton ib = exitButton.GetComponent<ItemButton>();
                    ib.Click.AddListener(new UnityAction<int>(SegatoolsSendExitGame));
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
            if (EMUICF.segatools != null) {
                EMUICF.segatools.SendExitGame();
            }
            if (EMUICF.ConfigProcessAddGameExitButton.Value) {
                string[] proclist = EMUICF.ConfigProcessExitList.Value.Split(',');
                foreach (string proc in proclist) {
                    foreach (Process p in Process.GetProcessesByName(proc)) {
                        EMUICF.Log.LogInfo("Killing: " + p.ProcessName);
                        p.Kill();
                    }
                }
            }
        }

    }
}
