using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Haruka.Arcade.SegatoolsAPI {
    public class SegatoolsAPI3 {

        public enum Packet : byte {
            Ping = 20,
            Ack = 21,
            Test = 22,
            Service = 23,
            Credit = 24,
            CardReadFelica = 25,
            CardReadAime = 26,
            PlaySequence = 28,
            VFDTextUTF = 29,
            VFDTextShiftJIS = 30,
            SetCardReaderState = 31,
            SetCardReaderBlocked = 32,
            SetCardReaderRGB = 33,
            ExitGame = 34,
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
        public event Action OnExitGame;
        public event Action<bool> OnConnectedChange;
        public event Action<byte[]> OnFelica;
        public event Action<byte[]> OnAime;
        public event Action<string> OnAuthorizationRequested;
        public event Action<bool> OnCardReaderBlocking;
        public static event Action<string> OnLogMessage;

        public SegatoolsAPI3(byte groupid, byte deviceid, String broadcast = "255.255.255.255", int port = 5364) {
            OnLogMessage?.Invoke("Created group " + groupid + ", device " + deviceid + " with " + broadcast + ":" + port);
            this.GroupId = groupid;
            this.DeviceId = deviceid;
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
            OnLogMessage?.Invoke("Starting device ID " + GroupId);
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
                        OnLogMessage?.Invoke("Received " + data.Length + " bytes from " + pt);
                        byte id = data[0];
                        byte grpid = data[1];
                        byte devid = data[2];
                        byte len = data[3];
                        if (grpid != GroupId) {
                            OnLogMessage?.Invoke("Not our group ID: " + grpid);
                            continue;
                        }
                        if (devid == DeviceId) {
                            OnLogMessage?.Invoke("Our own device ID, skipping");
                            continue;
                        }
                        byte[] inner = new byte[len];
                        Array.Copy(data, 4, inner, 0, len);
                        Handle((Packet)id, inner, pt);
                    }
                } catch (Exception ex) {
                    if (Running) {
                        OnLogMessage?.Invoke("Error while listening on " + BroadcastAddress + ":" + Port + " - " + ex);
                    }
                }
            }
            OnLogMessage?.Invoke("Stopped device ID " + GroupId);
            Connected = false;
            Running = false;
        }

        public void Stop() {
            Running = false;
            try {
                udp?.Close();
            } catch { }
        }

        private void Handle(Packet id, byte[] inner, IPEndPoint pt) {
            OnLogMessage?.Invoke("Received packet id " + id);
            if (!Connected) {
                Connected = true;
            }
            if (id == Packet.Ping) {
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.Ack) {
            } else if (id == Packet.Test) {
                OnTest?.Invoke();
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.Service) {
                OnService?.Invoke();
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.Credit) {
                OnCredit?.Invoke();
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.CardReadFelica) {
                OnFelica?.Invoke(inner);
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.CardReadAime) {
                OnAime?.Invoke(inner);
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.SetCardReaderBlocked) {
                OnCardReaderBlocking?.Invoke(inner[0] != 0);
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            } else if (id == Packet.ExitGame) {
                OnExitGame?.Invoke();
                Send(pt, Packet.Ack, new byte[] { (byte)id });
            }
        }

        private void Send(IPEndPoint pt, Packet id, byte[] data) {
            byte[] outdata = new byte[data.Length + 4];
            outdata[0] = (byte)id;
            outdata[1] = GroupId;
            outdata[2] = DeviceId;
            outdata[3] = (byte)data.Length;
            Array.Copy(data, 0, outdata, 4, data.Length);
            OnLogMessage?.Invoke("Sending packet " + id + " to " + pt);
            udp.Send(outdata, outdata.Length, pt);
        }

        internal void SendKillRequest() {
            throw new NotImplementedException();
        }

        public void SendSequenceStatus(SequenceStatus info) {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.PlaySequence, new byte[] { (byte)info });
        }

        public void SetVFDMessage(String str) {
            OnLogMessage?.Invoke("VFD: " + str);
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

        public void SendPing() {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.Ping, new byte[0]);
        }

        public void SendExitGame() {
            Send(new IPEndPoint(BroadcastAddress, Port), Packet.ExitGame, new byte[0]);
        }
    }
}
