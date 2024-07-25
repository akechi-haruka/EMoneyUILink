using OpenAimeIO_Managed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OAS.Segatools {
    public class SegatoolsAPI2 {

        public const String TAG = nameof(SegatoolsAPI2);

        public const int P_00_PING = 0;
        public const int P_01_ACK = 1;
        public const int P_02_TEST = 2;
        public const int P_03_SERVICE = 3;
        public const int P_04_CREDIT = 4;
        public const int P_05_FELICA = 5;
        public const int P_07_AIME = 7;
        public const int P_08_SEARCH = 8;
        public const int P_10_SEQUENCE = 10;
        public const byte SEQ_BEGIN = 0;
        public const byte SEQ_CONTINUE = 1;
        public const byte SEQ_END = 2;
        public const int P_11_VFD = 11;
        public const int P_12_PIN_AUTH = 12;
        public const int P_13_PIN_RESP = 13;
        public const int P_14_VFD_SHIFT_JIS = 14;
        public const int P_15_SET_CARD_READER = 15;
        public const int P_16_SET_CARD_READER_BLOCKING_STATE = 16;

        public byte GroupId { get; }
        public string BroadcastAddress { get; }
        public int RecvPort { get; }
        public int SendToPort { get; }
        public bool Running { get; private set; }
        private bool _connected;
        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                OnConnectedChange?.Invoke(value);
            }
        }
        private readonly UdpClient udp;

        public event Action OnTest;
        public event Action OnService;
        public event Action OnCredit;
        public event Action<bool> OnConnectedChange;
        public event Action<byte[]> OnFelica;
        public event Action<byte[]> OnAime;
        public event Action<string> OnAuthorizationRequested;
        public event Action<bool> OnCardReaderBlocking;
        private event Action<int, string> OnPin;

        public SegatoolsAPI2(byte groupid, String bcaddr, int recv_port = 5364, int send_to_port = 5365, String local_ep = "0.0.0.0") {
            Console.WriteLine("Created device ID " + groupid + " with " + bcaddr + ":" + recv_port);
            this.GroupId = groupid;
            this.BroadcastAddress = bcaddr;
            this.RecvPort = recv_port;
            this.SendToPort = send_to_port;
            udp = new UdpClient() {
                EnableBroadcast = true,
                ExclusiveAddressUse = false
            };
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Parse(local_ep), RecvPort));
        }

        public void Start() {
            if (Running) { return; }
            Console.WriteLine("Starting device ID " + GroupId);
            Running = true;
            new Thread(StartT) {
                Name = "Segatools API"
            }.Start();
        }

        private void StartT() {
            while (Running) {
                try {
                    IPEndPoint pt = new IPEndPoint(IPAddress.Any, RecvPort);
                    byte[] data = udp.Receive(ref pt);
                    if (data != null) {
                        Console.WriteLine("Received " + data.Length + " bytes from " + pt);
                        byte id = data[0];
                        byte len = data[1];
                        byte grpid = data[2];
                        if (grpid != GroupId) {
                            Console.WriteLine("Not our group ID: " + grpid);
                            continue;
                        }
                        byte[] inner = new byte[len];
                        Array.Copy(data, 3, inner, 0, len);
                        Handle(id, inner, pt);
                    }
                } catch (Exception ex) {
                    if (Running) {
                        Console.WriteLine("Error while listening on " + BroadcastAddress + ":" + RecvPort + " - " + ex);
                    }
                }
            }
            Console.WriteLine("Stopped device ID " + GroupId);
            Connected = false;
        }

        public void Stop() {
            Running = false;
            try {
                udp?.Close();
            } catch { }
        }

        public void SearchDevices() {
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_08_SEARCH, new byte[0]);
        }

        private void Handle(byte id, byte[] inner, IPEndPoint pt) {
            Console.WriteLine("Received packet id " + id);
            if (id == P_00_PING) {
                Connected = true;
                Send(pt, P_01_ACK, new byte[0]);
            } else if (id == P_02_TEST) {
                OnTest?.Invoke();
                Send(pt, P_01_ACK, new byte[0]);
            } else if (id == P_03_SERVICE) {
                OnService?.Invoke();
                Send(pt, P_01_ACK, new byte[0]);
            } else if (id == P_04_CREDIT) {
                OnCredit?.Invoke();
                Send(pt, P_01_ACK, new byte[0]);
            } else if (id == P_05_FELICA) {
                OnFelica?.Invoke(inner);
                Send(pt, P_01_ACK, new byte[0]);
            } else if (id == P_07_AIME) {
                OnAime?.Invoke(inner);
                Send(pt, P_01_ACK, new byte[0]);
            } else if (id == P_13_PIN_RESP) {
                OnPin?.Invoke(inner[0], Encoding.ASCII.GetString(inner, 1, inner.Length - 1));
                OnPin = null;
            } else if (id == P_16_SET_CARD_READER_BLOCKING_STATE) {
                OnCardReaderBlocking?.Invoke(inner[0] != 0);
                Send(pt, P_01_ACK, new byte[0]);
            }
        }

        private void Send(IPEndPoint pt, byte id, byte[] data) {
            byte[] outdata = new byte[data.Length + 3];
            outdata[0] = id;
            outdata[1] = (byte)data.Length;
            outdata[2] = GroupId;
            Array.Copy(data, 0, outdata, 3, data.Length);
            Console.WriteLine("Sending packet id " + id + " to " + pt);
            udp.Send(outdata, outdata.Length, pt);
        }

        internal void SendKillRequest() {
            throw new NotImplementedException();
        }

        public void SendSequenceStatus(byte info) {
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_10_SEQUENCE, new byte[] {info});
        }

        public void SetVFDMessage(String str) {
            Console.WriteLine("VFD: " + str);
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_11_VFD, Encoding.UTF8.GetBytes(str));
        }

        public void SendOpenMoneyAuthorization(Action<int, string> onPin, String message) {
            OnPin += onPin;
            byte[] msg = Encoding.UTF8.GetBytes(message);
            byte[] bytes = new byte[msg.Length + 1];
            bytes[0] = 1;
            Array.Copy(msg, 0, bytes, 1, msg.Length);
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_12_PIN_AUTH, bytes);
        }

        public void StopOpenMoneyAuthorization() {
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_12_PIN_AUTH, new byte[] { 0 });
        }

        public void SendCredit(uint count) {
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_04_CREDIT, new byte[] { (byte)(int)count });
        }

        public void SetCardReaderStatus(bool v) {
            Send(new IPEndPoint(IPAddress.Parse(BroadcastAddress), SendToPort), P_15_SET_CARD_READER, new byte[] { (byte)(v ? 1 : 0) });
        }
    }
}
