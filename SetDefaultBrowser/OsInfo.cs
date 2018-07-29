using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetDefaultBrowser
{
    static class OsInfo
    {
        /// <summary>
        /// Gets the Windows 10 version number as shown in the "About" screen in the settings app.
        /// This value is also known as the Windows 10 "Release ID".
        /// </summary>
        /// <value>
        /// The version number, such as <c>1803</c> or <c>1703</c>, or <c>null</c> if not running on Windows 10.
        /// </value>
        public static int? Windows10Version
        {
            get
            {
                var versionNumberText = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", null);
                if (versionNumberText == null)
                    return null;

                return int.Parse(versionNumberText, CultureInfo.InvariantCulture);
            }
        }
    }
}
