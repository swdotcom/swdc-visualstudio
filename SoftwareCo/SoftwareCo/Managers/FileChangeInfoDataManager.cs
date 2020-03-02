using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class FileChangeInfoDataManager
    {
        private static readonly Lazy<FileChangeInfoDataManager> lazy = new Lazy<FileChangeInfoDataManager>(() => new FileChangeInfoDataManager());

        private List<FileChangeInfo> _fileChangeInfos = new List<FileChangeInfo>();

        public static FileChangeInfoDataManager Instance { get { return lazy.Value; } }

        private FileChangeInfoDataManager()
        {
            //
        }

        public void ClearFileChangeInfoDataSummary()
        {
            string file = SoftwareCoUtil.getFileChangeInfoSummaryFile();

            if (SoftwareCoUtil.FileChangeInfoSummaryFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            try
            {
                File.WriteAllText(file, new JsonObject().ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }

            _fileChangeInfos = new List<FileChangeInfo>();
        }

        public async Task SaveFileChangeInfoDataSummaryToDisk(FileChangeInfo data)
        {
            string MethodName = "SaveFileChangeInfoDataSummaryToDisk";
            List<FileChangeInfo> changeInfos = GetFileChangeInfoSummaryList();
            bool foundExisting = false;
            foreach (FileChangeInfo changeInfo in changeInfos)
            {
                if (changeInfo.fsPath.Equals(data.fsPath))
                {
                    changeInfo.Clone(data);
                    foundExisting = true;
                    break;
                }
            }
            if (!foundExisting)
            {
                changeInfos.Add(data);
            }
            string file = SoftwareCoUtil.getFileChangeInfoSummaryFile();

            if (SoftwareCoUtil.FileChangeInfoSummaryFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            JsonObject jsonToSave = BuildJsonObjectFromList(changeInfos);

            try
            {
                File.WriteAllText(file, jsonToSave.ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }

            _fileChangeInfos = new List<FileChangeInfo>(changeInfos);
        }

        private JsonObject BuildJsonObjectFromList(List<FileChangeInfo> fileChangeInfos)
        {
            JsonObject jsonObj = new JsonObject();

            foreach (FileChangeInfo info in fileChangeInfos)
            {
                jsonObj.Add(info.fsPath, info.GetAsJson());
            }

            return jsonObj;
        }

        public FileChangeInfo GetFileChangeInfo(string fsPath)
        {
            if (_fileChangeInfos.Count == 0)
            {
                GetFileChangeInfoSummaryList();
            }

            foreach (FileChangeInfo info in _fileChangeInfos)
            {
                if (info.fsPath.Equals(fsPath))
                {
                    return info;
                }
            }
            return null;
        }

        public List<FileChangeInfo> GetTopKeystrokeFiles()
        {
            List<FileChangeInfo> changeInfos = GetFileChangeInfoSummaryList();
            if (changeInfos == null || changeInfos.Count == 0)
            {
                return new List<FileChangeInfo>();
            }
            Int32 limit = Math.Min(changeInfos.Count, 3);
            List<FileChangeInfo> orderedInfos = changeInfos.OrderBy(o => o.keystrokes).Reverse().ToList<FileChangeInfo>().GetRange(0, limit);
            return orderedInfos;
        }

        public List<FileChangeInfo> GetTopCodeTimeFiles()
        {
            List<FileChangeInfo> changeInfos = GetFileChangeInfoSummaryList();
            if (changeInfos == null || changeInfos.Count == 0)
            {
                return new List<FileChangeInfo>();
            }
            Int32 limit = Math.Min(changeInfos.Count, 3);
            List<FileChangeInfo> orderedInfos = changeInfos.OrderBy(o => o.duration_seconds).Reverse().ToList<FileChangeInfo>().GetRange(0, limit);
            return orderedInfos;
        }

        public List<FileChangeInfo> GetFileChangeInfoSummaryList()
        {
            _fileChangeInfos = new List<FileChangeInfo>();
            if (!SoftwareCoUtil.FileChangeInfoSummaryFileExists())
            {
                // create it
                ClearFileChangeInfoDataSummary();
            }

            string fileChangeInfoSummary = SoftwareCoUtil.getFileChangeInfoSummaryData();

            // it'll be a map of file to FileChangeInfo objects
            IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(fileChangeInfoSummary);
            foreach (string key in jsonObj.Keys)
            {
                FileChangeInfo info = new FileChangeInfo();

                jsonObj.TryGetValue(key, out object infoObj);
                try
                {
                    JsonObject infoObjJson = (infoObj == null) ? null : (JsonObject)infoObj;
                    if (infoObjJson != null)
                    {
                        info.CloneFromDictionary(infoObjJson);
                    }
                } catch (Exception e)
                {
                    //
                }

                _fileChangeInfos.Add(info);
            }

            return _fileChangeInfos;
        }
    }
}
