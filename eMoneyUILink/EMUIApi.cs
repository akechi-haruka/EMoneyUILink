using Haruka.Arcade.SEGA835Lib.Devices.Card;
using Haruka.Arcade.SEGA835Lib.Devices.Card._837_15396;
using Haruka.Arcade.SEGA835Lib.Devices.Misc;
using OpenAimeIO_Managed.Core.Services;
using System;

namespace eMoneyUILink {
    public class EMUIApi {

        public static void SetCardReaderBlocked(bool b) {
            EMoneyUILink.LogMessage("SetCardReaderBlocked: " + b);
            EMoneyUILink.cardReaderBlocked = b;
            EMoneyUILink.Vfd?.ClearScreen();
            if (b) {
                EMoneyUILink.Vfd?.SetText("Scan your Aime, BanaPassport, FeliCa or mobile phone! ", "", true, false);
            } else {
                EMoney.SetVfdIdleText();
            }
            EMoneyUILink.Vfd?.SetTextDrawing(true);
        }
        public static void SetAlive(bool b) {
            EMoneyUILink.LogMessage("SetAlive: " + b);
            EMoneyUILink.alive = b;
        }

        public static CardReader Aime { 
            get { return EMoneyUILink.ReaderAdapter; } 
            set { EMoneyUILink.ReaderAdapter = value; }
        }

        public static VFD_GP1232A02A Vfd {
            get { return EMoneyUILink.Vfd; }
        }

        public static void AddCoinEvent(Action<int> onCoin) {
            EMoneyUILink.OnEMoneyCoins += onCoin;
        }

    }
}
