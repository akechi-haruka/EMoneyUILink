using System.Diagnostics;
using System.Reflection;
using CommandLine;
using Haruka.Arcade.EXMoney.Debugging;
using Haruka.Arcade.EXMoney.GameConfig;
using Haruka.Arcade.EXMoney.SharedMemory;
using Haruka.Arcade.SEGA835Lib.Debugging;
using Haruka.Arcade.SEGA835Lib.Devices;
using Haruka.Arcade.SEGA835Lib.Devices.Misc;
using Haruka.Arcade.SegAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney;

class Program {
    public static readonly string NAME;

    public static VFD_GP1232A02A Vfd;
    public static SegApi SegApi;
    public static ConfigParser AppConfig;
    public static ShareMemoryAccessor Memory;
    public static IConfigurationRoot Config;
    public static MoneyBrand[] Brands;

    private static bool IsTerminating;

    static Program() {
        string gitHash = Assembly.Load(typeof(Program).Assembly.FullName)
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => attr.Key == "GitHash")?.Value;

        AssemblyName assemblyInfo = Assembly.GetExecutingAssembly().GetName();
        NAME = assemblyInfo.Name + "/" + assemblyInfo.Version + "-" + gitHash + " - Akechi Haruka";
    }

    private static int Main(string[] args) {
        try {
            return Parser.Default.ParseArguments
                    <Options>(args)
                .MapResult(Run, _ => 1);
        } catch (Exception ex) {
            if (Logging.Main != null) {
                Logging.Main.LogCritical(ex, "An error has occurred");
            } else {
                Console.WriteLine("An error has occurred");
                Console.WriteLine(ex);
            }

            return Int32.MinValue;
        }
    }

    private static int Run(Options options) {
        Config = Configuration.Initialize();
        Logging.Initialize(Config, options.Silent, true);
        Logging.Main.LogDebug("Command line: {c}", String.Join(' ', Environment.GetCommandLineArgs()));

        string keychip = options.KeychipId;
        int group = options.GroupId;
        int device = options.DeviceId;
        string broadcast = options.ApiBroadcast;
        int port = options.ApiPort;

        if (options.SegatoolsIniPath != null) {
            if (!File.Exists(options.SegatoolsIniPath)) {
                Logging.Main.LogError("segatools.ini could not be found at: {p}", options.SegatoolsIniPath);
                return 1;
            }

            IniFile ini = new IniFile(options.SegatoolsIniPath);

            keychip = ini.Read("id", "keychip");
            group = Int32.Parse(ini.Read("groupId", "api"));
            device = Int32.Parse(ini.Read("deviceId", "api"));
            broadcast = ini.Read("bindAddr", "api");
            port = Int32.Parse(ini.Read("port", "api"));
        } else {
            if (group == 0 || device == 0) {
                Logging.Main.LogError("If segatools.ini is not used, the -g and -d options must be present.");
                return 1;
            }
        }

        if (options.Server != null && keychip == null) {
            Logging.Main.LogError("If a server is specified, a keychip must also be specified, either with -k or from segatools.ini with -s.");
            return 1;
        }

        if (!File.Exists(options.EMoneyExecutable)) {
            Logging.Main.LogError("emoneyUI.exe could not be found at: {p}", options.EMoneyExecutable);
            return 1;
        }

        if (!File.Exists(options.AppConfig)) {
            Logging.Main.LogError("app.json could not be found at: {p}", options.AppConfig);
            return 1;
        }

        Logging.Main.LogInformation("Loading config from: {p}", options.AppConfig);
        AppConfig = JsonConvert.DeserializeObject<ConfigParser>(File.ReadAllText(options.AppConfig));
        Logging.Main.LogInformation("Game: {name} / {id}", AppConfig.titleName, AppConfig.subGameId);

        Memory = ShareMemoryAccessor.GetInstance();
        ShareMemoryAccessor.Result r = Memory.Create();
        if (r != ShareMemoryAccessor.Result.Ok) {
            Logging.Main.LogError("Memory initialization error: {r}", r);
            return 1;
        }

        Log.LogMessageWritten += LogOnLogMessageWritten;
        if (options.VfdPort > 0) {
            Logging.Main.LogInformation("Connecting VFD...");
            Vfd = new VFD_GP1232A02A(options.VfdPort);
            DeviceStatus ret = Vfd.Connect();
            if (ret != DeviceStatus.OK) {
                Logging.Main.LogError("Error connecting to VFD: {s}", ret);
                return 1;
            }

            Vfd.Reset();

            ret = Vfd.GetVersion(out string version);
            if (ret != DeviceStatus.OK) {
                Logging.Main.LogError("Error communicating with VFD: {s}", ret);
                return 1;
            }

            Vfd.SetEncoding(VFDEncoding.SHIFT_JIS);
            Vfd.SetOn(true);
            Vfd.SetBrightness(VFDBrightnessLevel.LEVEL2);
            Vfd.SetText("Please wait...", "");

            Logging.Main.LogInformation("VFD connected: {v}", version);
        }

        SegApi.OnLogMessage += SegApiOnOnLogMessage;
        SegApi = new SegApi((byte)group, (byte)device, broadcast, port);

        InitializeMemory(Memory);

        if (keychip != null && options.Server != null) {
            OpenMoney.Configure(options.Server, options.KeychipId);
        }

        Logging.Main.LogInformation("Initialized successfully");

        if (options.UIDelay > 0) {
            Logging.Main.LogInformation("Waiting {s} second(s)...", options.UIDelay);
            Thread.Sleep(options.UIDelay * 1000);
        }

        Logging.Main.LogDebug("Launching emoneyUI (args: {a})...", options.EMoneyArgs);
        Process.Start(new ProcessStartInfo(options.EMoneyExecutable) {
            UseShellExecute = true,
            Arguments = options.EMoneyArgs
        });

        Console.CancelKeyPress += ConsoleOnCancelKeyPress;

        ExMoney exmoney = new ExMoney(SegApi, Vfd, Brands, Memory, AppConfig, options);
        exmoney.Start();

        return 0;
    }

    private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e) {
        Logging.Main.LogInformation("Cancel key pressed!");
        if (!IsTerminating) {
            Memory.RequestExit = true;
            e.Cancel = true;
            IsTerminating = true;
        } else {
            Logging.Main.LogWarning("Force terminating!");
        }
    }

    private static void InitializeMemory(ShareMemoryAccessor mem) {
        UiSharedData data = mem.Data;
        data.Resource.EnableEmoney = AppConfig.emoney.enable;
        data.Resource.EntryDirection = (uint)AppConfig.ui.entry_icons.direction;
        data.Resource.EntryPosition = (uint)AppConfig.ui.entry_icons.position.position;
        data.Resource.EntryMarginX = (uint)AppConfig.ui.entry_icons.position.margin.x;
        data.Resource.EntryMarginY = (uint)AppConfig.ui.entry_icons.position.margin.y;
        data.Resource.MainPosition = (uint)AppConfig.ui.entry_icons.position.position;
        data.Resource.MainMarginX = (uint)AppConfig.ui.main_window.position.margin.x;
        data.Resource.MainMarginY = (uint)AppConfig.ui.main_window.position.margin.y;

        data.Daemon.DisplayBrands = new Brand[16];
        int count = 0;
        List<MoneyBrand> brands = new List<MoneyBrand>();
        foreach (IConfigurationSection value in Config.GetSection("Brands").GetChildren()) {
            MoneyBrand mb = new MoneyBrand(
                value.GetValue<uint>("Id"),
                value.GetValue<string>("Name"),
                value.GetValue<string>("Icon"),
                value.GetValue<string>("SoundStart"),
                value.GetValue<string>("SoundSuccess"),
                value.GetValue<string>("SoundError"),
                value.GetValue<bool>("Balance"),
                value.GetValue<bool?>("IsPaseli").GetValueOrDefault(false)
            );
            Brand b = new Brand() {
                Id = mb.ID,
                Filename = mb.Icon,
                EnableBalance = mb.HasBalance
            };
            if (AppConfig.emoney.paseli || !mb.IsPaseli) {
                data.Daemon.DisplayBrands[count++] = b;
            }

            brands.Add(mb);
        }

        data.Daemon.DisplayBrandCounts = (uint)count;
        Brands = brands.ToArray();

        data.Item.Items = new Item[5];
        for (int i = 0; i < AppConfig.emoney.credits.Length; i++) {
            int coins = AppConfig.emoney.credits[i];
            data.Item.Items[i].Enable = coins > 0;
            data.Item.Items[i].Name = coins > 0 ? coins + " CREDIT" + (coins > 1 ? "S" : "") : "---";
            data.Item.Items[i].Price = coins > 0 ? (uint)(coins * 100) : 99999;
        }

        data.Item.Counts = (uint)data.Item.Items.Length;
        data.GamePad.Enable = AppConfig.gamepad.enable;
        data.GamePad.MergeInput = AppConfig.gamepad.merge;
        data.GamePad.Sw = new ushort[8];

        mem.Data = data;
        mem.Update();
    }

    private static void LogOnLogMessageWritten(LogEntry obj) {
        switch (obj.Color) {
            case ConsoleColor.Red: Logging.SegaLib.LogError(obj.Message); break;
            case ConsoleColor.Yellow: Logging.SegaLib.LogWarning(obj.Message); break;
            default: Logging.SegaLib.LogInformation(obj.Message); break;
        }
    }

    private static void SegApiOnOnLogMessage(string obj) {
        Logging.SegApi.LogInformation(obj);
    }
}