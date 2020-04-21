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
        public Int32 new_day { get; set; } // 1 or zero to denote new day or not
        public String project_null_error { get; set; }
        public String session_seconds_error { get; set; }
        public String editor_seconds_error { get; set; }
        public PluginDataProject project { get; set; }

        public PluginData(string projectName, string projectDirectory)
        {
            this.type = "Events";
            this.pluginId = Constants.PluginId;
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            start = nowTime.now;
            local_start = nowTime.local_now;
            offset = nowTime.offset_minutes;
            version = SoftwareCoPackage.GetVersion();
            os = SoftwareCoPackage.GetOs();
            source = new List<PluginDataFileInfo>();
            project = GetPluginProjectUsingDir(projectDirectory);
            cumulative_editor_seconds = 0;
            elapsed_seconds = 0;
            cumulative_session_seconds = 0;
            project_null_error = "";
            session_seconds_error = "";
            editor_seconds_error = "";
            new_day = 0;
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
            pd.new_day = SoftwareCoUtil.ConvertObjectToInt(dict, "new_day");
            pd.project_null_error = SoftwareCoUtil.ConvertObjectToString(dict, "project_null_error");
            pd.session_seconds_error = SoftwareCoUtil.ConvertObjectToString(dict, "session_seconds_error");
            pd.editor_seconds_error = SoftwareCoUtil.ConvertObjectToString(dict, "editor_seconds_error");
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
            string projectDir = await DocEventManager.GetSolutionDirectory();
            return GetPluginProjectUsingDir(projectDir);
        }

        public static PluginDataProject GetPluginProjectUsingDir(string projectDir) {
            string name = "Unnamed";
            PluginDataProject project;
            if (projectDir != null && !projectDir.Equals(""))
            {
                FileInfo fi = new FileInfo(projectDir);
                name = fi.Name;
                RepoResourceInfo resourceInfo = GitUtilManager.GetResourceInfo(projectDir, false);
                project = new PluginDataProject(name, projectDir);
                if (resourceInfo != null && resourceInfo.identifier != null && !resourceInfo.identifier.Equals(""))
                {
                    project.identifier = resourceInfo.identifier;
                }
            }
            else
            {
                project = new PluginDataProject(name, "Untitled");
            }

            return project;
        }

        public async Task<string> CompletePayloadAndReturnJsonString()
        {
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
            jsonObj.Add("elapsed_seconds", this.elapsed_seconds);

            // get the source as json
            jsonObj.Add("source", BuildSourceJson());

            return jsonObj.ToString();
        }

        private async Task ValidateAndUpdateCumulativeDataAsync(long session_seconds)
        {

            TimeData td = await TimeDataManager.Instance.UpdateSessionAndFileSecondsAsync(this.project, session_seconds);

            // add the cumulative data

            long lastPayloadEnd = FileManager.getItemAsLong("latestPayloadTimestampEndUtc");
            this.new_day = lastPayloadEnd == 0 ? 1 : 0;

            // get the current payloads so we can compare our last cumulative seconds
            PluginData lastKpm = FileManager.GetLastSavedKeystrokeStats();
            bool initiateNewDayCheck = false;
            if (lastKpm != null)
            {
                String lastKpmDay = SoftwareCoUtil.GetFormattedDay(lastKpm.start);
                String thisDay = SoftwareCoUtil.GetFormattedDay(this.start);
                if (!lastKpmDay.Equals(thisDay))
                {
                    // the days don't match. don't use the editor or session seconds for a different day
                    lastKpm = null;
                    initiateNewDayCheck = true;
                    td = null;
                }
            }

            if (initiateNewDayCheck)
            {
                // clear out data from the previous day
                await WallclockManager.Instance.GetNewDayCheckerAsync();
            }

            cumulative_session_seconds = 60;
            cumulative_editor_seconds = 60;

            if (td != null)
            {
                this.cumulative_editor_seconds = td.editor_seconds;
                this.cumulative_session_seconds = td.session_seconds;
            }
            else if (lastKpm != null)
            {
                // no time data found, project null error
                this.project_null_error = "TimeData not found using " + this.project.directory + " for editor and session seconds";
                cumulative_editor_seconds = lastKpm.cumulative_editor_seconds + 60;
                cumulative_session_seconds = lastKpm.cumulative_session_seconds + 60;
            }

            if (cumulative_editor_seconds < cumulative_session_seconds)
            {
                long diff = cumulative_session_seconds - cumulative_editor_seconds;
                if (diff > 30)
                {
                    this.editor_seconds_error = "Cumulative editor seconds is behind session seconds by " + diff + " seconds";
                }
                cumulative_editor_seconds = cumulative_session_seconds;
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
