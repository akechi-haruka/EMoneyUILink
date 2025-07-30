using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct Sound {
        [MarshalAs(UnmanagedType.U1)] public bool IsStopRequired;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Filename;
    }
}