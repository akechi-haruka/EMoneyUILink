using Newtonsoft.Json;
using System;
using static EMUISharedBackend.GameConfig.ConfigParser;

namespace EMUISharedBackend.GameConfig
{
    public class Position
    {

        [JsonProperty]
        public int position;

        [JsonProperty]
        public Margin margin = new Margin();
    }
}
