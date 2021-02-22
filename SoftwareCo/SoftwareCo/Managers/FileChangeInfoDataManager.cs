using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string file = FileManager.getFileChangeInfoSummaryFile();

            if (FileManager.FileChangeInfoSummaryFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            try
            {
                string content = JsonConvert.SerializeObject(new JsonObject());

                File.WriteAllText(file, content, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }

            _fileChangeInfos = new List<FileChangeInfo>();
        }

        public async Task SaveFileChangeInfoDataSummaryToDisk(FileChangeInfo data)
        {
            List<FileChangeInfo> changeInfos = GetFileChangeInfoSummaryList();
            bool foundExisting = false;
            foreach (FileChangeInfo changeInfo in changeInfos)
            {
                if (!string.IsNullOrEmpty(changeInfo.fsPath) && changeInfo.fsPath.Equals(data.fsPath))
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
            string file = FileManager.getFileChangeInfoSummaryFile();

            if (FileManager.FileChangeInfoSummaryFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            try
            {
                JsonObject jsonToSave = BuildJsonObjectFromList(changeInfos);
                string json = JsonConvert.SerializeObject(jsonToSave);
                File.WriteAllText(file, json, System.Text.Encoding.UTF8);
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
                if (info.fsPath != null && info.fsPath.Equals(fsPath))
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
            List<FileChangeInfo> orderedInfos = changeInfos.OrderBy(o => o.keystrokes).Reverse().ToList<FileChangeInfo>();
            List<FileChangeInfo> finalList = new List<FileChangeInfo>();
            for (int i = 0; i < orderedInfos.Count; i++)
            {
                FileChangeInfo info = orderedInfos[i];
                if (info.keystrokes > 0)
                {
                    finalList.Add(info);
                }
                if (finalList.Count >= 3)
                {
                    break;
                }
            }
            return finalList;
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
            if (!FileManager.FileChangeInfoSummaryFileExists())
            {
                // create it
                ClearFileChangeInfoDataSummary();
            }

            string fileChangeInfoSummary = FileManager.getFileChangeInfoSummaryData();
            try
            {
                fileChangeInfoSummary = SoftwareCoUtil.CleanJsonToDeserialize(fileChangeInfoSummary);
                if (fileChangeInfoSummary.StartsWith("["))
                {
                    _fileChangeInfos = JsonConvert.DeserializeObject<List<FileChangeInfo>>(fileChangeInfoSummary);
                }
                else
                {
                    // it'll be a map of file to FileChangeInfo objects
                    IDictionary<string, object> jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileChangeInfoSummary);
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
                        }
                        catch (Exception e)
                        {
                            //
                        }

                        _fileChangeInfos.Add(info);
                    }
                }
            }
            catch (Exception) { }

            return _fileChangeInfos;
        }
    }
}
