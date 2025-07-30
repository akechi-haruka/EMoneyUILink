using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct Item {
        [MarshalAs(UnmanagedType.U1)] public bool Enable;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;

        public uint Price;
    }
}