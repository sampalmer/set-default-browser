using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetDefaultBrowser
{
    static class DefaultBrowserChanger
    {
        /// <exception cref="EnvironmentException">
        /// When there is a problem with either <paramref name="browserName"/> or the hosting environment.
        /// </exception>
        public static void Set(string browserName)
        {
            //This is needed since the control panel applet can crash if you give it an empty browser name
            if (String.IsNullOrWhiteSpace(browserName))
                throw new EnvironmentException($"The given browser name was blank.");

            using (var desktop = new Desktop("Default Browser Changer"))
            {
                var encodedBrowserName = Uri.EscapeDataString(browserName);
                var desktopProcess = new DesktopProcess(@"control.exe /name Microsoft.DefaultPrograms /page pageDefaultProgram\pageAdvancedSettings?pszAppName=" + encodedBrowserName, desktop.Name);
                var exitCode = Wait(() => desktopProcess.GetExitCode()); //TODO: Replace this with WaitForSingleObject
                if (exitCode != 1) //Control.exe always returns 1 regardless of whether it had valid arguments.
                    throw new Exception("control.exe returned " + exitCode);

                using (new DesktopContext(desktop.Handle))
                {
                    IntPtr window;
                    try
                    {
                        window = Wait(() => WindowsApi.FindWindow(IntPtr.Zero, "Set Program Associations"));
                    }
                    catch (TimeoutException timeout)
                    {
                        throw new EnvironmentException("The control panel applet didn't open. Try logging out and then logging in again.", timeout);
                    }

                    try
                    {
                        var listViewHandle = Wait(() => FindDescendantBy(window, className: "SysListView32"));
                        var listView = new ListView(listViewHandle);
                        var save = Wait(() => FindDescendantBy(window, text: "Save"));


                        var browserAssociations = new[] { ".htm", ".html", "HTTP", "HTTPS" };

                        int[] checkboxIndices;
                        try
                        {
                            checkboxIndices = Wait(() =>
                            {
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
                        }
                        catch (TimeoutException timeout)
                        {
                            throw new EnvironmentException($"Didn't find all of the following extensions and protocols: {String.Join(", ", browserAssociations)}. Please ensure '{browserName}' is:\n1. Spelled correctly\n2. Installed\n3. A browser", timeout);
                        }

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

        private static IntPtr FindDescendantBy(IntPtr parent, string className = null, string text = null)
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

        private static T Wait<T>(Func<T> action)
        {
            var timeout = TimeSpan.FromSeconds(5);

            var stopwatch = Stopwatch.StartNew();

            do
            {
                var result = action();
                if (!Object.Equals(result, default(T)))
                    return result;

                Thread.Sleep(1);
            } while (stopwatch.Elapsed < timeout);

            throw new TimeoutException($"Operation took longer than {timeout}.");
        }
    }
}
