using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct Request {
        [MarshalAs(UnmanagedType.U1)] public bool Cancel;

        [MarshalAs(UnmanagedType.U1)] public bool RequestBalance;

        [MarshalAs(UnmanagedType.U1)] public bool RequestPayToCoin;

        public uint BrandId;

        public uint Price;

        [MarshalAs(UnmanagedType.U1)] public bool Sound;

        public Sound SoundData;

        [MarshalAs(UnmanagedType.U1)] public bool OpenMain;

        [MarshalAs(UnmanagedType.U1)] public bool CloseUi;

        [MarshalAs(UnmanagedType.U1)] public bool RequestDone;
    }
}