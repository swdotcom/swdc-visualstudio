using System;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareCo
{
    internal enum LogLevel
    {
        Debug = 1,
        Info,
        Warning,
        HandledException
    };

    class Logger
    {
        private static IVsOutputWindowPane _softwarecoOutputWindowPane;

        private static IVsOutputWindowPane SoftwareCoOutputWindowPane
        {
            get { return _softwarecoOutputWindowPane ?? (_softwarecoOutputWindowPane = GetSoftwareCoOutputWindowPane()); }
        }

        private static IVsOutputWindowPane GetSoftwareCoOutputWindowPane()
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null) return null;

            var outputPaneGuid = new Guid(Guids.GuidSoftwareCoOutputPane.ToByteArray());
            IVsOutputWindowPane windowPane;

            outputWindow.CreatePane(ref outputPaneGuid, "SoftwareCo", 1, 1);
            outputWindow.GetPane(ref outputPaneGuid, out windowPane);

            return windowPane;
        }

        internal static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        internal static void Error(string message, Exception ex = null)
        {
            var exceptionMessage = string.Format("{0}: {1}", message, ex);

            Log(LogLevel.HandledException, exceptionMessage);
        }

        internal static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        internal static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        private static void Log(LogLevel level, string message)
        {
            var outputWindowPane = SoftwareCoOutputWindowPane;
            if (outputWindowPane == null) return;

            var outputMessage = string.Format("[SoftwareCo {0} {1}] {2}{3}", Enum.GetName(level.GetType(), level),
                DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture), message, Environment.NewLine);

            outputWindowPane.OutputString(outputMessage);
        }
    }
}
