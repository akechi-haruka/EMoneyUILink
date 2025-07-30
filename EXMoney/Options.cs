using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.EXMoney {
    public class Options {
        [Option("delay", Required = false, HelpText = "Waits with launching E-Money UI for the given seconds")]
        [UsedImplicitly]
        public int UIDelay { get; }

        [Option('v', Required = false, HelpText = "Port to the VFD (unused if unset)")]
        [UsedImplicitly]
        public int VfdPort { get; }

        [Option("exe", Required = false, HelpText = "Path to emoneyUI.exe", Default = "X:\\emoneyUI.exe")]
        [UsedImplicitly]
        public string EMoneyExecutable { get; }

        [Option("item-name", Required = false, HelpText = "Item name used for transactions")]
        [UsedImplicitly]
        public string ItemName { get; }

        [Option('g', Required = false, HelpText = "SegAPI group ID")]
        [UsedImplicitly]
        public int GroupId { get; }

        [Option('d', Required = false, HelpText = "SegAPI device ID")]
        [UsedImplicitly]
        public int DeviceId { get; }

        [Option('p', Required = false, HelpText = "SegAPI port", Default = 5364)]
        [UsedImplicitly]
        public int ApiPort { get; }

        [Option('b', Required = false, HelpText = "SegAPI broadcast address", Default = "255.255.255.255")]
        [UsedImplicitly]
        public string ApiBroadcast { get; }

        [Option('k', Required = false, HelpText = "Keychip ID")]
        [UsedImplicitly]
        public string KeychipId { get; }

        [Option('s', Required = false, HelpText = "Path to segatools.ini")]
        [UsedImplicitly]
        public string SegatoolsIniPath { get; }

        [Option("silent", Required = false, HelpText = "Disable console output")]
        [UsedImplicitly]
        public bool Silent { get; }

        [Value(0, MetaName = "AppConfig", Required = true, HelpText = "The app.json file of the played game")]
        [UsedImplicitly]
        public string AppConfig { get; }

        [Value(1, MetaName = "Server", Required = false, HelpText = "EXMoney/OpenMoney server address. If not specified no server will be used; infinite money will be available")]
        [UsedImplicitly]
        public string Server { get; }
    }
}