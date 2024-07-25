using Newtonsoft.Json;
using System;
using static EMUISharedBackend.GameConfig.ConfigParser;

namespace EMUISharedBackend.GameConfig
{
    public class EntryIcons
    {

        [JsonProperty]
        public int direction;

        [JsonProperty]
        public Position position = new Position();
    }
}
