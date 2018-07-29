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
        /// <param name="browser"></param>
        /// <exception cref="EnvironmentException">
        /// When there is a problem with either <paramref name="browser"/> or the hosting environment.
        /// </exception>
        public static void Set(Browser browser)
        {
            // In Windows 10 version 1803, the old control-panel-style Default Programs screen has been replaced with a new one inside the Windows 10 settings app.
            // However, the Windows 10 settings app cannot be automated without the user seeing it or interacting with it, so that option is unreliable.
            // Fortunately, someone else has written a better alternative to this app (http://kolbi.cz/blog/?p=396).
            // So for these reasons, this app won't support this or later versions of Windows.
            if (OsInfo.Windows10Version >= 1803)
                throw new EnvironmentException($"This app no longer works in this version of Windows.");

            // This is needed since the control panel applet can crash if you give it an empty browser name
            if (String.IsNullOrWhiteSpace(browser.UniqueApplicationName))
                throw new EnvironmentException($"The given browser's unique application name is blank.");

            using (var desktop = new Desktop("Default Browser Changer"))
            {
                var encodedBrowserName = Uri.EscapeDataString(browser.UniqueApplicationName);
                var desktopProcess = new DesktopProcess(@"control.exe /name Microsoft.DefaultPrograms /page pageDefaultProgram\pageAdvancedSettings?pszAppName=" + encodedBrowserName, desktop.Name);
                var exitCode = Wait(() => desktopProcess.GetExitCode()); //TODO: Replace this with WaitForSingleObject
                if (exitCode != 1) //Control.exe always returns 1 regardless of whether it had valid arguments.
                    throw new Exception("control.exe returned " + exitCode);

                using (new DesktopContext(desktop.Handle))
                {
                    IntPtr window;
                    try
                    {
                        // The window is located by class name rather than caption since the caption is locale-dependent.
                        window = Wait(() => WindowsApi.FindWindow("CabinetWClass" /* Windows Explorer */, IntPtr.Zero));
                    }
                    catch (TimeoutException timeout)
                    {
                        throw new EnvironmentException("The control panel applet didn't open. Try logging out and then logging in again.", timeout);
                    }

                    try
                    {
                        var listViewHandle = Wait(() =>
                        {
                            var matches = FindDescendantsBy(window, className: "SysListView32");
                            var expectedListBoxCount = 1;
                            if (matches.Count > expectedListBoxCount)
                                throw new Exception($"Found {matches.Count} list box(es), but expected {expectedListBoxCount}.");

                            return matches.FirstOrDefault();
                        });

                        var listView = new ListView(listViewHandle);

                        var save = Wait(() =>
                        {
                            var matches = FindDescendantsBy(window, className: "Button");
                            if (matches.Count == 0)
                                return default(IntPtr);

                            var expectedButtonCount = 3; // Select All, Save, Cancel
                            if (matches.Count != expectedButtonCount)
                                throw new Exception($"Found {matches} button(s), but expected {expectedButtonCount}.");

                            return matches[1];
                        });

                        var browserAssociations = browser.Associations
                            .Intersect(new[] { ".htm", ".html", "HTTP", "HTTPS" }, StringComparer.OrdinalIgnoreCase)
                            .ToArray();

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
                            throw new EnvironmentException($"Didn't find all of the following extensions and protocols: {String.Join(", ", browserAssociations)}.", timeout);
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

        private static List<IntPtr> FindDescendantsBy(IntPtr parent, string className = null, string text = null)
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

            return matches;
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
