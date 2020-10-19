using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Task = System.Threading.Tasks.Task;

namespace SoftwareCo
{
    public sealed class DashboardManager
    {
        private static readonly Lazy<DashboardManager> lazy = new Lazy<DashboardManager>(() => new DashboardManager());

        public static DashboardManager Instance { get { return lazy.Value; } }

        private DashboardManager()
        {

        }

        private static string NO_DATA = "CODE TIME\n\nNo data available\n";

        private async Task FetchCodeTimeDashboardAsync()
        {

            string summaryContent = "";
            string summaryInfoFile = FileManager.getSessionSummaryInfoFile();


            HttpResponseMessage resp =
            await SoftwareHttpManager.SendDashboardRequestAsync(HttpMethod.Get, "/dashboard?showToday=true");

            if (SoftwareHttpManager.IsOk(resp))
            {
                summaryContent = await resp.Content.ReadAsStringAsync();
            }
            else
            {
                summaryContent = NO_DATA;
            }


            if (File.Exists(summaryInfoFile))
            {
                File.SetAttributes(summaryInfoFile, FileAttributes.Normal);
            }

            try
            {

                File.WriteAllText(summaryInfoFile, summaryContent, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {

            }

            string dashboardFile = FileManager.getDashboardFile();
            string dashboardContent = "";

            if (FileManager.SessionSummaryInfoFileExists())
            {
                string SummaryData = FileManager.getSessionSummaryInfoFileData();
                dashboardContent += SummaryData;
            }


            if (File.Exists(dashboardFile))
            {
                File.SetAttributes(dashboardFile, FileAttributes.Normal);
            }
            try
            {
                File.WriteAllText(dashboardFile, dashboardContent, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {

            }

        }

        public async void LaunchCodeTimeDashboardAsync()
        {
            try
            {
                await FetchCodeTimeDashboardAsync();
                string dashboardFile = FileManager.getDashboardFile();
                if (File.Exists(dashboardFile))
                {
                    SoftwareCoPackage.ObjDte.ItemOperations.OpenFile(dashboardFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LaunchCodeTimeDashboardAsync, error : " + ex.Message, ex);

            }

        }

        public async void LaunchReadmeFileAsync()
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            try
            {
                string vsReadmeFile = FileManager.getVSReadmeFile();
                if (File.Exists(vsReadmeFile))
                {
                    SoftwareCoPackage.ObjDte.ItemOperations.OpenFile(vsReadmeFile);
                }
                else
                {
                    Assembly _assembly = Assembly.GetExecutingAssembly();
                    string[] resourceNames = _assembly.GetManifestResourceNames();
                    string fileName = "README.txt";
                    string readmeFile = resourceNames.Single(n => n.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase));
                    if (readmeFile == null && resourceNames != null && resourceNames.Length > 0)
                    {
                        foreach (string name in resourceNames)
                        {
                            if (name.IndexOf("README") != -1)
                            {
                                readmeFile = fileName;
                                break;
                            }
                        }
                    }
                    if (readmeFile != null)
                    {
                        StreamReader _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream(readmeFile));
                        string readmeContents = _textStreamReader.ReadToEnd();
                        File.WriteAllText(vsReadmeFile, readmeContents, System.Text.Encoding.UTF8);
                        SoftwareCoPackage.ObjDte.ItemOperations.OpenFile(vsReadmeFile);
                    }
                }
            }
            catch
            {
                //
            }
        }
    }
}
