namespace SoftwareCo
{
    internal static class Constants
    {
        internal const string EditorName = "visualstudio";
        internal const long DEFAULT_SESSION_THRESHOLD_SECONDS = 60 * 15;

        internal const string api_endpoint = "https://stagingapi.software.com"; // "https://api.software.com";
        internal const string url_endpoint = "https://staging.software.com"; //"https://app.software.com";
        internal const string cody_email_url = "mailto:cody@software.com";

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