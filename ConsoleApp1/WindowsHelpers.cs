using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class WindowsHelpers
    {
        public static string GetWindowTextRaw(IntPtr hwnd)
        {
            var length = (int)WindowsApi.SendMessage(hwnd, WindowsApi.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            var sb = new StringBuilder(length + 1);
            WindowsApi.SendMessage(hwnd, WindowsApi.WM_GETTEXT, (IntPtr)sb.Capacity, sb);
            return sb.ToString();
        }

        public static string GetWindowClassName(IntPtr hwnd)
        {
            var result = new StringBuilder(256);
            var charactersCopied = WindowsApi.GetClassName(hwnd, result, result.Capacity);
            if (charactersCopied == 0)
                CheckLastError();
            return result.ToString();
        }

        public static void CheckLastError()
        {
            var lastError = Marshal.GetLastWin32Error();
            if (lastError != 0)
                throw new Win32Exception(lastError);
        }
    }
}
