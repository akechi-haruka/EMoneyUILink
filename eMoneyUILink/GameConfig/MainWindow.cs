using Newtonsoft.Json;
using System;
using static EMUISharedBackend.GameConfig.ConfigParser;

namespace EMUISharedBackend.GameConfig
{
    public class MainWindow
    {

        [JsonProperty]
        public Position position = new Position();
    }
}
