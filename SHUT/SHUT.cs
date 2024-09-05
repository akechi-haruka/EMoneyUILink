using BepInEx;
using BepInEx.Configuration;
using Haruka.Arcade.SegatoolsAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SHUT
{
    [BepInPlugin("eu.haruka.gmg.shut", "SHUT", "1.0")]
    public class SHUT : BaseUnityPlugin {

        public static ConfigEntry<int> ConfigSegatoolsGID;
        public static ConfigEntry<int> ConfigSegatoolsDID;
        public static ConfigEntry<string> ConfigSegatoolsBC;
        public static ConfigEntry<bool> ConfigAllowShut;

        private SegatoolsAPI2 segatools;

        public void Awake() {
            ConfigSegatoolsGID = Config.Bind("Segatools API", "Group ID", 1, "Segatools API Group ID");
            ConfigSegatoolsDID = Config.Bind("Segatools API", "Device ID", 9, "Segatools API Device ID");
            ConfigSegatoolsBC = Config.Bind("Segatools API", "Broadcast Address", "255.255.255.255", "Segatools API Broadcast Address");
            ConfigAllowShut = Config.Bind("General", "Allow SHUT", true, "Allow Segatools API \"Exit Game\"?");

            SegatoolsAPI2.OnLogMessage += SegatoolsAPI2_OnLogMessage;

            segatools = new SegatoolsAPI2((byte)ConfigSegatoolsGID.Value, (byte)ConfigSegatoolsDID.Value, ConfigSegatoolsBC.Value);
            segatools.OnExitGame += Segatools_OnExitGame;
        }

        private void Segatools_OnExitGame() {
            Logger.LogMessage("Received exit command!");
            if (ConfigAllowShut.Value) {
                Process.GetCurrentProcess().Kill();
            }
        }

        private void SegatoolsAPI2_OnLogMessage(string obj) {
            Logger.LogDebug(obj);
        }

        public void Update() {

        }

    }
}
