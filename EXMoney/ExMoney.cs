using Haruka.Arcade.EXMoney.Debugging;
using Haruka.Arcade.EXMoney.GameConfig;
using Haruka.Arcade.EXMoney.SharedMemory;
using Haruka.Arcade.SEGA835Lib.Devices.Misc;
using Haruka.Arcade.SegAPI;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.EXMoney;

public class ExMoney {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(ExMoney));

    private readonly SegApi api;
    private readonly ShareMemoryAccessor memory;
    private readonly ConfigParser appConfig;
    private readonly VFD_GP1232A02A vfd;
    private readonly MoneyBrand[] brands;
    private readonly Options options;

    private bool alive;
    private bool readerBlocked;
    private PaymentProcess payment;

    public ExMoney(SegApi api, VFD_GP1232A02A vfd, MoneyBrand[] brands, ShareMemoryAccessor memory, ConfigParser appConfig, Options options) {
        this.vfd = vfd;
        this.brands = brands;
        this.api = api;
        this.memory = memory;
        this.appConfig = appConfig;
        this.options = options;
    }

    public void Start() {
        api.OnCardReaderBlocking += ApiOnOnCardReaderBlocking;
        api.OnConnectedChange += ApiOnOnConnectedChange;
        api.Start();
        api.SendPing();
        new Thread(MainLoopT) {
            Name = "ExMoney Main"
        }.Start();
    }

    private void ApiOnOnConnectedChange(bool obj) {
        if (!alive && obj) {
            SetVfdIdleText();
        }

        alive = obj;
        LOG.LogInformation("Connection status has changed to {r}", obj);
    }

    private void ApiOnOnCardReaderBlocking(bool b) {
        if (readerBlocked != b) {
            LOG.LogDebug("SetCardReaderBlocked: {b}", b);
            readerBlocked = b;
            vfd?.ClearScreen();
            if (b) {
                vfd?.SetText("Scan your Aime, BanaPassport, FeliCa or mobile phone! ", "", true);
            } else {
                SetVfdIdleText();
            }

            if (b) {
                payment?.Cancel();
            }

            vfd?.SetTextDrawing(true);
        }
    }

    public void SetVfdIdleText() {
        vfd?.SetText("E-Money Payment OK", String.Join(", ", brands.Select(b => b.Name)), false, true);
    }

    private void MainLoopT() {
        LOG.LogInformation("Process started");
        while (true) {
            memory.Update();
            UiSharedData data = memory.Data;

            data.Daemon.ServiseAlive = alive;
            data.Daemon.CanOperateDeal = !readerBlocked;
            data.Daemon.IsCancellable = payment == null || payment.IsCancellable;
            data.Daemon.IsBusy = payment != null && payment.Busy;

            if (data.Request.Cancel) {
                payment?.Cancel();
                data.Request.Cancel = false;
                data.Request.RequestDone = true;
            } else if (data.Request.RequestBalance) {
                payment = new PaymentProcess(this, vfd, api, brands);
                payment.RequestBalance(data.Request.BrandId);
                data.Request.RequestBalance = false;

                string sound = brands.First(mb => mb.ID == data.Request.BrandId).SoundOnScan;
                if (sound != null) {
                    data.Request.Sound = true;
                    data.Request.SoundData.Filename = sound;
                }
            } else if (data.Request.RequestPayToCoin) {
                payment = new PaymentProcess(this, vfd, api, brands);
                payment.PayToCoin(data.Request.BrandId, options.ItemName ?? data.Request.Price.ToString(), data.Request.Price / 100);
                data.Request.RequestPayToCoin = false;

                string sound = brands.First(mb => mb.ID == data.Request.BrandId).SoundOnScan;
                if (sound != null) {
                    data.Request.Sound = true;
                    data.Request.SoundData.Filename = sound;
                }
            }

            if ((payment?.PlaySound ?? false) && appConfig.emoney.sound) {
                bool success = payment.Result?.Success ?? false;
                MoneyBrand mb = brands.FirstOrDefault(mb => mb.ID == payment.Result?.Brand);
                if (mb != null) {
                    string sound = success ? mb.SoundOnSuccess : mb.SoundOnFailure;
                    if (sound != null) {
                        data.Request.Sound = true;
                        data.Request.SoundData.Filename = sound;
                    }
                }

                payment.PlaySound = false;
            }

            if (payment?.Result != null) {
                data.Request.RequestDone = true;
            }

            if (data.Condition.Closed) {
                break;
            }

            memory.Data = data;

            Thread.Sleep(500);
        }
    }

    public void InvokeCoins(uint count) {
        LOG.LogInformation("Sending {c} credit(s)", count);
        api.SendCredit(count);
    }
}