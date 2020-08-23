using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class PluginData
    {
        public String type { get; set; }
        // sublime = 1, vs code = 2, eclipse = 3, intellij = 4, visualstudio = 6, atom = 7
        public int pluginId { get; set; }
        public String version { get; set; }
        public String os { get; set; }

        // a unique list of file infos (each info represents a file and its metadata)
        public List<PluginDataFileInfo> source;
        public long keystrokes { get; set; }

        // start and end are in seconds
        public long start { get; set; }
        public long local_start { get; set; }
        public long end { get; set; }
        public long local_end { get; set; }
        public String timezone { get; set; }
        public double offset { get; set; }
        public long cumulative_editor_seconds { get; set; }
        public long elapsed_seconds { get; set; }
        public long cumulative_session_seconds { get; set; }
        public String project_null_error { get; set; }
        public String workspace_name { get; set; }
        public String hostname { get; set; }
        public PluginDataProject project { get; set; }

        public PluginData(string projectName, string projectDirectory)
        {
            this.type = "Events";
            this.pluginId = EnvUtil.getPluginId();
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            start = nowTime.now;
            local_start = nowTime.local_now;
            offset = nowTime.offset_minutes;
            version = EnvUtil.GetVersion();
            os = EnvUtil.GetOs();
            source = new List<PluginDataFileInfo>();
            project = GetPluginProjectUsingDir(projectDirectory);
            cumulative_editor_seconds = 0;
            elapsed_seconds = 0;
            cumulative_session_seconds = 0;
            project_null_error = "";
            workspace_name = "";
            hostname = "";
        }

        public static PluginData BuildFromDictionary(IDictionary<string, object> dict)
        {
            PluginDataProject proj = SoftwareCoUtil.ConvertObjectToProject(dict);
            PluginData pd = new PluginData(proj.name, proj.directory);
            pd.end = SoftwareCoUtil.ConvertObjectToLong(dict, "end");
            pd.start = SoftwareCoUtil.ConvertObjectToLong(dict, "start");
            pd.local_end = SoftwareCoUtil.ConvertObjectToLong(dict, "local_end");
            pd.local_start = SoftwareCoUtil.ConvertObjectToLong(dict, "local_start");
            pd.keystrokes = SoftwareCoUtil.ConvertObjectToLong(dict, "keystrokes");
            pd.cumulative_editor_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "cumulative_editor_seconds");
            pd.os = SoftwareCoUtil.ConvertObjectToString(dict, "os");
            pd.offset = SoftwareCoUtil.ConvertObjectToDouble(dict, "offset");
            pd.version = SoftwareCoUtil.ConvertObjectToString(dict, "version");
            pd.timezone = SoftwareCoUtil.ConvertObjectToString(dict, "timezone");
            pd.cumulative_session_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "cumulative_session_seconds");
            pd.pluginId = SoftwareCoUtil.ConvertObjectToInt(dict, "pluginId");
            pd.elapsed_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "elapsed_seconds");
            pd.workspace_name = SoftwareCoUtil.ConvertObjectToString(dict, "workspace_name");
            pd.hostname = SoftwareCoUtil.ConvertObjectToString(dict, "hostname");
            pd.project_null_error = SoftwareCoUtil.ConvertObjectToString(dict, "project_null_error");
            pd.project = proj;
            IDictionary<string, object> sourceDict = SoftwareCoUtil.ConvertObjectToSource(dict);
            if (sourceDict != null && sourceDict.Count > 0)
            {
                foreach (KeyValuePair<string, object> entry in sourceDict)
                {
                    IDictionary<string, object> fileInfoDict = new Dictionary<string, object>();
                    try
                    {
                        PluginDataFileInfo fileInfo = PluginDataFileInfo.GetPluginDataFromDict((JsonObject)entry.Value);
                        pd.source.Add(fileInfo);
                    }
                    catch (Exception e)
                    {
                        //
                    }
                }
            }
            return pd;
        }

        public static async Task<PluginDataProject> GetPluginProject()
        {
            string projectDir = await PackageManager.GetSolutionDirectory();
            return GetPluginProjectUsingDir(projectDir);
        }

        public static PluginDataProject GetPluginProjectUsingDir(string projectDir) {
            string name = "Unnamed";
            PluginDataProject project;
            if (projectDir != null && !projectDir.Equals(""))
            {
                FileInfo fi = new FileInfo(projectDir);
                name = fi.Name;
                project = new PluginDataProject(name, projectDir);
            }
            else
            {
                project = new PluginDataProject(name, "Untitled");
            }

            return project;
        }

        public async Task<string> CompletePayloadAndReturnJsonString()
        {
            RepoResourceInfo resourceInfo = null;
            // make sure we have a valid project and identifier if possible
            if (this.project == null || this.project.directory == null || this.project.directory.Equals("Untitled"))
            {
                // try to get a valid project
                string projectDir = await PackageManager.GetSolutionDirectory();
                if (projectDir != null && !projectDir.Equals(""))
                {
                    FileInfo fi = new FileInfo(projectDir);
                    project = new PluginDataProject(fi.Name, projectDir);
                    resourceInfo = GitUtilManager.GetResourceInfo(projectDir, false);
                }
            } else
            {
                resourceInfo = GitUtilManager.GetResourceInfo(this.project.directory, false);
            }

            if (resourceInfo != null && resourceInfo.identifier != null && !resourceInfo.identifier.Equals(""))
            {
                project.identifier = resourceInfo.identifier;
            }

            SessionSummaryManager summaryMgr = SessionSummaryManager.Instance;
            TimeGapData eTimeInfo = summaryMgr.GetTimeBetweenLastPayload();
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            this.end = nowTime.now;
            this.local_end = nowTime.local_now;

            // get the TimeData for this project dir
            await ValidateAndUpdateCumulativeDataAsync(eTimeInfo.session_seconds);
            this.elapsed_seconds = eTimeInfo.elapsed_seconds;

            // make sure all of the end times are set
            foreach (PluginDataFileInfo pdFileInfo in this.source)
            {
                pdFileInfo.EndFileInfoTime(nowTime);
            }

            double offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            this.offset = Math.Abs((int)offset);
            if (TimeZone.CurrentTimeZone.DaylightName != null
                && TimeZone.CurrentTimeZone.DaylightName != TimeZone.CurrentTimeZone.StandardName)
            {
                this.timezone = TimeZone.CurrentTimeZone.DaylightName;
            }
            else
            {
                this.timezone = TimeZone.CurrentTimeZone.StandardName;
            }

            // update the file metrics used in the tree
            List<FileInfoSummary> fileInfoList = this.GetSourceFileInfoList();
            KeystrokeAggregates aggregates = new KeystrokeAggregates();
            aggregates.directory = this.project.directory;

            foreach (FileInfoSummary fileInfo in fileInfoList)
            {
                aggregates.Aggregate(fileInfo);

                FileChangeInfo fileChangeInfo = FileChangeInfoDataManager.Instance.GetFileChangeInfo(fileInfo.fsPath);
                if (fileChangeInfo == null)
                {
                    // create a new entry
                    fileChangeInfo = new FileChangeInfo();
                }
                fileChangeInfo.UpdateFromFileInfo(fileInfo);
                FileChangeInfoDataManager.Instance.SaveFileChangeInfoDataSummaryToDisk(fileChangeInfo);
            }

            // increment the session summary minutes and other metrics
            summaryMgr.IncrementSessionSummaryData(aggregates, eTimeInfo);

            // create the json payload
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("start", this.start);
            jsonObj.Add("local_start", this.local_start);
            jsonObj.Add("pluginId", this.pluginId);
            jsonObj.Add("type", this.type);
            jsonObj.Add("keystrokes", this.keystrokes);
            jsonObj.Add("project", this.project.GetAsJson());
            jsonObj.Add("timezone", this.timezone);
            jsonObj.Add("offset", this.offset);
            jsonObj.Add("version", this.version);
            jsonObj.Add("os", this.os);
            jsonObj.Add("end", this.end);
            jsonObj.Add("local_end", this.local_end);
            jsonObj.Add("cumulative_editor_seconds", this.cumulative_editor_seconds);
            jsonObj.Add("cumulative_session_seconds", this.cumulative_session_seconds);
            jsonObj.Add("elapsed_seconds", this.elapsed_seconds);
            jsonObj.Add("workspace_name", this.workspace_name);
            jsonObj.Add("hostname", this.hostname);
            jsonObj.Add("project_null_error", this.project_null_error);

            // get the source as json
            jsonObj.Add("source", BuildSourceJson());

            return jsonObj.ToString();
        }

        private async Task ValidateAndUpdateCumulativeDataAsync(long session_seconds)
        {

            TimeData td = await TimeDataManager.Instance.UpdateSessionAndFileSecondsAsync(this.project, session_seconds);

            // get the current payloads so we can compare our last cumulative seconds
            PluginData lastKpm = FileManager.GetLastSavedKeystrokeStats();
            if (SoftwareCoUtil.IsNewDay())
            {
                // the days don't match. don't use the editor or session seconds for a different day
                lastKpm = null;
                // clear out data from the previous day
                await WallclockManager.Instance.GetNewDayCheckerAsync();
                if (td != null) {
                    td = null;
                    this.project_null_error = "TimeData should be null as its a new day";
                }
            }

            this.workspace_name = SoftwareCoUtil.workspace_name;
            this.hostname = SoftwareCoUtil.getHostname();
            this.cumulative_session_seconds = 60;
            this.cumulative_editor_seconds = 60;

            if (td != null)
            {
                this.cumulative_editor_seconds = td.editor_seconds;
                this.cumulative_session_seconds = td.session_seconds;
            }
            else if (lastKpm != null)
            {
                // no time data found, project null error
                this.project_null_error = "TimeData not found using " + this.project.directory + " for editor and session seconds";
                this.cumulative_editor_seconds = lastKpm.cumulative_editor_seconds + 60;
                this.cumulative_session_seconds = lastKpm.cumulative_session_seconds + 60;
            }

            if (this.cumulative_editor_seconds < this.cumulative_session_seconds)
            {
                this.cumulative_editor_seconds = cumulative_session_seconds;
            }
        }

        private JsonObject BuildSourceJson()
        {
            JsonObject sourceData = new JsonObject();
            foreach (PluginDataFileInfo fileInfo in source)
            {
                sourceData.Add(fileInfo.file, fileInfo.GetPluginDataFileInfoAsJson());
            }
            return sourceData;
        }

        public PluginDataFileInfo GetFileInfo(string file)
        {
            for (int i = 0; i < source.Count; i++)
            {
                PluginDataFileInfo fileInfo = source[i];
                if (fileInfo.file.Equals(file))
                {
                    return fileInfo;
                }
            }
            return null;
        }

        public void InitFileInfoIfNotExists(string file)
        {
            if (GetFileInfo(file) == null)
            {
                source.Add(new PluginDataFileInfo(file));
            }
        }

        public List<FileInfoSummary> GetSourceFileInfoList()
        {
            List<FileInfoSummary> fileInfoList = new List<FileInfoSummary>();

            foreach (PluginDataFileInfo pdFileInfo in source)
            {
                // go through the properties of this and check if any have data
                // close, open, paste, delete, keys
                FileInfoSummary fileInfo = new FileInfoSummary();
                fileInfo.close = pdFileInfo.close;
                fileInfo.open = pdFileInfo.open;
                fileInfo.paste = pdFileInfo.paste;
                fileInfo.linesAdded = pdFileInfo.linesAdded;
                fileInfo.linesRemoved = pdFileInfo.linesRemoved;
                fileInfo.delete = pdFileInfo.delete;
                fileInfo.add = pdFileInfo.add;
                fileInfo.keystrokes = fileInfo.add + fileInfo.delete + fileInfo.paste + fileInfo.linesAdded + fileInfo.linesRemoved;
                fileInfo.syntax = pdFileInfo.syntax;
                fileInfo.local_start = pdFileInfo.local_start;
                fileInfo.local_end = pdFileInfo.local_end;
                fileInfo.start = pdFileInfo.start;
                fileInfo.end = pdFileInfo.end;

                // wrapper for a file path
                FileInfo fi = new FileInfo(pdFileInfo.file);
                fileInfo.name = fi.Name;
                fileInfo.fsPath = fi.FullName;
                fileInfo.duration_seconds = fileInfo.end - fileInfo.start;

                fileInfoList.Add(fileInfo);
            }

            return fileInfoList;
        }
    }
}
