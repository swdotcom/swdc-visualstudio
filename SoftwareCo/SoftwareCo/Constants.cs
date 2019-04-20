namespace SoftwareCo
{
    internal static class Constants
    {
        internal const string PluginName = "swdc-visualstudio";
        //
        // sublime = 1, vs code = 2, eclipse = 3, intellij = 4, visualstudio = 6, atom = 7
        //
        internal static int PluginId = 6;
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