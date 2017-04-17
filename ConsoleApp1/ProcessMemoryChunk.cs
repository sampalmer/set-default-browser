using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    /// <summary>
    /// A chunk in another processes memory. Mostly used to allocate buffers
    /// in another process for sending messages to its windows.
    /// </summary>
    public class ProcessMemoryChunk : IDisposable
    {
        readonly Process process;
        readonly IntPtr location, hProcess;
        readonly int size;
        readonly bool free;

        private ProcessMemoryChunk(Process process, IntPtr hProcess, IntPtr location, int size, bool free)
        {
            this.process = process;
            this.hProcess = hProcess;
            this.location = location;
            this.size = size;
            this.free = free;
        }

        /// <summary>
        /// The location in memory (of the other process) this chunk refers to.
        /// </summary>
        public IntPtr Location { get { return location; } }

        /// <summary>
        /// Allocate a chunk in another process.
        /// </summary>
        public static ProcessMemoryChunk Alloc(Process process, int size)
        {
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMRead | ProcessAccessFlags.VMWrite, false, process.Id);
            IntPtr remotePointer = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)size,
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remotePointer == IntPtr.Zero)
                throw new Exception("Couldn't allocate memory");

            return new ProcessMemoryChunk(process, hProcess, remotePointer, size, true);
        }

        /// <summary>
        /// Allocate a chunk in another process and unmarshal a struct
        /// there.
        /// </summary>
        public static ProcessMemoryChunk AllocStruct(Process process, object structure)
        {
            int size = Marshal.SizeOf(structure);
            ProcessMemoryChunk result = Alloc(process, size);
            result.WriteStructure(0, structure);
            return result;
        }

        /// <summary>
        /// Free the memory in the other process, if it has been allocated before.
        /// </summary>
        public void Dispose()
        {
            if (free)
            {
                if (!VirtualFreeEx(hProcess, location, UIntPtr.Zero, MEM_RELEASE))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            CloseHandle(hProcess);
        }

        /// <summary>
        /// Write a structure into this chunk.
        /// </summary>
        public void WriteStructure(int offset, object structure)
        {
            int size = Marshal.SizeOf(structure);
            IntPtr localPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, localPtr, false);
                Write(offset, localPtr, size);
            }
            finally
            {
                Marshal.FreeHGlobal(localPtr);
            }
        }

        /// <summary>
        /// Write into this chunk.
        /// </summary>
        public void Write(int offset, IntPtr ptr, int length)
        {
            if (offset < 0) throw new ArgumentException("Offset may not be negative", "offset");
            if (offset + length > size) throw new ArgumentException("Exceeding chunk size");
            WriteProcessMemory(hProcess, new IntPtr(location.ToInt64() + offset), ptr, new UIntPtr((uint)length), IntPtr.Zero);
        }

        /// <summary>
        /// Read this chunk.
        /// </summary>
        /// <returns></returns>
        public byte[] Read() { return Read(0, size); }

        /// <summary>
        /// Read a part of this chunk.
        /// </summary>
        public byte[] Read(int offset, int length)
        {
            if (offset + length > size) throw new ArgumentException("Exceeding chunk size");
            byte[] result = new byte[length];
            ReadProcessMemory(hProcess, new IntPtr(location.ToInt64() + offset), result, new UIntPtr((uint)length), IntPtr.Zero);
            return result;
        }

        /// <summary>
        /// Read a part of this chunk to a pointer in this process.
        /// </summary>
        public void ReadToPtr(int offset, int length, IntPtr ptr)
        {
            if (offset + length > size) throw new ArgumentException("Exceeding chunk size");
            ReadProcessMemory(hProcess, new IntPtr(location.ToInt64() + offset), ptr, new UIntPtr((uint)length), IntPtr.Zero);
        }

        /// <summary>
        /// Read a part of this chunk to a structure.
        /// </summary>
        public object ReadToStructure(int offset, Type structureType)
        {
            int size = Marshal.SizeOf(structureType);
            IntPtr localPtr = Marshal.AllocHGlobal(size);
            try
            {
                ReadToPtr(offset, size, localPtr);
                return Marshal.PtrToStructure(localPtr, structureType);
            }
            finally
            {
                Marshal.FreeHGlobal(localPtr);
            }
        }

        #region PInvoke Declarations

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CloseHandle(IntPtr hObject);

        private static readonly uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000,
            MEM_RELEASE = 0x8000, PAGE_READWRITE = 0x04;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
            UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            IntPtr lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            IntPtr lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        #endregion
    }
}
