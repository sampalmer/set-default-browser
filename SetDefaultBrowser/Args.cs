using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SetDefaultBrowser
{
    class Args
    {
        private const string silentSwitch = "/silent";

        public string BrowserName { get; set; }
        public bool Silent { get; set; }

        public static Args TryParse(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
                return null;

            var result = new Args
            {
                BrowserName = args[0],
                Silent = false,
            };

            if (args.Length >= 2)
                if (args[1] == silentSwitch)
                    result.Silent = true;
                else
                    return null;

            return result;
        }

        public static string UsageText
        {
            get
            {
                return $@"Sets the default browser

Usage: {Path.GetFileName(Assembly.GetEntryAssembly().Location)} browsername [{silentSwitch}]

    browsername: The name of the browser as shown in Windows' ""Set Default Programs"" screen, such as ""Google Chrome"" or ""Firefox"".
    {silentSwitch}: Don't show error messages or usage help (optional)
";
            }
        }
    }
}
