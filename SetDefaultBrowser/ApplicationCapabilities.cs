namespace SetDefaultBrowser
{
    public class ApplicationCapabilities
    {
        /// <summary>
        /// May be <c>null</c>.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// File extensions are in the form ".abc".
        /// Protocols are in the form "abc".
        /// </summary>
        public string[] Associations { get; set; }
    }
}
