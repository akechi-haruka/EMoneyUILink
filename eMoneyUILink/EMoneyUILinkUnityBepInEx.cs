using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Emoney.SharedMemory;
using EMUISharedBackend.GameConfig;
using Haruka.Arcade.SEGA835Lib.Devices;
using Haruka.Arcade.SEGA835Lib.Devices.Card;
using Haruka.Arcade.SEGA835Lib.Devices.Card._837_15396;
using Haruka.Arcade.SEGA835Lib.Devices.Misc;
using Newtonsoft.Json;
using OpenAimeIO_Managed.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace eMoneyUILink
{
    [BepInPlugin("eu.haruka.gmg.apm.emoneyuilink", "EMoneyUILink", "1.0")]
    public class EMoneyUILinkUnityBepInEx : BaseUnityPlugin {

        public static ManualLogSource Log;

        public static ConfigEntry<String> ConfigKeychip;
        public static ConfigEntry<String> ConfigOpenMoneyAddress;
        private static ConfigEntry<String> EMoneyConfigFile;
        private static ConfigEntry<String> EMoneyUiExecutable;
        private static ConfigEntry<String> EMoneyItemName;
        private static ConfigEntry<int> VFDPort;
        private static ConfigEntry<bool> SegaLibLogging;

        public void Awake() {

            Log = Logger;

            EMoneyConfigFile = Config.Bind("General", "Config File", "W:\\app.json", "Path to app.json");
            EMoneyUiExecutable = Config.Bind("General", "UI Executable", "C:\\apm\\emoneyUI.exe", "Path to emoneyUI.exe");
            ConfigOpenMoneyAddress = Config.Bind("Network", "OpenMoney Endpoint", "http://127.0.0.1/openmoney/request2", "Address to OpenMoney server");
            ConfigKeychip = Config.Bind("Network", "Keychip ID", "A00E-01E00000000", "Keychip ID");
            EMoneyItemName = Config.Bind("Network", "Item Name", "OpenMoney Payment", "Item Name for the EMoney payment. This may show up in payment logs for the user.");
            VFDPort = Config.Bind("Real Hardware", "VFD Port", 0, "Port for VFD");
            SegaLibLogging = Config.Bind("Real Hardware", "SegaLib Logging", true, "Enable Sega835Lib logs");

            Haruka.Arcade.SEGA835Lib.Debugging.Log.Mute = true;
            Haruka.Arcade.SEGA835Lib.Debugging.Log.LogMessageWritten += Log_LogMessageWritten;

            EMoneyUILink.Initialize(EMoneyConfigFile.Value, VFDPort.Value, EMoneyUiExecutable.Value, ConfigOpenMoneyAddress.Value, ConfigKeychip.Value, LogMessage, EMoneyItemName.Value);

        }

        private void LogMessage(string obj) {
            Log.LogDebug(obj);
        }

        private void Log_LogMessageWritten(Haruka.Arcade.SEGA835Lib.Debugging.LogEntry obj) {
            if (SegaLibLogging.Value) {
                Log.LogDebug("Sega835Lib: " + obj.Message);
            }
        }

        
    }
}
