using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney.GameConfig {
    public class EntryIcons {
        [JsonProperty] public int direction;

        [JsonProperty] public Position position = new Position();
    }
}