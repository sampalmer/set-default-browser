using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Desktop : IDisposable
    {
        public string Name { get; }
        public IntPtr Handle { get; }

        public Desktop(string baseName)
        {
            Name = baseName + " " + DateTime.Now.Ticks.ToString(); //Ensuring this is unique since sometimes a name can't be reused later on

            Handle = WindowsApi.CreateDesktop(Name, null, null, 0, WindowsApi.ACCESS_MASK.DESKTOP_CREATEWINDOW, null);
            if (Handle == IntPtr.Zero)
                WindowsHelpers.CheckLastError();
        }

        public void Dispose()
        {
            if (!WindowsApi.CloseDesktop(Handle))
                WindowsHelpers.CheckLastError();
        }
    }
}
