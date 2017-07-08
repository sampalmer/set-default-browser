namespace SetDefaultBrowser
{
    public class Browser
    {
        public string UniqueApplicationName { get; set; }
        public ApplicationCapabilities Capabilities { get; set; }

        public override string ToString() => Capabilities.DisplayName;
    }
}
