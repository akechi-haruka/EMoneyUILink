using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct GamePadData {
        [MarshalAs(UnmanagedType.U1)] public bool Enable;

        [MarshalAs(UnmanagedType.U1)] public bool MergeInput;

        [MarshalAs(UnmanagedType.U1)] public bool UsingAnalogStick;

        [MarshalAs(UnmanagedType.U1)] public byte AnalogThreshold;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public ushort[] Sw;
    }
}