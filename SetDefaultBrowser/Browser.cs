namespace SetDefaultBrowser
{
    public class Browser
    {
        /// <summary>
        /// Uniquely identifies the browser for Windows' "Set Default Programs" applet.
        /// Not suitable for display to user.
        /// </summary>
        public string UniqueApplicationName { get; set; }

        public string DisplayName { get; set; }

        /// <summary>
        /// File extensions are in the form ".abc".
        /// Protocols are in the form "abc".
        /// </summary>
        public string[] Associations { get; set; }

        public override string ToString() => DisplayName;
    }
}
