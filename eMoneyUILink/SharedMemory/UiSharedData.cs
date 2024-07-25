using System;
using System.Runtime.InteropServices;

namespace Emoney.SharedMemory
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
	public class UiSharedData : ICloneable
	{
		public UiSharedData()
		{
		}

		protected UiSharedData(UiSharedData other)
		{
			this.Resource = other.Resource;
			this.Condition = other.Condition;
			this.Daemon = other.Daemon;
			this.Request = other.Request;
			this.Item = other.Item;
			this.GamePad = other.GamePad;
		}

		public virtual object Clone()
		{
			return new UiSharedData(this);
		}

		public Resource Resource;

		public UiCondition Condition;

		public DaemonCondition Daemon;

		public Request Request;

		public ItemData Item;

		public GamePadData GamePad;
	}
}
