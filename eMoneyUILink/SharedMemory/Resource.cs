using System;
using System.Runtime.InteropServices;

namespace Emoney.SharedMemory
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
	public struct Resource
	{
		[MarshalAs(UnmanagedType.U1)]
		public bool EnableEmoney;

		public uint EntryDirection;

		public uint EntryPosition;

		public uint EntryMarginX;

		public uint EntryMarginY;

		public uint MainPosition;

		public uint MainMarginX;

		public uint MainMarginY;
	}
}
