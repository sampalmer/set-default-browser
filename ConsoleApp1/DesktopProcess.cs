using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class DesktopProcess : IDisposable
    {
        private readonly WindowsApi.PROCESS_INFORMATION processInformation;

        public DesktopProcess(string commandLine, string desktopName)
        {
            var startupInfo = new WindowsApi.STARTUPINFO
            {
                cb = Marshal.SizeOf<WindowsApi.STARTUPINFO>(),
                lpDesktop = desktopName,
            };

            if (!WindowsApi.CreateProcess(
                null,
                commandLine,
                null,
                null,
                true,
                0,
                IntPtr.Zero,
                null,
                ref startupInfo,
                out processInformation
            ))
                WindowsHelpers.CheckLastError();
        }

        public void Dispose()
        {
            if (!WindowsApi.CloseHandle(processInformation.hProcess))
                WindowsHelpers.CheckLastError();

            if (!WindowsApi.CloseHandle(processInformation.hThread))
                WindowsHelpers.CheckLastError();
        }

        public int? GetExitCode()
        {
            if (!WindowsApi.GetExitCodeProcess(processInformation.hProcess, out var exitCode))
                WindowsHelpers.CheckLastError();

            return exitCode == 259 /* STILL_ACTIVE */ ? null : (int?)exitCode;
        }
    }
}
