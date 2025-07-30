using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney.GameConfig {
    public class Position {
        [JsonProperty] public int position;

        [JsonProperty] public Margin margin = new Margin();
    }
}