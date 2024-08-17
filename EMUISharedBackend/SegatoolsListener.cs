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

        public enum Packet : byte {
            Ping = 20,
            Ack = 21,
            Test = 22,
            Service = 23,
            Credit = 24,
            CardReadFelica = 25,
            CardReadAime = 26,
            DeviceSearch = 27,
            PlaySequence = 28,
            VFDTextUTF = 29,
            VFDTextShiftJIS = 30,
            SetCardReaderState = 31,
            SetCardReaderBlocked = 32,
            SetCardReaderRGB = 33
        }

        public enum SequenceStatus : byte {
            Start = 0,
            Continue = 1,
            End = 2
        }

        public byte GroupId { get; }
        public byte DeviceId { get; }
        public IPAddress BroadcastAddress { get; }
        public int Port { get; }
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
        private Thread thread;

        public event Action OnTest;
        public event Action OnService;
        public event Action OnCredit;
        public event Action<bool> OnConnectedChange;
        public event Action<byte[]> OnFelica;
        public event Action<byte[]> OnAime;
        public event Action<string> OnAuthorizationRequested;
        public event Action<bool> OnCardReaderBlocking;

        public SegatoolsAPI2(byte groupid, byte devid, String broadcast = "255.255.255.255", int port = 5364) {
            Console.WriteLine("Created group " + groupid + ", device "+devid+" with " + broadcast + ":" + port);
            this.GroupId = groupid;
            this.DeviceId = devid;
            this.BroadcastAddress = IPAddress.Parse(broadcast);
            this.Port = port;
            udp = new UdpClient() {
                EnableBroadcast = true,
                ExclusiveAddressUse = false
            };
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
        }

        public void Start() {
            if (Running) { return; }
            Console.WriteLine("Starting device ID " + GroupId);
            Running = true;
            thread = new Thread(StartT) {
                Name = "Segatools API"
            };
            thread.Start();
        }

        private void StartT() {
            while (Running) {
                try {
                    IPEndPoint pt = new IPEndPoint(IPAddress.Any, Port);
                    byte[] data = udp.Receive(ref pt);
                    if (data != null) {
                        Console.WriteLine("Received " + data.Length + " bytes from " + pt);
                        byte id = data[0];
                        byte grpid = data[1];
                        byte devid = data[2];
                        byte len = data[3];
                        if (grpid != GroupId) {
                            Console.WriteLine("Not our group ID: " + grpid);
                            continue;
                        }
                        if (devid != DeviceId) {
                            Console.WriteLine("Our own device ID, skipping");
                            continue;
                        }
                        byte[] inner = new byte[len];
                        Array.Copy(data, 4, inner, 0, len);
                        Handle((Packet)id, inner, pt);
                    }
                } catch (Exception ex) {
                    if (Running) {
                        Console.WriteLine("Error while listening on " + BroadcastAddress + ":" + Port + " - " + ex);
                    }
                }
            }
            Console.WriteLine("Stopped device ID " + GroupId);
            Connected = false;
            Running = false;
        }

        public void Stop() {
            Running = false;
            try {
                udp?.Close();
            } catch { }
        }

        public void SearchDevices() {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.DeviceSearch, new byte[0]);
        }

        private void Handle(Packet id, byte[] inner, IPEndPoint pt) {
            Console.WriteLine("Received packet id " + id);
            if (id == Packet.Ping) {
                Connected = true;
                Send(pt, Packet.Ack, new byte[0]);
            } else if (id == Packet.Test) {
                OnTest?.Invoke();
                Send(pt, Packet.Ack, new byte[0]);
            } else if (id == Packet.Service) {
                OnService?.Invoke();
                Send(pt, Packet.Ack, new byte[0]);
            } else if (id == Packet.Credit) {
                OnCredit?.Invoke();
                Send(pt, Packet.Ack, new byte[0]);
            } else if (id == Packet.CardReadFelica) {
                OnFelica?.Invoke(inner);
                Send(pt, Packet.Ack, new byte[0]);
            } else if (id == Packet.CardReadAime) {
                OnAime?.Invoke(inner);
                Send(pt, Packet.Ack, new byte[0]);
            } else if (id == Packet.SetCardReaderBlocked) {
                OnCardReaderBlocking?.Invoke(inner[0] != 0);
                Send(pt, Packet.Ack, new byte[0]);
            }
        }

        private void Send(IPEndPoint pt, Packet id, byte[] data) {
            byte[] outdata = new byte[data.Length + 3];
            outdata[0] = (byte)id;
            outdata[1] = GroupId;
            outdata[2] = DeviceId;
            outdata[3] = (byte)data.Length;
            Array.Copy(data, 0, outdata, 3, data.Length);
            Console.WriteLine("Sending packet " + id + " to " + pt);
            udp.Send(outdata, outdata.Length, pt);
        }

        internal void SendKillRequest() {
            throw new NotImplementedException();
        }

        public void SendSequenceStatus(SequenceStatus info) {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.PlaySequence, new byte[] {(byte)info});
        }

        public void SetVFDMessage(String str) {
            Console.WriteLine("VFD: " + str);
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.VFDTextUTF, Encoding.UTF8.GetBytes(str));
        }

        public void SendCredit(uint count) {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.Credit, new byte[] { (byte)(int)count });
        }

        public void SetCardReaderStatus(bool v) {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.SetCardReaderState, new byte[] { (byte)(v ? 1 : 0) });
        }

        public void SetCardReaderRGB(byte r, byte g, byte b) {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.SetCardReaderRGB, new byte[] { r, g, b });
        }

    }
}
