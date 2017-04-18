using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SetDefaultBrowser
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    throw new EnvironmentException($"Sets the default browser\n\nUsage: {Path.GetFileName(Assembly.GetExecutingAssembly().Location)} browsername\nbrowsername: The name of the browser as shown in Windows' \"Set Default Programs\" screen, such as \"Google Chrome\" or \"Firefox\".");

                Go(args[0]);
            }
            catch (EnvironmentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch //Ensures all nested "finally" blocks execute
            {
                throw;
            }

            return 0;
        }

        private static void Go(string browserName)
        {
            using (var desktop = new Desktop("Default Browser Changer"))
            {
                var encodedBrowserName = Uri.EscapeDataString(browserName);
                var desktopProcess = new DesktopProcess(@"control.exe /name Microsoft.DefaultPrograms /page pageDefaultProgram\pageAdvancedSettings?pszAppName=" + encodedBrowserName, desktop.Name);
                var exitCode = Wait(() => desktopProcess.GetExitCode()); //TODO: Replace this with WaitForSingleObject
                if (exitCode != 1) //Control.exe always seems returns 1 regardless of whether it had valid arguments.
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
