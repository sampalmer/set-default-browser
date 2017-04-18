using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UIAutomationClient;

namespace Control_Panel___UI_Automation
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var process = Process.Start(@"C:\Windows\system32\control.exe", @"/name Microsoft.DefaultPrograms /page pageDefaultProgram\pageAdvancedSettings?pszAppName=google%20chrome");
                var exited = process.WaitForExit(3000);
                if (!exited)
                {
                    process.Kill();
                    throw new Exception("Control panel process didn't exit");
                }

                var uiAutomation = new CUIAutomation();

                var window = FindAndWait(uiAutomation.GetRootElement(), TreeScope.TreeScope_Children, uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Set Program Associations"), TimeSpan.FromSeconds(3));
                try
                {
                    //Sadly, the window can't be hidden or moved to another desktop since doing so results in UI Automation not seeing it.

                    var listView = FindAndWait(window, TreeScope.TreeScope_Descendants, uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Program Association List"), TimeSpan.FromSeconds(3));
                    var save = FindAndWait(window, TreeScope.TreeScope_Descendants, uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Save"), TimeSpan.FromSeconds(3));

                    var listItems = new[]
                    {
                        ".htm",
                        ".html",
                        "HTTP",
                        "HTTPS",
                    }.Select(name => FindAndWait(
                        listView,
                        TreeScope.TreeScope_Descendants,
                        uiAutomation.CreateAndCondition(
                            uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, name),
                            uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_ListItemControlTypeId)
                        ),
                        TimeSpan.FromSeconds(3)
                    )).ToArray();

                    foreach (var listItem in listItems)
                    {
                        var toggle = ((IUIAutomationTogglePattern)listItem.GetCurrentPattern(UIA_PatternIds.UIA_TogglePatternId));
                        while (toggle.CurrentToggleState != ToggleState.ToggleState_On) //TODO: Watch out for infinite looping here!
                            toggle.Toggle();
                    }

                    ((IUIAutomationInvokePattern)save.GetCurrentPattern(UIA_PatternIds.UIA_InvokePatternId)).Invoke();
                }
                finally
                {
                    ((IUIAutomationWindowPattern)window.GetCurrentPattern(UIA_PatternIds.UIA_WindowPatternId)).Close();
                }
            }
            catch //Ensures all nested finally blocks execute
            {
                throw;
            }
        }

        private static IUIAutomationElement FindAndWait(IUIAutomationElement rootElement, TreeScope scope, IUIAutomationCondition condition, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();

            do
            {
                var result = rootElement.FindAll(scope, condition);
                if (result.Length > 1)
                    throw new Exception("Found multiple elements");
                else if (result.Length == 1)
                    return result.GetElement(0);

                Thread.Sleep(1);
            } while (stopwatch.Elapsed < timeout);

            throw new TimeoutException();
        }
    }
}
