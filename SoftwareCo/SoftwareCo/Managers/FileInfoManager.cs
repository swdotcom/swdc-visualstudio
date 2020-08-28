using System.IO;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class FileInfoManager
    {
        public static string solutionDirectory { set; get; }

        /**
         * This will return the project and file detail info.
         * A fileName does not have to be passed in to fetch the current project.
         **/
        public static async Task<FileDetails> GetFileDatails(string fileName)
        {
            FileDetails fd = new FileDetails();
            if (string.IsNullOrEmpty(fileName))
            {
                return fd;
            }

            FileInfo fi = new FileInfo(fileName);

            fd.full_file_name = fileName;
            fd.file_name = fi.Name;

            fd.character_count = fi.Length;
            // in case the ObjDte has issues obtaining the syntax
            string ext = fi.Extension;
            if (ext.IndexOf(".") != -1)
            {
                fd.syntax = ext.Substring(ext.IndexOf(".") + 1);
            }
            else
            {
                fd.syntax = ext;
            }

            fd.line_count = DocEventManager.CountLinesLINQ(fileName);

            solutionDirectory = await PackageManager.GetSolutionDirectory();
            if (!string.IsNullOrEmpty(solutionDirectory))
            {
                FileInfo projInfo = new FileInfo(solutionDirectory);
                fd.project_directory = solutionDirectory;
                fd.project_name = projInfo.Name;

                if (!string.IsNullOrEmpty(fileName))
                {
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
                fd.project_file_name = fi.Name;
            }
            return fd;
        }
    }
}
