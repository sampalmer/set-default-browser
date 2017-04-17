using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var desktop = new Desktop("Default Browser Changer"))
                {
                    //TODO: Make browser name configurable.
                    //TODO: Detect if the browser isn't found.
                    var desktopProcess = new DesktopProcess(@"C:\Windows\system32\control.exe /name Microsoft.DefaultPrograms /page pageDefaultProgram\pageAdvancedSettings?pszAppName=google%20chrome", desktop.Name);
                    var exitCode = Wait(() => desktopProcess.GetExitCode()); //TODO: Replace this with WaitForSingleObject
                    if (exitCode != 1) //TODO: The fact that it's returning 1 instead of 0 could suggest something's wrong
                        throw new Exception("Control panel call returned " + exitCode);

                    using (new DesktopContext(desktop.Handle))
                    {
                        var window = Wait(() => WindowsApi.FindWindow(IntPtr.Zero, "Set Program Associations"));

                        try
                        {
                            var listViewHandle = Wait(() => FindDescendantBy(window, className: "SysListView32"));
                            var listView = new ListView(listViewHandle);

                            var save = Wait(() => FindDescendantBy(window, text: "Save"));

                            var checkboxIndices = Wait(() =>
                            {
                                var browserAssociations = new[] { ".htm", ".html", "HTTP", "HTTPS" };

                                var itemIndices =
                                (
                                    from item in listView.GetListItems().Select((text, index) => new { text, index })
                                    where browserAssociations.Contains(item.text, StringComparer.InvariantCultureIgnoreCase)
                                    select item.index
                                ).ToArray();

                                if (itemIndices.Length < browserAssociations.Length)
                                    return null;
                                else
                                    return itemIndices;
                            });

                            foreach (var i in checkboxIndices)
                                listView.CheckItem(i);

                            WindowsApi.SendMessage(save, WindowsApi.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                        }
                        finally
                        {
                            WindowsApi.SendMessage(window, WindowsApi.WM_CLOSE, IntPtr.Zero, IntPtr.Zero); //Just in case it doesn't close itself
                        }
                    }
                }
            }
            catch //Ensures all nested finally blocks execute
            {
                throw;
            }
        }

        public static IntPtr FindDescendantBy(IntPtr parent, string className = null, string text = null)
        {
            var matches = new List<IntPtr>();
            WindowsApi.EnumChildWindows(parent, (handle, pointer) =>
            {
                if (
                    (className == null || WindowsHelpers.GetWindowClassName(handle) == className)
                    &&
                    (text == null || WindowsHelpers.GetWindowTextRaw(handle) == text)
                )
                    matches.Add(handle);

                return true;
            }, IntPtr.Zero);

            if (matches.Count > 1)
                throw new Exception($"Found {matches.Count} matching descendants.");

            return matches.FirstOrDefault();
        }

        public static T Wait<T>(Func<T> action)
        {
            var stopwatch = Stopwatch.StartNew();

            do
            {
                var result = action();
                if (!Object.Equals(result, default(T)))
                    return result;

                Thread.Sleep(1);
            } while (stopwatch.Elapsed < TimeSpan.FromSeconds(5));

            throw new TimeoutException();
        }
    }
}
