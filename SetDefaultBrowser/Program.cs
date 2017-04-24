using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SetDefaultBrowser
{
    class Program
    {
        static int Main(string[] rawArgs)
        {
            var args = Args.TryParse(rawArgs);
            if (args == null)
            {
                //Using message boxes for output so we don't have to show a console window.
                MessageBox.Show(text: Args.UsageText, caption: ApplicationTitle, buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
                return 1;
            }

            try
            {
                DefaultBrowserChanger.Set(args.BrowserName);
            }
            catch (EnvironmentException ex)
            {
                if (!args.Silent)
                    MessageBox.Show(text: ex.Message, caption: ApplicationTitle, buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                return 1;
            }
            catch //Ensures all nested "finally" blocks execute
            {
                throw;
            }

            return 0;
        }

        private static string ApplicationTitle => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;
    }
}
