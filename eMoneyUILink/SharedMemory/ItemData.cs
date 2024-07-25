using System;
using System.Runtime.InteropServices;

namespace Emoney.SharedMemory
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
	public struct ItemData
	{
		public uint Counts;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
		public Item[] Items;
	}
}
