﻿using System;
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
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            end = nowTime.now;
            local_end = nowTime.local_now;

            // get the TimeData for this project dir
            TimeData td = await TimeDataManager.Instance.GetTodayTimeDataSummary(this.project);

            long editorSeconds = td != null ? Math.Max(td.editor_seconds, 60) : 60;

            // make sure all of the end times are set
            foreach (PluginDataFileInfo pdFileInfo in this.source)
            {
                pdFileInfo.EndFileInfoTime(nowTime);
                pdFileInfo.cumulative_editor_seconds = editorSeconds;
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

            // get the source as json
            jsonObj.Add("source", BuildSourceJson());

            return jsonObj.ToString();
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

        public void UpdatePluginDataFileInfo(PluginDataFileInfo fileInfo)
        {
            //
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
