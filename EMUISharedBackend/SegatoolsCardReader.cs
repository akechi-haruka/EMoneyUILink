using Haruka.Arcade.SEGA835Lib.Devices;
using Haruka.Arcade.SEGA835Lib.Devices.Card;
using Haruka.Arcade.SEGA835Lib.Serial;
using OAS.Segatools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMUISharedBackend {
    internal class SegatoolsCardReader : CardReader {

        public SegatoolsAPI2 Segatools { get; private set; }

        private byte group;
        private byte device;
        private String bcaddr;
        private int port;
        private bool polling;

        private byte[] card;
        private CardType? cardType;

        public SegatoolsCardReader(byte group, byte device, string bcaddr, int port) {
            this.group = group;
            this.device = device;
            this.bcaddr = bcaddr;
            this.port = port;
        }

        public override DeviceStatus Connect() {
            Segatools = new SegatoolsAPI2(group, device, bcaddr, port);
            Segatools.OnAime += Segatools_OnAime;
            Segatools.OnFelica += Segatools_OnFelica;
            Segatools.Start();
            return DeviceStatus.OK;
        }

        private void Segatools_OnFelica(byte[] obj) {
            card = obj;
            cardType = CardType.FeliCa;
        }

        private void Segatools_OnAime(byte[] obj) {
            card = obj;
            cardType = CardType.MIFARE;
        }

        public override DeviceStatus Disconnect() {
            Segatools?.Stop();
            cardType = null;
            card = null;
            return DeviceStatus.OK;
        }

        public override CardType? GetCardType() {
            return cardType;
        }

        public override byte[] GetCardUID() {
            return card;
        }

        public override string GetDeviceModel() {
            return "Segatools API";
        }

        public override string GetName() {
            return "Segatools API";
        }

        public override bool HasDetectedCard() {
            return cardType != null;
        }

        public override bool IsPolling() {
            return polling;
        }

        public override DeviceStatus Read(out SProtFrame recv) {
            recv = null;
            return DeviceStatus.OK;
        }

        public override DeviceStatus StartPolling() {
            ClearCard();
            Segatools.SetCardReaderStatus(true);
            polling = true;
            return DeviceStatus.OK;
        }

        public override DeviceStatus StopPolling() {
            Segatools.SetCardReaderStatus(false);
            polling = false;
            return DeviceStatus.OK;
        }

        public override DeviceStatus Write(SProtFrame send) {
            return DeviceStatus.OK;
        }

        public override void ClearCard() {
            cardType = null;
            card = null;
        }

        public override DeviceStatus LEDSetColor(byte red, byte green, byte blue) {
            // todo
            return DeviceStatus.OK;
        }

        public override DeviceStatus LEDReset() {
            // todo
            return DeviceStatus.OK;
        }
    }
}
