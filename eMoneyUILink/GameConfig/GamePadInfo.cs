using Newtonsoft.Json;
using System;

namespace EMUISharedBackend.GameConfig
{
    public class GamePadInfo
    {
        public GamePadInfo()
        {
            enable = false;
            merge = false;
        }
        [JsonProperty]
        public bool enable;

        [JsonProperty]
        public bool merge;
    }
}
