using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SendKeys
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            WindowHook.sendKeystroke((ushort)Keys.A);
        }

        class WindowHook
        {
            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll")]
            public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            [DllImport("user32")]
            public static extern bool SetForegroundWindow(IntPtr hwnd);

            public static void sendKeystroke(ushort k)
            {
                const uint WM_KEYDOWN = 0x100;
                const uint WM_KEYUP = 0x101;
                const uint WM_SYSCOMMAND = 0x018;
                const uint SC_CLOSE = 0x053;

                IntPtr WindowToFind = FindWindow(null, "new 4 - Notepad++");
                SetForegroundWindow(WindowToFind);

                SendMessage(WindowToFind, WM_KEYDOWN, ((IntPtr)k), (IntPtr)0);
                SendMessage(WindowToFind, WM_KEYUP, ((IntPtr)k), (IntPtr)0);
            }
        }
    }
}
