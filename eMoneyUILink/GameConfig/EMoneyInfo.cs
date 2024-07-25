using Newtonsoft.Json;
using System;
using System.Linq;

namespace EMUISharedBackend.GameConfig
{
    public class EMoneyInfo
    {
        [JsonProperty]
        public bool enable;
        [JsonProperty]
        public bool paseli;
        [JsonProperty]
        public bool sound;
        [JsonProperty]
        public int[] credits = new int[5];
    }
}
