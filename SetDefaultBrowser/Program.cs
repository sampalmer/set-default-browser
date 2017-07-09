using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SetDefaultBrowser
{
    class Program
    {
        static int Main(string[] rawArgs)
        {
            var browsers = BrowserRegistry.GetInstalledBrowsers();

            var args = Args.TryParse(rawArgs);
            if (args == null)
            {
                //Using message boxes for output so we don't have to show a console window.
                MessageBox.Show(text: Args.GetUsageText(FormattedInstalledBrowsers(browsers)), caption: ApplicationTitle, buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
                return 1;
            }

            try
            {
                if (!browsers.Any())
                    throw new EnvironmentException("Didn't find any web browsers installed.");

                //Using a current culture string comparison here since we're comparing user input against display text.
                var browser = browsers.FirstOrDefault(b => b.DisplayName.Equals(args.BrowserName, StringComparison.CurrentCulture));
                if (browser == null)
                    throw new EnvironmentException($"Didn't find a web browser with the name '{args.BrowserName}'.\n\nPlease use one of the following:\n{FormattedInstalledBrowsers(browsers)}");

                DefaultBrowserChanger.Set(browser);
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
        private static string FormattedInstalledBrowsers(IEnumerable<Browser> browsers)
        {
            return String.Join("\n", browsers.Select(browser => "• " + browser.DisplayName).Distinct().OrderBy(n => n));
        }
    }
}
