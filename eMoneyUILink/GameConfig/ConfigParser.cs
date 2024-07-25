using eMoneyUILink;
using Haruka.Arcade.SEGA835Lib.Debugging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace EMUISharedBackend.GameConfig
{
    public class ConfigParser
    {

        [JsonProperty]
        public string titleName = "";

        [JsonProperty]
        public string subGameId = "";

        [JsonProperty]
        public bool aime;

        [JsonProperty]
        public EMoneyInfo emoney = new EMoneyInfo();

        [JsonProperty]
        public UiInfo ui = new UiInfo();

        [JsonProperty]
        public GamePadInfo gamepad = new GamePadInfo();

    }
}
