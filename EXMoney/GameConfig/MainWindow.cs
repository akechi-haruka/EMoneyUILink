using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney.GameConfig {
    public class MainWindow {
        [JsonProperty] public Position position = new Position();
    }
}