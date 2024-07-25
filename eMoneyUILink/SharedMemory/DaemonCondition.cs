using System;
using System.Runtime.InteropServices;

namespace Emoney.SharedMemory
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
	public struct DaemonCondition
	{
		[MarshalAs(UnmanagedType.U1)]
		public bool ServiseAlive;

		[MarshalAs(UnmanagedType.U1)]
		public bool CanOperateDeal;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsBusy;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsCancellable;

		public uint DisplayBrandCounts;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public Brand[] DisplayBrands;
	}
}
