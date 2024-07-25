using eMoneyUILink;
using Haruka.Arcade.SEGA835Lib.Devices.Card;
using Haruka.Arcade.SEGA835Lib.Devices.Card._837_15396;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenAimeIO_Managed.Core.Services {
    public class EMoney {

        public delegate void EMoneySuccessCallback(int amount, int remaining);

        public enum EMoneyBrandEnum {
            Nanaco = 1, Edy = 2, iD = 3, Transport = 4, Waon = 5, Paseli = 6, Sapica = 7, Max = 7
        }

        public struct EMoneyResult {
            public DateTime time;
            public EMoneyBrandEnum brand;
            public String dealNumber;
            public String cardNumber;
            public int amount;
            public int balanceBefore;
            public int balanceAfter;
            public bool success;
            public int count;

            public EMoneyResult(object _) {
                time = DateTime.Now;
                brand = (EMoneyBrandEnum)(-1);
                dealNumber = null;
                cardNumber = null;
                amount = 0;
                balanceAfter = 0;
                balanceBefore = 0;
                success = false;
                count = 0;
            }

            public EMoneyResult(bool success, EMoneyBrandEnum brand) : this() {
                this.success = success;
                this.brand = brand;
            }

            public string GetSoundEffectForBrandAndResult() {
                switch (brand) {
                    case EMoneyBrandEnum.Nanaco: return success ? "0001-02-01" : "0001-02-02";
                    case EMoneyBrandEnum.Sapica: return success ? "0001-02-02" : "0001-02-02";
                    case EMoneyBrandEnum.Transport: return success ? "0005-02-00" : "0005-02-01";
                    case EMoneyBrandEnum.iD: return success ? "0003-02-00" : "0003-02-01";
                    case EMoneyBrandEnum.Edy: return success ? "0002-02-00" : "0002-02-01";
                    case EMoneyBrandEnum.Paseli: return success ? "0008-02-00" : "0008-02-01";
                    case EMoneyBrandEnum.Waon: return success ? "0006-02-00" : "0006-02-01";
                    default: return "9999-02-99";
                }
            }
        }

        public enum EMoneyResultStatus {
            Fail,
            Success,
            Unconfirm,
            Incomplete,
            Cancel
        }

        public static EMoneyResult? Result { get; private set; } = null;
        public static bool Busy { get; private set; } = false;
        public static bool IsCancellable { get; private set; } = true;
        public static bool IsHeldOver { get; private set; }
        public static bool IsError { get; private set; }
        public static bool PlaySound { get; set; }
        public static EMoneyResultStatus Status { get; private set; } = EMoneyResultStatus.Fail;

        private static DateTime LastOperationFinish = DateTime.Now;

        private static Thread Executor;

        public static void EndOperation(bool error, EMoneyResultStatus status, EMoneyResult? result, String error_message) {
            EMoneyUILink.LogMessage("EMoney: Operation ended: " + status);
            IsError = error;
            IsHeldOver = false;
            IsCancellable = true;
            Status = status;
            Result = result;
            Busy = false;
            if (!error) {
                LastOperationFinish = DateTime.Now;
            }
            EMoneyUILink.ReaderAdapter?.StopPolling();
            EMoneyUILink.ReaderAdapter?.LEDSetColor(0, 0, 0);
            EMoneyUILink.ReaderAdapter?.ClearCard();
            SetVfdIdleText();
            try {
                Executor?.Interrupt();
            } catch { }
            Executor = null;
        }

        public static void Cancel() {
            EMoneyUILink.LogMessage("EMoney: Cancelling operation");
            EndOperation(false, EMoneyResultStatus.Cancel, null, "Cancelled");
        }

        public static void RequestBalance(EMoneyBrandEnum brandId) {
            if (CheckTooFastRequest()) {
                return;
            }
            IsError = false;
            Busy = true;
            IsCancellable = true;
            Result = null;
            Executor = new Thread(() => EMoneyRequestT(brandId, 0, 0, PaymentRequestType.Balance, "BALANCE", OnBalanceSuccess));
            Executor.Start();
        }

        public static void PayToCoin(EMoneyBrandEnum brandId, string itemName, uint coin) {
            if (CheckTooFastRequest()) {
                return;
            }
            IsError = false;
            Busy = true;
            IsCancellable = true;
            Result = null;
            Executor = new Thread(() => EMoneyRequestT(brandId, 1, (int)coin, PaymentRequestType.PayToCoin, itemName, (amount, remaining) => OnPayCoinSuccess(amount, remaining, coin)));
            Executor.Start();
        }

        public static void PayAmount(EMoneyBrandEnum brandId, string itemId, int amount, uint count) {
            if (CheckTooFastRequest()) {
                return;
            }
            Busy = true;
            IsCancellable = true;
            Result = null;
            Executor = new Thread(() => EMoneyRequestT(brandId, amount, (int)count, PaymentRequestType.PayAmount, itemId, (amount_, remaining) => OnPayAmountSuccess(amount_, remaining, count)));
            Executor.Start();
        }

        private static bool CheckTooFastRequest() {
            if (DateTime.Now - LastOperationFinish < TimeSpan.FromSeconds(1)) {
                return true;
            }
            return false;
        }

        private static void EMoneyRequestT(EMoneyBrandEnum brandId, int amount, int count, PaymentRequestType requestType, String itemName, EMoneySuccessCallback onSucess) {
            try {
                PaymentResponse result;
                EMoneyUILink.LogMessage("EMoney: Request " + requestType);

                CardReader aime = EMoneyUILink.ReaderAdapter;

                EMoneyUILink.LogMessage("EMoney: Wait for card");
                EMoneyUILink.Vfd?.SetText("Please touch card: ", brandId.ToString());
                aime.LEDReset();
                aime.LEDSetColor(255, 255, 255);
                aime.StartPolling();
                while (!aime.HasDetectedCard()) {
                    if (!aime.IsPolling()) {
                        EndOperation(true, EMoneyResultStatus.Fail, new EMoneyResult(false, brandId), "Card reader error");
                        return;
                    }
                    Thread.Sleep(100);
                }
                EMoneyUILink.LogMessage("EMoney: Card detected: " + aime.GetCardUIDAsString());

                IsCancellable = false;
                EMoneyUILink.LogMessage("EMoney: Processing...");
                EMoneyUILink.Vfd?.SetText("Processing...", "");
                try {
                    result = OpenMoney.OpenMoneyRequest(aime.GetCardUIDAsString(), brandId, amount, count, requestType, itemName);
                    Thread.Sleep(1000);
                } catch (Exception ex) {
                    EMoneyUILink.LogMessage("Request failed: " + ex);
                    EndOperation(true, EMoneyResultStatus.Unconfirm, new EMoneyResult(false, brandId), "Failed to contact server");
                    return;
                }
                if (result.success) {
                    onSucess(amount, result.balance_after);
                } else {
                    EMoneyUILink.Vfd?.SetText("An error has occurred:", result.error, true);
                }
                aime.StopPolling();
                if (result.success) {
                    aime.LEDSetColor(0, 0, 255);
                } else {
                    aime.LEDSetColor(255, 0, 0);
                }
                Result = new EMoneyResult() {
                    time = DateTime.Now,
                    amount = amount,
                    balanceAfter = result.balance_after,
                    balanceBefore = result.balance_after + amount,
                    brand = (EMoneyBrandEnum)(int)brandId,
                    cardNumber = aime.GetCardUIDAsString(),
                    dealNumber = "lolwut",
                    success = result.success,
                    count = count
                };
                if (requestType != PaymentRequestType.Balance) {
                    PlaySound = true;
                }
                Thread.Sleep(5000);
                EndOperation(!result.success, result.success ? EMoneyResultStatus.Success : EMoneyResultStatus.Incomplete, Result, result.error);
            } catch (ThreadInterruptedException) {
                EMoneyUILink.LogMessage("EMoney: Process cancelled");
            }
        }

        private static void OnBalanceSuccess(int amount, int remaining) {
            EMoneyUILink.Vfd.SetText("Balance: " + remaining, "");
        }

        private static void OnPayCoinSuccess(int amount, int remaining, uint count) {
            EMoneyUILink.InvokeCoins((int)count);
            EMoneyUILink.Vfd?.SetText("Payment: " + amount, "Balance: " + remaining);
        }

        private static void OnPayAmountSuccess(int amount, int remaining, uint count) {
            EMoneyUILink.InvokeCoins((int)count);
            EMoneyUILink.Vfd?.SetText("Payment: " + amount, "Balance: " + remaining);
        }

        internal static void SetVfdIdleText() {
            StringBuilder brands = new StringBuilder();
            for (int i = 1; i < (int)EMoney.EMoneyBrandEnum.Max; i++) {
                brands.Append((EMoney.EMoneyBrandEnum)i);
                brands.Append(", ");
            }
            EMoneyUILink.Vfd?.SetText("E-Money Payment OK", brands.ToString(), false, true);
        }
    }
}
