namespace SoftwareCo
{
    internal static class Constants
    {
        internal const string PluginName = "swdc-visualstudio";
        internal static string PluginVersion = "0.1.7";
        internal const string EditorName = "visualstudio";

        internal const string PROD_API_ENDPOINT = "https://api.software.com";
        internal const string PROD_URL_ENDPOINT = "https://app.software.com";

        internal const string api_endpoint = PROD_API_ENDPOINT;
        internal const string url_endpoint = PROD_URL_ENDPOINT;

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