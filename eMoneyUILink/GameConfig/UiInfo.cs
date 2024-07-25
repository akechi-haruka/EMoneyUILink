using Newtonsoft.Json;
using System;
using static EMUISharedBackend.GameConfig.ConfigParser;

namespace EMUISharedBackend.GameConfig
{
    public class UiInfo
    {
        public UiInfo(){
        }

        [JsonProperty]
        public EntryIcons entry_icons = new EntryIcons();

        [JsonProperty]
        public MainWindow main_window = new MainWindow();
    }
}
