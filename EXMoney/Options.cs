using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.EXMoney {
    public class Options {
        [Option("delay", Required = false, HelpText = "Waits with launching E-Money UI for the given seconds.")]
        [UsedImplicitly]
        public int UIDelay { get; set; }

        [Option('v', Required = false, HelpText = "Port to the VFD (unused if unset).")]
        [UsedImplicitly]
        public int VfdPort { get; set; }

        [Option("exe", Required = false, HelpText = "Path to emoneyUI.exe.", Default = "X:\\emoneyUI.exe")]
        [UsedImplicitly]
        public string EMoneyExecutable { get; set; }

        [Option("exe-args", Required = false, HelpText = "Arguments passed to emoneyUI.exe. Has no effect without mods.")]
        [UsedImplicitly]
        public string EMoneyArgs { get; set; }

        [Option("item-name", Required = false, HelpText = "Item name used for transactions.")]
        [UsedImplicitly]
        public string ItemName { get; set; }

        [Option('g', Required = false, HelpText = "SegAPI group ID.")]
        [UsedImplicitly]
        public int GroupId { get; set; }

        [Option('d', Required = false, HelpText = "SegAPI device ID.")]
        [UsedImplicitly]
        public int DeviceId { get; set; }

        [Option('p', Required = false, HelpText = "SegAPI port.", Default = 5364)]
        [UsedImplicitly]
        public int ApiPort { get; set; }

        [Option('b', Required = false, HelpText = "SegAPI broadcast address.", Default = "255.255.255.255")]
        [UsedImplicitly]
        public string ApiBroadcast { get; set; }

        [Option('k', Required = false, HelpText = "Keychip ID sent to the payment server.")]
        [UsedImplicitly]
        public string KeychipId { get; set; }

        [Option('s', Required = false, HelpText = "Path to segatools.ini.")]
        [UsedImplicitly]
        public string SegatoolsIniPath { get; set; }

        [Option("silent", Required = false, HelpText = "Disable console output.")]
        [UsedImplicitly]
        public bool Silent { get; set; }

        [Value(0, MetaName = "AppConfig", Required = true, HelpText = "The app.json file of the played game.")]
        [UsedImplicitly]
        public string AppConfig { get; set; }

        [Value(1, MetaName = "Server", Required = false, HelpText = "EXMoney/OpenMoney server address. If not specified no server will be used; infinite money will be available.")]
        [UsedImplicitly]
        public string Server { get; set; }
    }
}