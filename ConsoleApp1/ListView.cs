using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class ListView
    {
        private readonly IntPtr handle;
        private readonly Process process;

        public ListView(IntPtr handle)
        {
            this.handle = handle;
            process = GetWindowProcess(handle);
        }

        private static Process GetWindowProcess(IntPtr window)
        {
            WindowsApi.GetWindowThreadProcessId(window, out var processId);
            return Process.GetProcessById((int)processId);
        }

        public IEnumerable<string> GetListItems()
        {
            var listViewItemCount = (int)WindowsApi.SendMessage(handle, WindowsApi.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);

            for (var i = 0; i < listViewItemCount; ++i)
            {
                var lvi = new WindowsApi.LVITEM
                {
                    cchTextMax = 512,
                    iItem = i,
                    iSubItem = 0,
                    stateMask = 0xffffffff,
                    mask = WindowsApi.LVIF_STATE | WindowsApi.LVIF_TEXT,
                };

                using (var textBuffer = ProcessMemoryChunk.Alloc(process, lvi.cchTextMax * 2 /* Assuming 2-byte character size just in case */))
                {
                    lvi.pszText = textBuffer.Location;

                    using (var structBuffer = ProcessMemoryChunk.AllocStruct(process, lvi))
                    {
                        if (WindowsApi.SendMessage(handle, WindowsApi.LVM_GETITEM, IntPtr.Zero, structBuffer.Location) == IntPtr.Zero)
                            throw new Exception("Couldn't get list item");

                        lvi = (WindowsApi.LVITEM)structBuffer.ReadToStructure(0, typeof(WindowsApi.LVITEM));
                    }

                    //TODO: Check if pszText pointer location changed

                    var titleBytes = textBuffer.Read();
                    var title = Encoding.Default.GetString(titleBytes);
                    if (title.IndexOf('\0') != -1) title = title.Substring(0, title.IndexOf('\0'));

                    yield return title;
                }
            }
        }

        public void CheckItem(int itemIndex)
        {
            var lvi = new WindowsApi.LVITEM
            {
                iItem = itemIndex,
                iSubItem = 0,
                stateMask = 8192, //TODO: Replace these with the proper constants
                state = 8192,
            };

            using (var structBuffer = ProcessMemoryChunk.AllocStruct(process, lvi))
            {
                if (WindowsApi.SendMessage(handle, WindowsApi.LVM_SETITEMSTATE, (IntPtr)itemIndex, structBuffer.Location) == IntPtr.Zero)
                    throw new Exception("Couldn't set list item state");
            }
        }
    }
}
