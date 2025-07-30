using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney.GameConfig {
    public class ConfigParser {
        [JsonProperty] public string titleName = "";

        [JsonProperty] public string subGameId = "";

        [JsonProperty] public bool aime;

        [JsonProperty] public EMoneyInfo emoney = new EMoneyInfo();

        [JsonProperty] public UiInfo ui = new UiInfo();

        [JsonProperty] public GamePadInfo gamepad = new GamePadInfo();
    }
}