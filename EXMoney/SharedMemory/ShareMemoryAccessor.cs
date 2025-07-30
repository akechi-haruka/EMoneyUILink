using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Haruka.Arcade.EXMoney.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.EXMoney.SharedMemory {
    public class ShareMemoryAccessor : IDisposable {
        private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(ShareMemoryAccessor));

        private ShareMemoryAccessor() {
            structureSize = Marshal.SizeOf(typeof(UiSharedData));
            byteBuffer = new byte[structureSize];
            ptrBuffer = Marshal.AllocCoTaskMem(structureSize);
            Marshal.Copy(byteBuffer, 0, ptrBuffer, structureSize);
            Marshal.PtrToStructure(ptrBuffer, memory);
            LOG.LogTrace("Created memory struct of size {s}", structureSize);
        }

        ~ShareMemoryAccessor() {
            Dispose();
        }

        public UiSharedData Data {
            get { return memory; }
            set {
                if (Write(value)) {
                    memory = value;
                }
            }
        }

        public bool RequestSound {
            set { Write(value, EditRequestSound); }
        }

        public bool RequestOpen {
            set { Write(value, EditRequestOpen); }
        }

        public bool Boot {
            set { Write(value, EditUiBoot); }
        }

        public bool ShowGamePadPreviewWindow {
            set { Write(value, EditUiShowGamePadWindow); }
        }

        public bool ShowMainWindow {
            set { Write(value, EditUiShowWindow); }
        }

        private bool IsOpen {
            get { return mapped != null; }
        }

        public static ShareMemoryAccessor GetInstance() {
            return accessor;
        }

        public void Dispose() {
            Close();
            byteBuffer = null;
            if (ptrBuffer != IntPtr.Zero) {
                Marshal.FreeCoTaskMem(ptrBuffer);
                ptrBuffer = IntPtr.Zero;
            }
        }

        public void Update() {
            if (IsOpen) {
                semaphore.WaitOne();

                Read(byteBuffer, 0, structureSize);
                Marshal.Copy(byteBuffer, 0, ptrBuffer, structureSize);
                Marshal.PtrToStructure(ptrBuffer, memory);

                semaphore.Release();
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
            if (!IsOpen) {
                if (Exist()) {
                    LOG.LogError("Shared memory is already in use. Is another instance running?");
                    return Result.ErrorAlready;
                }

                try {
                    LOG.LogTrace("Creating semaphore");
                    semaphore = new Semaphore(0, 1, SEMAPHORE_NAME);
                    semaphore.Release();
                } catch (Exception ex) {
                    LOG.LogError("Failed to create semaphore: {ex}", ex);
                    Close();
                    return Result.ErrorSemaphore;
                }

                try {
                    mapped = MemoryMappedFile.CreateNew(SHARE_MEMORY_NAME, structureSize);
                    LOG.LogDebug("Shared memory created successfully");
                } catch (Exception ex) {
                    LOG.LogError("Failed to create shared memory: {ex}", ex);
                    Close();
                    return Result.ErrorMemoryMap;
                }

                if (data == null) {
                    data = memory;
                }

                Write(data);
            }

            return Result.Ok;
        }

        public void Close() {
            if (mapped != null) {
                mapped.Dispose();
                mapped = null;
            }

            semaphore = null;
            if (byteBuffer != null) {
                for (int i = 0; i < byteBuffer.Length; i++) {
                    byteBuffer[i] = 0;
                }

                Marshal.Copy(byteBuffer, 0, ptrBuffer, structureSize);
                Marshal.PtrToStructure(ptrBuffer, memory);
            }
        }

        public void DropRequestExit() {
            Write(false, DropRequestExit);
        }

        private bool Write(UiSharedData data) {
            if (!IsOpen) {
                LOG.LogWarning("Attempted write while memory wasn't open");
                return false;
            }

            semaphore.WaitOne();

            Marshal.StructureToPtr(data, ptrBuffer, false);
            Marshal.Copy(ptrBuffer, byteBuffer, 0, structureSize);
            Write(byteBuffer, 0, structureSize);

            semaphore.Release();
            return true;
        }

        private bool Exist() {
            try {
                Semaphore.OpenExisting(SEMAPHORE_NAME);
            } catch {
                return false;
            }

            return true;
        }

        private void Write(bool data, Edit edit) {
            if (IsOpen) {
                semaphore.WaitOne();

                Read(byteBuffer, 0, structureSize);
                Marshal.Copy(byteBuffer, 0, ptrBuffer, structureSize);
                Marshal.PtrToStructure(ptrBuffer, memory);
                memory = edit != null ? edit(memory, data) : null;
                Marshal.StructureToPtr(memory, ptrBuffer, false);
                Marshal.Copy(ptrBuffer, byteBuffer, 0, structureSize);
                Write(byteBuffer, 0, structureSize);

                semaphore.Release();
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

        private static readonly string SEMAPHORE_NAME = "ApmEmoneySemaphore";

        private static readonly string SHARE_MEMORY_NAME = "ApmEmoneyMemory";

        private static readonly ShareMemoryAccessor accessor = new ShareMemoryAccessor();

        private MemoryMappedFile mapped;

        private Semaphore semaphore;

        private UiSharedData memory = new UiSharedData();

        private byte[] byteBuffer;

        private IntPtr ptrBuffer;

        private readonly int structureSize;

        private delegate UiSharedData Edit(UiSharedData current, bool value);

        public enum Result {
            Ok,
            ErrorAlready,
            ErrorSemaphore,
            ErrorMemoryMap
        }
    }
}