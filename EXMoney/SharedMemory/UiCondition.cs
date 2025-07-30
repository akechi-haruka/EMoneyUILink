using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct UiCondition {
        [MarshalAs(UnmanagedType.U1)] public bool Runnnig;

        [MarshalAs(UnmanagedType.U1)] public bool DisplayingMain;

        [MarshalAs(UnmanagedType.U1)] public bool Closed;

        [MarshalAs(UnmanagedType.U1)] public bool DisplayingGamePad;
    }
}