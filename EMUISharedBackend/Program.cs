using eMoneyUILink;
using Haruka.Arcade.SEGA835Lib.Devices;
using System;
using Haruka.Arcade.SegatoolsAPI;

namespace EMUISharedBackend {
    internal class Program {

        private static SegatoolsCardReader card;

        static void Main(string[] args) {

            if (args.Length < 10) {
                Console.WriteLine("Usage: EMUISharedBackend <app.json> <vfd_port/0> <emoneyui_exe> <openmoney_addr> <keychip> <segatools_group> <segatools_device> <segatools_broadcast> <segatools_port> <item_name>");
                return;
            }

            SegatoolsAPI3.OnLogMessage += SegatoolsAPI2_OnLogMessage;

            Console.WriteLine(@"----------------------------------------
EMUISharedBackend 0.3.1
2024-2025 Haruka
----------------------------------------");

            Console.WriteLine("Args: " + String.Join(",", args));

            card = new SegatoolsCardReader(Byte.Parse(args[5]), Byte.Parse(args[6]), args[7], Int32.Parse(args[8]));
            DeviceStatus ret = card.Connect();
            if (ret != DeviceStatus.OK) {
                Console.WriteLine("API error: " + ret);
                return;
            }
            card.Segatools.OnConnectedChange += Segatools_OnConnectedChange;
            card.Segatools.OnCardReaderBlocking += Segatools_OnCardReaderBlocking;

            EMUIApi.AddCoinEvent(CoinEvent);
            EMUIApi.Aime = card;

            EMoneyUILink.Initialize(args[0], Int32.Parse(args[1]), args[2], args[3], args[4], log_callback, args[9]);
        }

        private static void SegatoolsAPI2_OnLogMessage(string obj) {
            Console.WriteLine(obj);
        }

        private static void Segatools_OnConnectedChange(bool obj) {
            EMUIApi.SetAlive(obj);
        }

        private static void CoinEvent(int obj) {
            card.Segatools.SendCredit((uint)obj);
        }

        private static void log_callback(string obj) {
            Console.WriteLine(obj);
        }

        private static void Segatools_OnCardReaderBlocking(bool obj) {
            EMUIApi.SetAlive(true);
            EMUIApi.SetCardReaderBlocked(obj);
        }

    }
}
