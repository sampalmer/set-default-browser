using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                {
                    var associations = capabilities.Associations.Select(association => association.Association).ToArray();
                    if (new[] { "http", "https" }.All(protocol => associations.Contains(protocol, StringComparer.OrdinalIgnoreCase)))
                    {
                        var displayName = TryGetDisplayName(capabilities);
                        if (displayName != null)
                            results.Add(new Browser
                            {
                                UniqueApplicationName = registeredApplication.Key,
                                DisplayName = displayName,
                                Associations = associations,
                            });
                    }
                }
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
                    result.ApplicationName = hresult == 0 ? resourceValue.ToString() : rawApplicationName;
                }

                var associations = new List<ApplicationAssociation>();
                foreach (var subkeyName in new[] { "FileAssociations", "URLAssociations" })
                    using (var subkey = key.OpenSubKey(subkeyName))
                        if (subkey != null)
                            foreach (var association in subkey.GetValueNames())
                            {
                                var progId = subkey.GetValue(association) as string;
                                associations.Add(new ApplicationAssociation
                                {
                                    Association = association,
                                    ProgId = progId,
                                });
                            }

                result.Associations = associations.ToArray();
            }

            return result;
        }

        private static string TryGetDisplayName(ApplicationCapabilities capabilities)
        {
            // The display name of a registered application is optional. Internet Explorer doesn't use it, for example.
            if (capabilities.ApplicationName != null)
                return capabilities.ApplicationName;

            foreach (var association in capabilities.Associations)
                if (!String.IsNullOrEmpty(association.ProgId))
                {
                    var appName = AssocQueryString(association);
                    if (appName != null)
                        return appName;
                }

            return null;
        }

        private static string AssocQueryString(ApplicationAssociation association)
        {
            uint appNameLength = 0;
            var result = WindowsApi.AssocQueryString(WindowsApi.AssocF.None, WindowsApi.AssocStr.FriendlyAppName, association.ProgId, null, null, ref appNameLength);
            if (new[] { 2147943555, 2147942402 }.Contains(result)) // No application is associated with the specified file for this operation OR The system cannot find the file specified
                return null;

            if (result != 1)
                throw new Win32Exception((int)result);

            var buffer = new StringBuilder((int)appNameLength);
            var bufferLength = (uint)buffer.Capacity;
            result = WindowsApi.AssocQueryString(WindowsApi.AssocF.None, WindowsApi.AssocStr.FriendlyAppName, association.ProgId, null, buffer, ref bufferLength);
            if (result != 0)
                throw new Win32Exception((int)result);

            return buffer.ToString();
        }

        private class ApplicationCapabilities
        {
            /// <summary>
            /// May be <c>null</c>.
            /// </summary>
            public string ApplicationName { get; set; }
            public ApplicationAssociation[] Associations { get; set; }
        }

        private class ApplicationAssociation
        {
            public string Association { get; set; }
            
            /// <summary>
            /// May be null
            /// </summary>
            public string ProgId { get; set; }
        }

    }
}
