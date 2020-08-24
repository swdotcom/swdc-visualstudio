using EnvDTE;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class ProjectInfoManager
    {
        public static DTE ObjDte { set; get; }
        public static string solutionDirectory { set; get; }

        /**
         * This will return the project and file detail info.
         * A fileName does not have to be passed in to fetch the current project.
         **/
        public static async Task<FileDetails> GetFileDatails(string fileName)
        {
            // await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            FileDetails fd = new FileDetails();
            if (!string.IsNullOrEmpty(fileName))
            {
                fd.full_file_name = fileName;

                FileInfo fi = new FileInfo(fileName);
                fd.file_name = fi.Name;

                fd.character_count = fi.Length;
                // in case the ObjDte has issues obtaining the syntax
                fd.syntax = fi.Extension;
            }

            solutionDirectory = await GetSolutionDirectory();
            if (!string.IsNullOrEmpty(solutionDirectory))
            {
                FileInfo projInfo = new FileInfo(solutionDirectory);
                fd.project_directory = solutionDirectory;
                fd.project_name = projInfo.Name;

                if (!string.IsNullOrEmpty(fileName))
                {
                    // get the project file name
                    fd.project_file_name = fileName.Substring(solutionDirectory.Length);
                }

                try
                {
                    ProjectItem projItem = ObjDte.Solution.FindProjectItem(fileName);
                    if (projItem != null)
                    {
                        fd.syntax = projItem.Document.Language;
                    }
                }
                catch (Exception e)
                {
                    Logger.Info($"Unable to obtain file language: {e.Message}");
                }
            }
            else
            {
                fd.project_name = "Unnamed";
                fd.project_directory = "Untitled";
            }
            return fd;
        }

        public static async Task<string> GetSolutionDirectory()
        {
            // await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string solutionDirectory = "";
            if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
            {
                solutionDirectory = Path.GetDirectoryName(ObjDte.Solution.FileName);
            }
            return solutionDirectory;
        }
    }
}
