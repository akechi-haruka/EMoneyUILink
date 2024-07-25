using Emoney.SharedMemory;
using Haruka.Arcade.SEGA835Lib.Debugging;
using Haruka.Arcade.SEGA835Lib.Devices.Misc;
using Haruka.Arcade.SEGA835Lib.Devices;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Haruka.Arcade.SEGA835Lib.Devices.Card;
using EMUISharedBackend.GameConfig;
using Newtonsoft.Json;
using OpenAimeIO_Managed.Core.Services;
using static UnityEngine.Application;

namespace eMoneyUILink {
    public class EMoneyUILink {

        private static ShareMemoryAccessor mem;
        private static UiSharedData data;
        private static Action<string> log_callback;

        internal static CardReader ReaderAdapter;
        internal static VFD_GP1232A02A Vfd;
        internal static event Action<int> OnEMoneyCoins;
        internal static bool cardReaderBlocked;
        internal static bool alive;

        internal static String keychip_id;
        internal static String openmoney_url;

        public static void Initialize(string config_path, int vfd_port, string emoneyui_exe, string openmoney_addr, string keychip, Action<string> log_callback_func) {
            log_callback = log_callback_func;
            openmoney_url = openmoney_addr;
            keychip_id = keychip;

            log_callback("Loading config...");
            ConfigParser config = JsonConvert.DeserializeObject<ConfigParser>(File.ReadAllText(config_path));
            log_callback("Game: " + config.titleName + " / " + config.subGameId);

            if (vfd_port > 0) {
                log_callback("Connecting VFD...");
                Vfd = new VFD_GP1232A02A(vfd_port);
                DeviceStatus ret = Vfd.Connect();
                if (ret == DeviceStatus.OK) {
                    log_callback("VFD Connected");
                    Vfd.Reset();
                    Vfd.SetEncoding(VFDEncoding.SHIFT_JIS);
                    Vfd.SetOn(true);
                    Vfd.SetBrightness(VFDBrightnessLevel.LEVEL2);
                    Vfd.SetText("Please wait...", "");
                } else {
                    log_callback("VFD failed to connect: " + ret);
                    Vfd = null;
                }
            }

            log_callback("Creating Memory...");
            mem = ShareMemoryAccessor.GetInstance();
            ShareMemoryAccessor.Result r = mem.Create();
            if (r != ShareMemoryAccessor.Result.Ok) {
                log_callback("Memory initialization error: " + r);
                return;
            }

            data = mem.Data;
            data.Resource.EnableEmoney = config.emoney.enable;
            data.Resource.EntryDirection = (uint)config.ui.entry_icons.direction;
            data.Resource.EntryPosition = (uint)config.ui.entry_icons.position.position;
            data.Resource.EntryMarginX = (uint)config.ui.entry_icons.position.margin.x;
            data.Resource.EntryMarginY = (uint)config.ui.entry_icons.position.margin.y;
            data.Resource.MainPosition = (uint)config.ui.entry_icons.position.position;
            data.Resource.MainMarginX = (uint)config.ui.main_window.position.margin.x;
            data.Resource.MainMarginY = (uint)config.ui.main_window.position.margin.y;

            data.Daemon.DisplayBrands = new Brand[16];
            data.Daemon.DisplayBrands[0] = new Brand() {
                Id = 1U,
                Filename = "0001-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrands[1] = new Brand() {
                Id = 2U,
                Filename = "0002-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrands[2] = new Brand() {
                Id = 3U,
                Filename = "0003-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrands[3] = new Brand() {
                Id = 4U,
                Filename = "0005-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrands[4] = new Brand() {
                Id = 5U,
                Filename = "0006-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrands[5] = new Brand() {
                Id = 6U,
                Filename = "0008-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrands[6] = new Brand() {
                Id = 7U,
                Filename = "0009-01-00",
                EnableBalance = true
            };
            data.Daemon.DisplayBrandCounts = (uint)data.Daemon.DisplayBrands.Length;

            data.Item.Items = new Item[5];
            for (int i = 0; i < config.emoney.credits.Length; i++) {
                int coins = config.emoney.credits[i];
                data.Item.Items[i].Enable = true;
                data.Item.Items[i].Name = coins + " CREDIT" + (coins > 1 ? "S" : "");
                data.Item.Items[i].Price = (uint)(coins * 100);
            }
            data.Item.Counts = (uint)data.Item.Items.Length;
            data.GamePad.Enable = config.gamepad.enable;
            data.GamePad.MergeInput = config.gamepad.merge;
            data.GamePad.Sw = new ushort[8];

            mem.Data = data;

            log_callback("Initialized");

            new Thread(MainLoop).Start();

            string cwd = Directory.GetParent(emoneyui_exe).FullName;
            log_callback("exe: " + emoneyui_exe + ", cwd: " + cwd);
            Process.Start(new ProcessStartInfo(emoneyui_exe) {
                UseShellExecute = true,
                WorkingDirectory = cwd
            });

        }

        private static void MainLoop() {
            while (true) {
                mem.Update();
                data = mem.Data;

                data.Daemon.ServiseAlive = alive;
                data.Daemon.CanOperateDeal = !cardReaderBlocked;
                data.Daemon.IsCancellable = EMoney.IsCancellable;
                data.Daemon.IsBusy = EMoney.Busy;

                if (data.Request.Cancel) {
                    EMoney.Cancel();
                    data.Request.Cancel = false;
                    data.Request.RequestDone = true;
                } else if (data.Request.RequestBalance) {
                    EMoney.RequestBalance((EMoney.EMoneyBrandEnum)data.Request.BrandId);
                    data.Request.RequestBalance = false;
                } else if (data.Request.RequestPayToCoin) {
                    EMoney.PayAmount((EMoney.EMoneyBrandEnum)data.Request.BrandId, data.Request.Price.ToString(), (int)data.Request.Price, data.Request.Price / 100);
                    data.Request.RequestPayToCoin = false;
                }

                if (EMoney.PlaySound) {
                    data.Request.Sound = true;
                    data.Request.SoundData.Filename = EMoney.Result?.GetSoundEffectForBrandAndResult();
                    EMoney.PlaySound = false;
                }

                if (EMoney.Result != null) {
                    data.Request.RequestDone = true;
                }

                mem.Data = data;

                Thread.Sleep(500);
            }
        }

        internal static void InvokeCoins(int count) {
            OnEMoneyCoins?.Invoke(count);
        }

        internal static void LogMessage(string str) {
            if (log_callback != null) {
                log_callback(str);
            }
        }
    }
}