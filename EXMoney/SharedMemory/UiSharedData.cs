using System.Runtime.InteropServices;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public class UiSharedData : ICloneable {
        public UiSharedData() {
        }

        protected UiSharedData(UiSharedData other) {
            Resource = other.Resource;
            Condition = other.Condition;
            Daemon = other.Daemon;
            Request = other.Request;
            Item = other.Item;
            GamePad = other.GamePad;
        }

        public virtual object Clone() {
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