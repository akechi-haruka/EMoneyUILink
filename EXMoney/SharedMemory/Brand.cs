using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct Brand {
        public uint Id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Filename;

        public bool EnableBalance;
    }
}