using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SoftwareCo
{

    class TrackerUtilManager
    {
        private static TrackerManager tracker;

        public static void init()
        {
            tracker = new TrackerManager(Constants.api_endpoint, "CodeTime", "Code Time");
            TrackEditorActionEvent("editor", "activate");
        }

        public static async Task TrackCodeTimeEventAsync(PluginData pluginData)
        {
            if (tracker == null)
            {
                return;
            }

            AuthEntity authEntity = GetAuthEntity();
            PluginEntity pluginEntity = GetPluginEntity();

            RepoEntity repoEntity = null;
            foreach (PluginDataFileInfo fileInfo in pluginData.source)
            {
                CodetimeEvent codetimeEvent = new CodetimeEvent();
                codetimeEvent.chars_added = fileInfo.add;
                codetimeEvent.chars_deleted = fileInfo.delete;
                codetimeEvent.chars_pasted = fileInfo.charsPasted;
                codetimeEvent.keystrokes = fileInfo.keystrokes;
                codetimeEvent.end_time = fileInfo.end.ToString();
                codetimeEvent.start_time = fileInfo.start.ToString();
                codetimeEvent.pastes = fileInfo.paste;

                // entities
                codetimeEvent.pluginEntity = pluginEntity;
                codetimeEvent.authEntity = authEntity;

                codetimeEvent.fileEntity = await GetFileEntity(fileInfo.file);
                codetimeEvent.projectEntity = await GetProjectEntity(fileInfo.file);

                if (repoEntity == null)
                {
                    repoEntity = GetRepoEntity(codetimeEvent.projectEntity.project_directory);
                }

                codetimeEvent.repoEntity = repoEntity;

                Console.WriteLine($"CODE TIME EVENT: {codetimeEvent.ToString()}");

                tracker.TrackCodetimeEvent(codetimeEvent);
            }
        }

        public static async Task TrackEditorActionEvent(string entity, string type)
        {
            TrackEditorFileActionEvent(entity, type, null);
        }

        public static async Task TrackEditorFileActionEvent(string entity, string type, string fileName)
        {
            if (tracker == null)
            {
                return;
            }

            EditorActionEvent editorActionEvent = new EditorActionEvent();
            editorActionEvent.entity = entity;
            editorActionEvent.type = type;

            // entities
            editorActionEvent.pluginEntity = GetPluginEntity();
            editorActionEvent.authEntity = GetAuthEntity();

            editorActionEvent.fileEntity = await GetFileEntity(fileName);
            editorActionEvent.projectEntity = await GetProjectEntity(fileName);

            editorActionEvent.repoEntity = GetRepoEntity(editorActionEvent.projectEntity.project_directory);

            tracker.TrackEditorActionEvent(editorActionEvent);
        }

        public static async Task TrackUIInteractionEvent(UIInteractionType interaction_type, UIElementEntity uIElementEntity)
        {
            if (tracker == null)
            {
                return;
            }

            UIInteractionEvent uIInteractionEvent = new UIInteractionEvent();
            uIInteractionEvent.interaction_type = interaction_type;
            uIInteractionEvent.uiElementEntity = uIElementEntity;

            // entities
            uIInteractionEvent.pluginEntity = GetPluginEntity();
            uIInteractionEvent.authEntity = GetAuthEntity();

            tracker.TrackUIInteractionEvent(uIInteractionEvent);
        }

        private static AuthEntity GetAuthEntity()
        {
            string jwt = FileManager.getItem("jwt").ToString();
            AuthEntity authEntity = new AuthEntity();
            authEntity.jwt = !string.IsNullOrEmpty(jwt) ? jwt.Substring("JWT ".Length) : jwt;
            return authEntity;
        }

        private static PluginEntity GetPluginEntity()
        {
            PluginEntity pluginEntity = new PluginEntity();
            pluginEntity.plugin_version = EnvUtil.GetVersion();
            pluginEntity.plugin_id = EnvUtil.getPluginId();
            pluginEntity.plugin_name = EnvUtil.getPluginName();
            return pluginEntity;
        }

        public static async Task<ProjectEntity> GetProjectEntity(string fileName)
        {
            FileDetails fd = await ProjectInfoManager.GetFileDatails(fileName);
            ProjectEntity projectEntity = new ProjectEntity();
            projectEntity.project_directory = fd.project_directory;
            projectEntity.project_name = fd.project_name;
            return projectEntity;
        }

        public static RepoEntity GetRepoEntity(string projectDir)
        {
            RepoResourceInfo info = GitUtil.GetResourceInfo(projectDir, false);
            RepoEntity repoEntity = new RepoEntity();
            repoEntity.git_branch = info.branch;
            repoEntity.git_tag = info.tag;
            repoEntity.owner_id = info.ownerId;
            repoEntity.repo_identifier = info.identifier;
            repoEntity.repo_name = info.repoName;
            return repoEntity;
        }

        public static async Task<FileEntity> GetFileEntity(string fileName)
        {
            FileDetails fd = await ProjectInfoManager.GetFileDatails(fileName);
            FileEntity fileEntity = new FileEntity();
            fileEntity.file_name = fd.project_file_name;
            fileEntity.file_path = fd.full_file_name;
            fileEntity.character_count = fd.character_count;
            fileEntity.line_count = fd.line_count;
            fileEntity.syntax = fd.syntax;

            return fileEntity;
        }
    }
}
