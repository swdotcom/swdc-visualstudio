using System;
using System.Reflection;

namespace SoftwareCo
{
    class EnvUtil
    {
        private static string pluginName = "swdc-visualstudio";
        //
        // sublime = 1, vs code = 2, eclipse = 3, intellij = 4, visualstudio = 6, atom = 7
        //
        private static int pluginId = 6;

        public static int getPluginId()
        {
            return pluginId;
        }

        public static string getPluginName()
        {
            return pluginName;
        }

        public static string GetVersion()
        {
            return string.Format("{0}.{1}.{2}", CodeTimeAssembly.Version.Major, CodeTimeAssembly.Version.Minor, CodeTimeAssembly.Version.Build);
        }

        public static string GetOs()
        {
            return System.Environment.OSVersion.VersionString;
        }

        public static class CodeTimeAssembly
        {
            static readonly Assembly Reference = typeof(CodeTimeAssembly).Assembly;

            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
