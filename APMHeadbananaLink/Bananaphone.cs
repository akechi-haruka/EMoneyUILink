using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace APMHeadbananaLink {

    [BepInPlugin("eu.haruka.apm.bananaphone", "HeadbananaLink", "1.0")]
    public class Bananaphone : BaseUnityPlugin {

        [DllImport("apmHeadphoneVolume", EntryPoint = "apmHeadbananaVersionGet")]
        private static extern int ApmHeadbananaVersionGet();

        [DllImport("apmHeadphoneVolume", EntryPoint = "apmHeadphoneVolumeGet")]
        private static extern float ApmHeadphoneVolumeGet();

        [DllImport("apmHeadphoneVolume", EntryPoint = "apmHeadphoneVolumeSet")]
        private static extern void ApmHeadphoneVolumeSet(float volume);

        [DllImport("apmHeadphoneVolume", EntryPoint = "apmHeadphoneChannelsSet")]
        private static extern void ApmHeadphoneChannelsSet([MarshalAs(UnmanagedType.LPArray)]  int[] chanels, int len);

        private ConfigEntry<String> ConfigChannelList;

        public void Awake() {

            ConfigChannelList = Config.Bind("General", "Headphone Channels", "2,3", "A comma seperated list of channels that should be manipulated by APMHeadbanana");
            ConfigChannelList.SettingChanged += ConfigChannelList_SettingChanged;

            try {
                Logger.LogInfo("BANANA: version " +  ApmHeadbananaVersionGet());
            } catch {
                Logger.LogError("NO BANANA.");
                return;
            }

            UpdateChannels();
        }

        private void ConfigChannelList_SettingChanged(object sender, EventArgs e) {
            UpdateChannels();
        }

        private void UpdateChannels() {
            List<int> channels = new List<int>();
            foreach (String s in ConfigChannelList.Value.Split(',')) {
                if (Int32.TryParse(s, out int channel)) {
                    channels.Add(channel);
                } else {
                    Logger.LogWarning("Could not parse channel: " + s);
                }
            }
            if (channels.Count > 0) {
                ApmHeadphoneChannelsSet(channels.ToArray(), channels.Count);
                Logger.LogDebug("Channel list updated");
            } else {
                Logger.LogError("Channel list is empty");
            }
        }
    }
}
