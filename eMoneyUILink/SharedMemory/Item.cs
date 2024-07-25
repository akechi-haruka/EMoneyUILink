using System;
using System.Runtime.InteropServices;

namespace Emoney.SharedMemory
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
	public struct Item
	{
		[MarshalAs(UnmanagedType.U1)]
		public bool Enable;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
		public string Name;

		public uint Price;
	}
}
