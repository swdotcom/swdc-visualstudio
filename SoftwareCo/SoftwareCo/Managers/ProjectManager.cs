using System.Threading.Tasks;
using System.IO;
using EnvDTE;
using EnvDTE80;

namespace SoftwareCo
{
    class ProjectManager
    {
        public static DTE2 ObjDte { set; get; }
        public static string solutionDirectory { set; get; }

        /**
         * This will return the project and file detail info.
         * A fileName does not have to be passed in to fetch the current project.
         **/
        public static FileDetails GetFileDatails(string fileName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
                    fd.project_file_name = fileName.Split(solutionDirectory)[1];
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
            } else
            {
                fd.project_name = "Unnamed";
                fd.project_directory = "Untitled";
            }
        }

        public static async Task<string> GetSolutionDirectory()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
            {
                _solutionDirectory = Path.GetDirectoryName(ObjDte.Solution.FileName);
            }
            return _solutionDirectory;
        }
    }
}
