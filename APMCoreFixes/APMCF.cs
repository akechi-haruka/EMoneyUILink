using am.abaas;
using AMDaemon;
using Apm.System.Setting.NonVolatile;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace APMCoreFixes {

    [BepInPlugin("eu.haruka.gmg.apm.fixes", "APMCoreFixes", "0.1")]
    [BepInProcess("Apmv3System")]
    public class APMCF : BaseUnityPlugin {

        private const String CAT_HOME_USE = "Home Use";
        private const String CAT_INPUT = "Input";
        private const String CAT_NETWORK = "Network";
        private const String CAT_DEBUG = "Debug";

        public static ManualLogSource Log;
        public static InputId AnalogX { get; private set; }
        public static InputId AnalogY { get; private set; }

        public static ConfigEntry<bool> ConfigUnencryptedABaaSGs;
        public static ConfigEntry<bool> ConfigFakeABaaSLinkOnline;
        public static ConfigEntry<bool> ConfigSkipVHDMount;
        public static ConfigEntry<bool> ConfigDisableOPTPresenceCheck;
        public static ConfigEntry<bool> ConfigDisableNameChecks;
        public static ConfigEntry<bool> ConfigSkipWarning;
        public static ConfigEntry<bool> ConfigUseBatchLaunchSystem;
        public static ConfigEntry<KeyboardShortcut> ConfigDebugFakeAimeRead;

        public static ConfigEntry<bool> ConfigAMDAnalogInsteadOfButtons;
        public static ConfigEntry<int> ConfigIO4StickDeadzone;
        public static ConfigEntry<bool> ConfigHardTranslations;
        public static ConfigEntry<bool> ConfigIO4AxisXInvert;
        public static ConfigEntry<bool> ConfigIO4AxisYInvert;

        private DateTime lastClockUpdate = DateTime.Now;
        private GameObject clock;

        public void Awake() {
            Log = Logger;

            ConfigUnencryptedABaaSGs = Config.Bind(CAT_NETWORK, "Unencrypted ABaaSGs Communication", true, "Disabled ABaaSGs encryption and compression");
            ConfigFakeABaaSLinkOnline = Config.Bind(CAT_NETWORK, "Fake ABaaSLink Online", true, "Simulates the matching server being online. Serves no purpose except getting a green online indicator.");
            ConfigSkipVHDMount = Config.Bind(CAT_HOME_USE, "No VHD Mounting", true, "Disables VHD mounting/unmounting. Use if using decrypted data.");
            ConfigDisableOPTPresenceCheck = Config.Bind(CAT_HOME_USE, "Disable .opt Presence Check", true, "Disables the required existence of (any) .opt files in game directories.");
            ConfigDisableNameChecks = Config.Bind(CAT_HOME_USE, "Disable Game Name Checking", true, "Disables various checks related to game names and game IDs for files and folders.");
            ConfigSkipWarning = Config.Bind(CAT_HOME_USE, "Skip Japan Warning", false, "Skips the \"only use in Japan\" warning.");
            ConfigUseBatchLaunchSystem = Config.Bind(CAT_HOME_USE, "Use .bat launchers", true, "Instead of amdaemon, use .bat files to launch games, see readme");
            ConfigDebugFakeAimeRead = Config.Bind(CAT_DEBUG, "Aime reader debug scan key", new KeyboardShortcut(KeyCode.F11), "for development use");

            ConfigAMDAnalogInsteadOfButtons = Config.Bind(CAT_INPUT, "Use Analog instead of buttons", false, "Use analog for navigation instead of 4 buttons (Requires a modified common.json)");
            ConfigIO4StickDeadzone = Config.Bind(CAT_INPUT, "Stick Deadzone", 30, "The stick deadzone in percent");
            ConfigIO4AxisXInvert = Config.Bind(CAT_INPUT, "X Axis Invert", false, "Inverts the X axis");
            ConfigIO4AxisYInvert = Config.Bind(CAT_INPUT, "Y Axis Invert", false, "Inverts the Y axis");

            Harmony.CreateAndPatchAll(typeof(ABaaSGsPatches), "eu.haruka.gmg.apm.fixes.abaasgs");
            Harmony.CreateAndPatchAll(typeof(APMDebugPatches), "eu.haruka.gmg.apm.fixes.debug");
            Harmony.CreateAndPatchAll(typeof(MiscPatches), "eu.haruka.gmg.apm.fixes.misc");
            Harmony.CreateAndPatchAll(typeof(AppMounterPatches), "eu.haruka.gmg.apm.fixes.vhd");
        }

        public void Update() {
            if (AnalogX == null && AMDaemon.Core.IsReady) {
                AnalogX = new InputId("analog_x");
                AnalogY = new InputId("analog_y");
                Log.LogInfo("Analog initialized");
            }

            if (!Cursor.visible && GeneralSettingManager.manager.Info.SubSystemTestSetting.UseMouseInput) {
                Cursor.visible = true;
            }

            // Clock on game selection screen
            DateTime now = DateTime.Now;
            if (lastClockUpdate.Second != now.Second) {
                lastClockUpdate = now;
                if (clock == null || !clock.activeInHierarchy) {
                    GameObject text = GameObject.Find("MainCanvas/HeaderCanvas/CoopGroup");
                    if (text != null) {
                        clock = UnityEngine.Object.Instantiate(text, text.transform.parent);
                        clock.transform.position += new Vector3(0, -30);
                        clock.SetActive(true);
                    }
                }
                if (clock != null) {
                    Text textComponent = clock.GetComponent<Text>();
                    if (textComponent != null) {
                        textComponent.text = lastClockUpdate.ToString();
                    }
                }
            }

            if (ConfigDebugFakeAimeRead.Value.IsDown()) {
                new Thread(AimeDebugReadStart).Start();
            }
        }
        
        private void AimeDebugReadStart() {
            Log.LogMessage("aime debug read start");
            var aime = Aime.Units[0];
            while (aime.IsBusy) {
                Log.LogMessage("waiting");
                Thread.Sleep(1000);
            }
            aime.Start(AimeCommand.Scan);
            Log.LogMessage("Scan start");
            while (aime.IsBusy) {
                Log.LogMessage("waiting");
                Thread.Sleep(1000);
            }
            
            if (!aime.HasResult) {
                Log.LogMessage("no result!");
                return;
            }

            if (aime.HasError) {
                Log.LogMessage("Error: " + aime.ErrorInfo.Number + ", " + aime.ErrorInfo.Category + "; " + aime.ErrorInfo.Message);
            }
            Log.LogMessage("AC:" + aime.Result.AccessCode);
            Log.LogMessage("AK;" + aime.Result.SegaIdAuthKey);
        }
    }
}
