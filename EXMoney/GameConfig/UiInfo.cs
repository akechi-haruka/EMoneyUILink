using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney.GameConfig {
    public class UiInfo {
        [JsonProperty] public EntryIcons entry_icons = new EntryIcons();

        [JsonProperty] public MainWindow main_window = new MainWindow();
    }
}