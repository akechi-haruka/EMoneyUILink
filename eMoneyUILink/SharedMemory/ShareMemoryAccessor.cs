using eMoneyUILink;
using Haruka.Arcade.SEGA835Lib.Debugging;
using SharedMemory;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace Emoney.SharedMemory {
    public class ShareMemoryAccessor : IDisposable {
        private ShareMemoryAccessor() {
            this.structureSize = Marshal.SizeOf(typeof(UiSharedData));
            this.byteBuffer = new byte[this.structureSize];
            this.ptrBuffer = Marshal.AllocCoTaskMem(this.structureSize);
            Marshal.Copy(this.byteBuffer, 0, this.ptrBuffer, this.structureSize);
            Marshal.PtrToStructure(this.ptrBuffer, this.memory);
        }

        ~ShareMemoryAccessor() {
            Dispose();
        }

        public UiSharedData Data {
            get {
                return this.memory;
            }
            set {
                if (Write(value)) {
                    this.memory = value;
                }
            }
        }

        public bool RequestSound {
            set {
                Write(value, new Edit(EditRequestSound));
            }
        }

        public bool RequestOpen {
            set {
                Write(value, new Edit(EditRequestOpen));
            }
        }

        public bool Boot {
            set {
                Write(value, new Edit(EditUiBoot));
            }
        }

        public bool ShowGamePadPreviewWindow {
            set {
                Write(value, new Edit(EditUiShowGamePadWindow));
            }
        }

        public bool ShowMainWindow {
            set {
                Write(value, new Edit(EditUiShowWindow));
            }
        }

        private bool IsOpen {
            get {
                return this.mapped != null;
            }
        }

        public static ShareMemoryAccessor GetInstance() {
            return accessor;
        }

        public void Dispose() {
            Close();
            if (this.byteBuffer != null) {
                this.byteBuffer = null;
            }
            if (this.ptrBuffer != IntPtr.Zero) {
                Marshal.FreeCoTaskMem(this.ptrBuffer);
                this.ptrBuffer = IntPtr.Zero;
            }
        }

        public void Update() {
            if (this.IsOpen) {
                this.semaphore.WaitOne();

                Read(this.byteBuffer, 0, this.structureSize);
                Marshal.Copy(this.byteBuffer, 0, this.ptrBuffer, this.structureSize);
                Marshal.PtrToStructure(this.ptrBuffer, this.memory);

                this.semaphore.Release();
            }
        }

        private unsafe void Read(byte[] buf, int offset, int size) {
            using (MemoryMappedViewAccessor a = mapped.CreateViewAccessor(offset, size)) {
                byte* ptr = null;
                a.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                for (int i = 0; i < size; i++) {
                    buf[i] = ptr[i];
                }
                a.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }

        private unsafe void Write(byte[] buf, int offset, int size) {
            using (MemoryMappedViewAccessor a = mapped.CreateViewAccessor(offset, size)) {
                byte* ptr = null;
                a.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                for (int i = 0; i < size; i++) {
                    ptr[i] = buf[i];
                }
                a.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }

        public Result Create(UiSharedData data = null) {
            if (!this.IsOpen) {
                if (Exist()) {
                    return Result.ErrorAlready;
                }
                try {
                    this.semaphore = new Semaphore(0, 1, SemaphoreName);
                    this.semaphore.Release();
                } catch {
                    Close();
                    return Result.ErrorSemaphore;
                }
                try {
                    this.mapped = MemoryMappedFile.CreateNew(ShareMemoryName, (long)this.structureSize);
                    EMoneyUILink.LogMessage("Create success");
                } catch {
                    Close();
                    return Result.ErrorMemoryMap;
                }
                if (data == null) {
                    data = this.memory;
                }
                Write(data);
                return Result.Ok;
            }
            return Result.Ok;
        }

        public Result Open() {
            if (!this.IsOpen) {
                try {
                    this.semaphore = Semaphore.OpenExisting(SemaphoreName);
                } catch {
                    Close();
                    return Result.ErrorSemaphore;
                }
                try {
                    this.mapped = MemoryMappedFile.OpenExisting(ShareMemoryName);
                } catch {
                    Close();
                    return Result.ErrorMemoryMap;
                }
                Update();
                return Result.Ok;
            }
            return Result.Ok;
        }

        public void Close() {
            if (this.mapped != null) {
                this.mapped.Dispose();
                this.mapped = null;
            }
            if (this.semaphore != null) {
                this.semaphore = null;
            }
            if (this.byteBuffer != null) {
                for (int i = 0; i < this.byteBuffer.Length; i++) {
                    this.byteBuffer[i] = 0;
                }
                Marshal.Copy(this.byteBuffer, 0, this.ptrBuffer, this.structureSize);
                Marshal.PtrToStructure(this.ptrBuffer, this.memory);
            }
        }

        public void DropRequestExit() {
            Write(false, new Edit(DropRequestExit));
        }

        private bool Write(UiSharedData data) {
            if (!this.IsOpen) {
                EMoneyUILink.LogMessage("Write while file wasn't open");
                return false;
            }
            this.semaphore.WaitOne();

            Marshal.StructureToPtr(data, this.ptrBuffer, false);
            Marshal.Copy(this.ptrBuffer, this.byteBuffer, 0, this.structureSize);
            Write(this.byteBuffer, 0, this.structureSize);

            this.semaphore.Release();
            return true;
        }

        private bool Exist() {
            try {
                Semaphore.OpenExisting(SemaphoreName);
            } catch {
                return false;
            }
            return true;
        }

        private void Write(bool data, Edit edit) {
            if (this.IsOpen) {
                this.semaphore.WaitOne();

                Read(this.byteBuffer, 0, this.structureSize);
                Marshal.Copy(this.byteBuffer, 0, this.ptrBuffer, this.structureSize);
                Marshal.PtrToStructure(this.ptrBuffer, this.memory);
                this.memory = ((edit != null) ? edit(this.memory, data) : null);
                Marshal.StructureToPtr(this.memory, this.ptrBuffer, false);
                Marshal.Copy(this.ptrBuffer, this.byteBuffer, 0, this.structureSize);
                Write(this.byteBuffer, 0, this.structureSize);

                this.semaphore.Release();
            }
        }

        private UiSharedData EditRequestSound(UiSharedData current, bool value) {
            current.Request.Sound = value;
            return current;
        }

        private UiSharedData EditUiBoot(UiSharedData current, bool value) {
            current.Condition.Runnnig = value;
            return current;
        }

        private UiSharedData EditUiShowWindow(UiSharedData current, bool value) {
            current.Condition.DisplayingMain = value;
            return current;
        }

        private UiSharedData EditRequestOpen(UiSharedData current, bool value) {
            current.Request.OpenMain = value;
            return current;
        }

        private UiSharedData EditUiShowGamePadWindow(UiSharedData current, bool value) {
            current.Condition.DisplayingGamePad = value;
            return current;
        }

        private UiSharedData DropRequestExit(UiSharedData current, bool value) {
            current.Request.CloseUi = false;
            current.Condition.DisplayingMain = false;
            current.Condition.Runnnig = false;
            current.Condition.Closed = true;
            return current;
        }

        private static readonly string SemaphoreName = "ApmEmoneySemaphore";

        private static readonly string ShareMemoryName = "ApmEmoneyMemory";

        private static ShareMemoryAccessor accessor = new ShareMemoryAccessor();

        private MemoryMappedFile mapped;

        private Semaphore semaphore;

        private UiSharedData memory = new UiSharedData();

        private byte[] byteBuffer;

        private IntPtr ptrBuffer = IntPtr.Zero;

        private int structureSize;

        private delegate UiSharedData Edit(UiSharedData current, bool value);

        public enum Result {
            Ok,
            ErrorAlready,
            ErrorSemaphore,
            ErrorMemoryMap
        }

    }
}
