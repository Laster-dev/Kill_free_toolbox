using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Authorization
{
    internal class Weixin
    {
        public static bool Verification()
        {
#if DEBUG
            return true;
#else
            string targetString = string.Concat((char)((1 << 0) + (1 << 3) + (1 << 4) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 4) + (1 << 6)), (char)((1 << 2) + (1 << 4) + (1 << 6)), (char)((1 << 3) + (1 << 4) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 2) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 3) + (1 << 6)), (char)((1 << 1) + (1 << 3) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 3) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 2) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 2) + (1 << 5) + (1 << 6)), (char)((1 << 1) + (1 << 2) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 2) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 5) + (1 << 6)), (char)((1 << 1) + (1 << 2) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 3) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 2) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 2) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 1) + (1 << 2) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 2) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 1) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 5) + (1 << 6)), (char)((1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 2) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 1) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 2) + (1 << 5) + (1 << 6))); //YSTXGKJCKyushentianxiagongkaijichuke
            bool found = false;
            try
            {
                string ee = string.Concat((char)((1 << 0) + (1 << 1) + (1 << 2) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 2) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 3) + (1 << 4) + (1 << 5) + (1 << 6)), (char)((1 << 0) + (1 << 3) + (1 << 5) + (1 << 6)), (char)((1 << 1) + (1 << 2) + (1 << 3) + (1 << 5) + (1 << 6))); //weixin
                Process[] processes = Process.GetProcessesByName(ee);
                if (processes.Length == 0)
                {
                    return false;
                }
                foreach (Process process in processes)
                {
                    try
                    {
                        using (var scanner = new MemoryScanner(process))
                        {
                            if (scanner.Contains(targetString))
                            {
                                found = true;
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                return found;
            }
            catch (Exception)
            {
                return found;
            }
#endif
        }
    }
    public class MemoryScanner : IDisposable
    {
        #region Windows API
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            VirtualMemoryRead = 0x00000010,
            QueryInformation = 0x00000400
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READONLY = 0x02;
        const uint PAGE_READWRITE = 0x04;
        const uint PAGE_EXECUTE_READ = 0x20;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint PAGE_GUARD = 0x100;
        const uint PAGE_NOACCESS = 0x01;
        #endregion

        private IntPtr processHandle;
        private bool disposed = false;

        public MemoryScanner(Process targetProcess)
        {
            processHandle = OpenProcess(ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.QueryInformation, false, targetProcess.Id);

            if (processHandle == IntPtr.Zero)
            {
                throw null;
            }
        }
        public bool Contains(string searchText)
        {
            if (disposed) return false;

            byte[] searchBytes = Encoding.UTF8.GetBytes(searchText);

            try
            {
                IntPtr address = IntPtr.Zero;

                while (true)
                {
                    MEMORY_BASIC_INFORMATION mbi;
                    int result = VirtualQueryEx(processHandle, address, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));

                    if (result == 0) break;
                    if (mbi.State == MEM_COMMIT && IsReadableMemory(mbi.Protect))
                    {
                        if (ScanMemoryRegion(mbi.BaseAddress, (int)mbi.RegionSize, searchBytes))
                        {
                            return true;
                        }
                    }

                    address = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
                }
            }
            catch
            {
            }

            return false;
        }

        private bool IsReadableMemory(uint protect)
        {
            return (protect & PAGE_GUARD) == 0 &&
                   (protect & PAGE_NOACCESS) == 0 &&
                   ((protect & PAGE_READONLY) != 0 ||
                    (protect & PAGE_READWRITE) != 0 ||
                    (protect & PAGE_EXECUTE_READ) != 0 ||
                    (protect & PAGE_EXECUTE_READWRITE) != 0);
        }

        private bool ScanMemoryRegion(IntPtr baseAddress, int regionSize, byte[] searchBytes)
        {
            try
            {
                const int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];

                for (int offset = 0; offset < regionSize; offset += bufferSize)
                {
                    int bytesToRead = Math.Min(bufferSize, regionSize - offset);
                    IntPtr currentAddress = new IntPtr(baseAddress.ToInt64() + offset);

                    if (ReadProcessMemory(processHandle, currentAddress, buffer, bytesToRead, out IntPtr bytesRead))
                    {
                        int actualBytesRead = bytesRead.ToInt32();

                        for (int i = 0; i <= actualBytesRead - searchBytes.Length; i++)
                        {
                            bool found = true;
                            for (int j = 0; j < searchBytes.Length; j++)
                            {
                                if (buffer[i + j] != searchBytes[j])
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found) return true;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public void Dispose()
        {
            if (!disposed && processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
                disposed = true;
            }
        }
    }
}
