using Haruka.Arcade.EXMoney.Debugging;
using Haruka.Arcade.SEGA835Lib.Devices.Misc;
using Haruka.Arcade.SegAPI;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.EXMoney {
    public class PaymentProcess {
        private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(PaymentProcess));

        public delegate void EMoneySuccessCallback(int amount, int remaining);

        public class EMoneyResult {
            public DateTime Time;
            public uint Brand;
            public string DealNumber;
            public string CardNumber;
            public int Amount;
            public int BalanceBefore;
            public int BalanceAfter;
            public bool Success;
            public int Count;

            public EMoneyResult(bool success, uint brand) {
                Success = success;
                Brand = brand;
            }
        }

        public enum EMoneyResultStatus {
            Fail,
            Success,
            Unconfirm,
            Incomplete,
            Cancel
        }

        public EMoneyResult Result { get; private set; }
        public bool Busy { get; private set; }
        public bool IsCancellable { get; private set; } = true;
        public bool IsHeldOver { get; private set; }
        public bool IsError { get; private set; }
        public bool PlaySound { get; set; }
        public EMoneyResultStatus Status { get; private set; } = EMoneyResultStatus.Fail;

        private Thread executor;
        private VFD_GP1232A02A vfd;
        private SegApi api;
        private ExMoney exmoney;
        private byte[] lastScannedCard;

        public PaymentProcess(ExMoney exmoney, VFD_GP1232A02A vfd, SegApi api) {
            this.vfd = vfd;
            this.api = api;
            this.exmoney = exmoney;
        }

        private void EndOperation(bool isError, EMoneyResultStatus status, EMoneyResult result, string errorMessage) {
            LOG.LogInformation("Operation ended: {status} / {error}", status, errorMessage);
            IsError = isError;
            IsHeldOver = false;
            IsCancellable = true;
            Status = status;
            Result = result;
            Busy = false;

            api.SendCardReaderState(false);
            api.SendCardReaderLed(0, 0, 0);
            exmoney.SetVfdIdleText();
            executor?.Interrupt();

            executor = null;
        }

        public void Cancel() {
            LOG.LogInformation("Cancelling operation");
            EndOperation(false, EMoneyResultStatus.Cancel, null, "Cancelled");
        }

        public void RequestBalance(uint brandId) {
            IsError = false;
            Busy = true;
            IsCancellable = true;
            Result = null;
            executor = new Thread(() => PaymentRequestT(brandId, 0, 0, PaymentRequestType.Balance, "BALANCE", OnBalanceSuccess));
            executor.Start();
        }

        public void PayToCoin(uint brandId, string itemName, uint coin) {
            IsError = false;
            Busy = true;
            IsCancellable = true;
            Result = null;
            executor = new Thread(() => PaymentRequestT(brandId, 1, (int)coin, PaymentRequestType.PayToCoin, itemName, (amount, remaining) => OnPayCoinSuccess(amount, remaining, coin)));
            executor.Start();
        }

        public void PayAmount(uint brandId, string itemId, int amount, uint count) {
            Busy = true;
            IsCancellable = true;
            Result = null;
            executor = new Thread(() => PaymentRequestT(brandId, amount, (int)count, PaymentRequestType.PayAmount, itemId, (amountPaid, remaining) => OnPayAmountSuccess(amountPaid, remaining, count)));
            executor.Start();
        }

        private void PaymentRequestT(uint brandId, int amount, int count, PaymentRequestType requestType, string itemName, EMoneySuccessCallback onSucess) {
            try {
                PaymentResponse result;
                LOG.LogInformation("Request {r}", requestType);
                LOG.LogInformation("Wait for card");
                vfd?.SetText("Please touch card: ", brandId.ToString());
                api.SetCardReaderRGB(255, 255, 255);
                lastScannedCard = null;
                api.OnFelica += OnCard;
                api.OnAime += OnCard;
                api.SetCardReaderStatus(true);
                while (lastScannedCard == null) {
                    // TODO: what on card reader error?
                    Thread.Sleep(100);
                }

                string cardId = Convert.ToHexString(lastScannedCard);

                LOG.LogInformation("Card detected: {c}", cardId);

                IsCancellable = false;
                LOG.LogInformation("Processing...");
                vfd?.SetText("Processing...", "");
                try {
                    if (OpenMoney.IsConfigured()) {
                        result = OpenMoney.OpenMoneyRequest(cardId, brandId, amount, count, requestType, itemName);
                    } else {
                        result = new PaymentResponse() {
                            success = true,
                            balance_after = 1337
                        };
                    }

                    Thread.Sleep(1000);
                } catch (Exception ex) {
                    LOG.LogError(ex, "Request failed");
                    result = new PaymentResponse() {
                        success = false,
                        error = ex.Message
                    };
                }

                if (result.success) {
                    onSucess(amount, result.balance_after);
                } else {
                    vfd?.SetText("An error has occurred:", result.error, true);
                }

                api.SetCardReaderStatus(false);
                if (result.success) {
                    api.SetCardReaderRGB(0, 0, 255);
                } else {
                    api.SetCardReaderRGB(255, 0, 0);
                }

                Result = new EMoneyResult(result.success, brandId) {
                    Time = DateTime.Now,
                    Amount = amount,
                    BalanceAfter = result.balance_after,
                    BalanceBefore = result.balance_after + amount,
                    CardNumber = cardId,
                    DealNumber = "lolwut",
                    Count = count
                };
                if (requestType != PaymentRequestType.Balance) {
                    PlaySound = true;
                }

                Thread.Sleep(5000);
                EndOperation(!result.success, result.success ? EMoneyResultStatus.Success : EMoneyResultStatus.Incomplete, Result, result.error);
            } catch (ThreadInterruptedException) {
                LOG.LogInformation("Process cancelled");
            } finally {
                api.OnFelica -= OnCard;
                api.OnAime -= OnCard;
            }
        }

        private void OnCard(byte[] obj) {
            lastScannedCard = obj;
        }

        private void OnBalanceSuccess(int amount, int remaining) {
            vfd?.SetText("Balance: " + remaining, "");
        }

        private void OnPayCoinSuccess(int amount, int remaining, uint count) {
            exmoney.InvokeCoins(count);
            vfd?.SetText("Payment: " + amount, "Balance: " + remaining);
        }

        private void OnPayAmountSuccess(int amount, int remaining, uint count) {
            exmoney.InvokeCoins(count);
            vfd?.SetText("Payment: " + amount, "Balance: " + remaining);
        }
    }
}