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

        public static string GetUsageText(string formattedBrowserList)
        {
            var browserNameHelp = String.IsNullOrWhiteSpace(formattedBrowserList)
                ?
                "The name of the browser as shown in Windows' \"Set Default Programs\" screen"
                :
                "One of the following:\n" + IndentBy(formattedBrowserList, 8);

            return $@"Sets the default browser

Usage: {Path.GetFileName(Assembly.GetEntryAssembly().Location)} browser [{silentSwitch}]

    browser: {browserNameHelp}
    {silentSwitch}: Don't show error messages or usage help (optional)
";
        }

        private static string IndentBy(string text, int indentSize)
        {
            var lineSeparator = '\n';
            var indentation = new string(' ', indentSize);

            return string.Join(lineSeparator.ToString(),
                text
                    .Split(lineSeparator)
                    .Select(line => indentation + line)
            );
        }
    }
}
