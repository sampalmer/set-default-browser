using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Temporarily switches the current thread's desktop.
    /// </summary>
    class DesktopContext : IDisposable
    {
        private readonly IntPtr originalDesktop;

        /// <summary>
        /// Switches to the <paramref name="newDesktopHandle"/> desktop.
        /// </summary>
        public DesktopContext(IntPtr newDesktopHandle)
        {
            originalDesktop = WindowsApi.GetThreadDesktop(WindowsApi.GetCurrentThreadId());
            if (originalDesktop == IntPtr.Zero)
                WindowsHelpers.CheckLastError();

            if (!WindowsApi.SetThreadDesktop(newDesktopHandle))
                WindowsHelpers.CheckLastError();
        }

        public void Dispose()
        {
            if (!WindowsApi.SetThreadDesktop(originalDesktop))
                WindowsHelpers.CheckLastError();
        }
    }
}
