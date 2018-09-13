namespace SoftwareCo
{
    internal static class Constants
    {
        internal const string PluginName = "swdc-visualstudio";
        internal static string PluginVersion = "0.1.13";
        internal const string EditorName = "visualstudio";

        internal const string api_endpoint = "https://api.software.com";
        internal const string url_endpoint = "https://app.software.com";

        internal static string EditorVersion
        {
            get
            {
                if (SoftwareCoPackage.ObjDte == null)
                {
                    return string.Empty;
                }
                return SoftwareCoPackage.ObjDte.Version;
            }
        }
    }
}