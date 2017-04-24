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

                DefaultBrowserChanger.Set(browserName: args[0]);
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


    }
}
