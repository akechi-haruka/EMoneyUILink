using System;
using AMDaemon;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace APMCoreFixes {
    [BepInPlugin("eu.haruka.gmg.apm.fixes", "APMCoreFixes", "0.2")]
    [BepInProcess("Apmv3System")]
    public class ApmCoreFixes : BaseUnityPlugin {
        private const string CAT_HOME_USE = "Home Use";
        private const string CAT_INPUT = "Input";
        private const string CAT_NETWORK = "Network";

        public static ManualLogSource Log;
        public static InputId AnalogX { get; private set; }
        public static InputId AnalogY { get; private set; }

        public static ConfigEntry<bool> ConfigDisableOPTPresenceCheck;
        public static ConfigEntry<bool> ConfigDisableNameChecks;
        public static ConfigEntry<bool> ConfigSkipWarning;
        public static ConfigEntry<bool> ConfigUseBatchLaunchSystem;
        public static ConfigEntry<string> ConfigOptionDirectory;
        public static ConfigEntry<bool> ConfigShowMouse;
        public static ConfigEntry<bool> ConfigShowClock;
        public static ConfigEntry<bool> ConfigAddXFolders;

        public static ConfigEntry<bool> ConfigAMDAnalogInsteadOfButtons;
        public static ConfigEntry<int> ConfigIO4StickDeadzone;
        public static ConfigEntry<bool> ConfigIO4AxisXInvert;
        public static ConfigEntry<bool> ConfigIO4AxisYInvert;

        private DateTime lastClockUpdate = DateTime.Now;
        private GameObject clock;

        public void Awake() {
            Log = Logger;

            ConfigDisableOPTPresenceCheck = Config.Bind(CAT_HOME_USE, "Disable .opt Presence Check", true, "Disables the required existence of (any) .opt files in game directories.");
            ConfigDisableNameChecks = Config.Bind(CAT_HOME_USE, "Disable Game Name Checking", true, "Disables various checks related to game names and game IDs for files and folders.");
            ConfigSkipWarning = Config.Bind(CAT_HOME_USE, "Skip Japan Warning", false, "Skips the \"only use in Japan\" warning.");
            ConfigUseBatchLaunchSystem = Config.Bind(CAT_HOME_USE, "Use root .bat launchers", true, new ConfigDescription("If a game.bat is placed outside the App directory, launch that directly instead of via amdaemon / mount routines. See readme for more information."));
            ConfigOptionDirectory = Config.Bind(CAT_HOME_USE, "Option directory", "option", new ConfigDescription("If root .bat launchers are enabled, set the option directory here (same as in segatools.ini, may be absolute or relative). See readme for more information."));
            ConfigShowMouse = Config.Bind(CAT_HOME_USE, "Show Mouse", false, "Shows the mouse cursor.");
            ConfigShowClock = Config.Bind(CAT_HOME_USE, "Show Clock", true, "Shows a clock on the game selection screen.");
            ConfigAddXFolders = Config.Bind(CAT_NETWORK, "Add X-APMCF-Folders Field", true, new ConfigDescription("Adds all existing folders to an extra field in network communication when fetching game list.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));

            ConfigAMDAnalogInsteadOfButtons = Config.Bind(CAT_INPUT, "Use Analog instead of buttons", false, "Use analog for navigation instead of 4 buttons (Requires config_hook.json, see readme)");
            ConfigIO4StickDeadzone = Config.Bind(CAT_INPUT, "Stick Deadzone", 30, "The stick deadzone in percent");
            ConfigIO4AxisXInvert = Config.Bind(CAT_INPUT, "X Axis Invert", false, "Inverts the X axis");
            ConfigIO4AxisYInvert = Config.Bind(CAT_INPUT, "Y Axis Invert", false, "Inverts the Y axis");

            Harmony.CreateAndPatchAll(typeof(ABaaSGsPatches), "eu.haruka.gmg.apm.fixes.abaasgs");
            Harmony.CreateAndPatchAll(typeof(APMDebugPatches), "eu.haruka.gmg.apm.fixes.debug");
            Harmony.CreateAndPatchAll(typeof(MiscPatches), "eu.haruka.gmg.apm.fixes.misc");
        }

        public void Update() {
            if (AnalogX == null && Core.IsReady) {
                AnalogX = new InputId("analog_x");
                AnalogY = new InputId("analog_y");
                Log.LogInfo("Analog initialized");
            }

            if (!Cursor.visible && ConfigShowMouse.Value) {
                Cursor.visible = true;
            }

            // Clock on game selection screen
            if (ConfigShowClock.Value) {
                DateTime now = DateTime.Now;
                if (lastClockUpdate.Second != now.Second) {
                    lastClockUpdate = now;
                    if (clock == null || !clock.activeInHierarchy) {
                        GameObject text = GameObject.Find("MainCanvas/HeaderCanvas/CoopGroup");
                        if (text != null) {
                            clock = Instantiate(text, text.transform.parent);
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
            }
        }
    }
}