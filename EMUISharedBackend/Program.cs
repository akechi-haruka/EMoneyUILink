using Emoney.SharedMemory;
using eMoneyUILink;
using EMUISharedBackend.GameConfig;
using Haruka.Arcade.SEGA835Lib.Devices;
using OpenAimeIO_Managed;
using System;
using System.Threading;
using static Emoney.SharedMemory.ShareMemoryAccessor;

namespace EMUISharedBackend {
    internal class Program {

        private static SegatoolsCardReader card;

        static void Main(string[] args) {

            if (args.Length < 9) {
                Console.WriteLine("Usage: EMUISharedBackend <app.json> <vfd_port/0> <emoneyui_exe> <openmoney_addr> <keychip> <segatools_group> <segatools_broadcast> <segatools_port_in> <segatools_port_out>");
                return;
            }

            Console.WriteLine(@"----------------------------------------
OpenAimeEMUI 0.2
2024 Haruka
----------------------------------------");

            Console.WriteLine("Args: " + String.Join(",", args));

            card = new SegatoolsCardReader(Byte.Parse(args[5]), args[6], Int32.Parse(args[7]), Int32.Parse(args[8]));
            DeviceStatus ret = card.Connect();
            if (ret != DeviceStatus.OK) {
                Console.WriteLine("API error: " + ret);
                return;
            }
            card.Segatools.OnConnectedChange += Segatools_OnConnectedChange;
            card.Segatools.OnCardReaderBlocking += Segatools_OnCardReaderBlocking;
            int time = 10;
            while (!card.Segatools.Connected) {
                card.Segatools.SearchDevices();
                Thread.Sleep(1000);
                if (time-- < 0) {
                    break;
                }
            }

            EMUIApi.AddCoinEvent(CoinEvent);
            EMUIApi.Aime = card;

            EMoneyUILink.Initialize(args[0], Int32.Parse(args[1]), args[2], args[3], args[4], log_callback);
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
            EMUIApi.SetCardReaderBlocked(obj);
        }

    }
}
