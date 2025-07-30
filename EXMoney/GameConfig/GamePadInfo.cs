using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney.GameConfig {
    public class GamePadInfo {
        public GamePadInfo() {
            enable = false;
            merge = false;
        }

        [JsonProperty] public bool enable;

        [JsonProperty] public bool merge;
    }
}