using System.IO;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class ProjectInfoManager
    {
        public static string solutionDirectory { set; get; }

        /**
         * This will return the project and file detail info.
         * A fileName does not have to be passed in to fetch the current project.
         **/
        public static async Task<FileDetails> GetFileDatails(string fileName)
        {
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

            solutionDirectory = await PackageManager.GetSolutionDirectory();
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

                if (string.IsNullOrEmpty(fd.syntax))
                {
                    fd.syntax = await PackageManager.GetActiveDocumentSyntax();
                }
            }
            else
            {
                fd.project_name = "Unnamed";
                fd.project_directory = "Untitled";
            }
            return fd;
        }
    }
}
