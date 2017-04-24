using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SetDefaultBrowser
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                var usageText = $"Sets the default browser\n\nUsage: {Path.GetFileName(Application.Location)} browsername\nbrowsername: The name of the browser as shown in Windows' \"Set Default Programs\" screen, such as \"Google Chrome\" or \"Firefox\".";

                MessageBox.Show(text: usageText, caption: ApplicationTitle, buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
                return 1;
            }

            try
            {
                DefaultBrowserChanger.Set(browserName: args[0]);
            }
            catch (EnvironmentException ex)
            {
                MessageBox.Show(text: ex.Message, caption: ApplicationTitle, buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                return 1;
            }
            catch //Ensures all nested "finally" blocks execute
            {
                throw;
            }

            return 0;
        }

        private static Assembly Application => Assembly.GetEntryAssembly();
        private static string ApplicationTitle => Application.GetCustomAttribute<AssemblyTitleAttribute>().Title;
    }
}
