using System;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareCo
{
    internal enum LogLevel
    {
        Debug = 1,
        Info,
        Warning,
        HandledException,
        File
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

            outputWindow.CreatePane(ref outputPaneGuid, "CodeTime", 1, 1);
            outputWindow.GetPane(ref outputPaneGuid, out windowPane);

            return windowPane;
        }

        internal static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        internal static void FileLog(string message,string method)
        {
            Log(LogLevel.File, message,method);
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

        private static void Log(LogLevel level, string message, string method = null)
        {
            var outputWindowPane = SoftwareCoOutputWindowPane;
            if (outputWindowPane == null) return;

            var outputMessage = string.Format("[CodeTime {0} {1}] {2}{3}", Enum.GetName(level.GetType(), level),
                DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture), message, Environment.NewLine);

            if(Enum.GetName(level.GetType(),level)=="File" || Enum.GetName(level.GetType(), level) == "HandledException")
            {
                string LogContent = outputMessage.ToString();
                string LogDataPath = FileManager.getLogFile();
                    try
                    {
                    File.AppendAllText(LogDataPath, LogContent);
                    }
                    catch (Exception ex)
                    {

                    }
                
                
            }
            else
            outputWindowPane.OutputString(outputMessage);
        }
    }
}
