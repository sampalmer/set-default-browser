using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetDefaultBrowser
{
    partial class BrowserRegistry
    {
        public static Browser[] GetInstalledBrowsers()
        {
            var registeredApplications = GetRegisteredApplications();

            var results = new List<Browser>();
            foreach (var registeredApplication in registeredApplications)
            {
                var capabilities = TryGetCapabilities(registeredApplication.Value);
                if (capabilities != null)
                    if (capabilities.DisplayName != null && capabilities.Associations.Contains("http", StringComparer.OrdinalIgnoreCase))
                        results.Add(new Browser
                        {
                            UniqueApplicationName = registeredApplication.Key,
                            Capabilities = capabilities,
                        });
            }

            return results.ToArray();
        }

        private static IDictionary<string, string> GetRegisteredApplications()
        {
            var result = new Dictionary<string, string>();

            // Grabbing values from HKEY_LOCAL_MACHINE, and then overriding them with HKEY_CURRENT_USER.
            foreach (var baseKey in new[] { Registry.LocalMachine, Registry.CurrentUser })
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\RegisteredApplications"))
                    if (key != null)
                        foreach (var valueName in key.GetValueNames())
                        {
                            var value = key.GetValue(valueName);
                            if (value is string stringValue)
                                result[valueName] = stringValue;
                        }

            return result;
        }

        private static ApplicationCapabilities TryGetCapabilities(string applicationCapabilityPath)
        {
            var result = new ApplicationCapabilities();

            using (var key = Registry.CurrentUser.OpenSubKey(applicationCapabilityPath) ?? Registry.LocalMachine.OpenSubKey(applicationCapabilityPath))
            {
                if (key == null)
                    return null;

                var rawApplicationName = key.GetValue("ApplicationName") as string;
                if (rawApplicationName != null)
                {
                    var resourceValue = new StringBuilder(4096);
                    var hresult = WindowsApi.SHLoadIndirectString(rawApplicationName, resourceValue, resourceValue.Capacity, IntPtr.Zero);
                    result.DisplayName = hresult == 0 ? resourceValue.ToString() : rawApplicationName;
                }

                var associations = new List<string>();
                foreach (var subkeyName in new[] { "FileAssociations", "URLAssociations" })
                    using (var subkey = key.OpenSubKey(subkeyName))
                        if (subkey != null)
                            associations.AddRange(subkey.GetValueNames());
                result.Associations = associations.ToArray();
            }

            return result;
        }
    }
}
