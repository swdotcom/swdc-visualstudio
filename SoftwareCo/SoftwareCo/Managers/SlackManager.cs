using Slack.NetStandard;
using Slack.NetStandard.WebApi;
using Slack.NetStandard.WebApi.Dnd;
using Slack.NetStandard.WebApi.Users;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoftwareCo
{
    class SlackManager
    {

        private static DndStatus slackDndStatus;
        private static string slackPresence;
        private static string slackStatusText;
        private static int connectTryCount = 0;

        public static void clearSlackInfoCache()
        {
            slackDndStatus = null;
            slackPresence = null;
            slackStatusText = null;
        }

        public static List<Integration> GetSlackWorkspaces()
        {
            List<Integration> workspaces = new List<Integration>();
            foreach (Integration integration in FileManager.GetIntegrations())
            {
                if (integration.status.ToLower().Equals("active") && integration.name.ToLower().Equals("slack"))
                {
                    workspaces.Add(integration);
                }
            }
            return workspaces;
        }

        public static Integration GetSlackWorkspace(string authId)
        {
            foreach (Integration integration in GetSlackWorkspaces())
            {
                if (integration.authId.Equals(authId))
                {
                    return integration;
                }
            }
            return null;
        }

        public static bool HasSlackWorkspaces()
        {
            return (GetSlackWorkspaces().Count > 0);
        }

        public static void ConnectSlackWorkspace()
        {
            bool isRegistered = CheckRegistration(true);
            if (!isRegistered)
            {
                return;
            }

            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("plugin", "codetime");
            jsonObj.Add("plugin_uuid", FileManager.getPluginUuid());
            jsonObj.Add("pluginVersion", EnvUtil.GetVersion());
            jsonObj.Add("plugin_id", EnvUtil.getPluginId());
            jsonObj.Add("auth_callback_state", FileManager.getAuthCallbackState(true));
            jsonObj.Add("integrate", "slack");

            StringBuilder sb = new StringBuilder();
            // create the query string from the json object
            foreach (KeyValuePair<string, object> kvp in jsonObj)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(kvp.Key).Append("=").Append(System.Web.HttpUtility.UrlEncode(kvp.Value.ToString(), System.Text.Encoding.UTF8));
            }

            string url = Constants.api_endpoint + "/auth/slack?" + sb.ToString();
            Process.Start(url);

            if (connectTryCount > 0)
            {
                // it's currently fetching, raise it to the standard 40
                connectTryCount = 40;
            } else
            {
                Task.Delay(1000 * 12).ContinueWith((task) => { RefetchSlackConnectStatusLazily(40); });
            }
        }

        public async static Task<bool> GetSlackAuth()
        {
            bool foundNewIntegration = false;
            UserState userState = await SoftwareUserManager.GetRegistrationState(true);
            if (userState.user != null && userState.user.integrations != null && userState.user.integrations.Count > 0)
            {
                List<Integration> currentIntegrations = FileManager.GetIntegrations();
                foreach (Integration integration in userState.user.integrations)
                {
                    if (integration.name.ToLower().Equals("slack") && integration.status.ToLower().Equals("active")) {
                        Integration foundIntegration = currentIntegrations.Find(delegate (Integration n) { return n.authId.Equals(integration.authId); });
                        if (foundIntegration == null)
                        {
                            // get the identity info
                            SlackWebApiClient slackClient = new SlackWebApiClient(integration.access_token);
                            UserIdentityResponse identity = await slackClient.Users.Identity();
                            integration.team_domain = (string)identity.Team.OtherFields["domain"];
                            integration.team_name = identity.Team.Name;
                            integration.integration_id = identity.User.ID;
                            currentIntegrations.Add(integration);
                            FileManager.syncIntegrations(currentIntegrations);
                            foundNewIntegration = true;
                        }
                    }
                }
            }

            return foundNewIntegration;
        }

        public static void DisconnectSlackAuth(string authId)
        {
            Integration foundIntegration = GetSlackWorkspaces().Find(delegate (Integration n) { return n.authId.Equals(authId); });
            if (foundIntegration == null)
            {
                Logger.Warning("Unable to find slack workspace to disconnect");
            }
            string msg = "Are you sure you would like to disconnect the '" + foundIntegration.team_domain + "' Slack workspace?";
            DialogResult res = MessageBox.Show(msg, "Disconnect Slack", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            if (res == DialogResult.Yes)
            {
                string jwt = FileManager.getItemAsString("jwt");
                JsonObject jsonObj = new JsonObject();
                jsonObj.Add("authId", foundIntegration.authId);
                SoftwareHttpManager.SendRequestAsync(HttpMethod.Put, "/auth/slack/disconnect", jsonObj.ToString(), jwt);

                // remove it from the integrations list
                List<Integration> newList = new List<Integration>();
                foreach (Integration integration in FileManager.GetIntegrations())
                {
                    if (!integration.authId.Equals(authId))
                    {
                        newList.Add(integration);
                    }
                }
                FileManager.syncIntegrations(newList);

                clearSlackInfoCache();
                PackageManager.RebuildTreeAsync();
            }
        }

        public static async Task PauseSlackNotificationsAsync()
        {
            bool isRegistered = CheckRegistration(true);
            if (!isRegistered)
            {
                return;
            }

            bool isConnected = CheckSlackConnection(true);
            if (!isConnected)
            {
                return;
            }

            bool updated = false;
            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                DndStatus status = await slackClient.Dnd.SetSnooze(120);
                if (status != null && status.OK)
                {
                    updated = true;
                }
            }

            if (updated)
            {
                string msg = "Slack notifications are pause for 2 hours";
                const string caption = "Code Time";
                Task.Delay(0).ContinueWith((task) => { MessageBox.Show(msg, caption, MessageBoxButtons.OK); });

                slackDndStatus = null;
                PackageManager.RebuildFlowButtons();
            }
        }

        public static async Task EnableSlackNotifications()
        {
            bool isRegistered = CheckRegistration(true);
            if (!isRegistered)
            {
                return;
            }

            bool isConnected = CheckSlackConnection(true);
            if (!isConnected)
            {
                return;
            }

            bool updated = false;
            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                DndStatus status = await slackClient.Dnd.EndSnooze();
                if (status != null && status.OK)
                {
                    updated = true;
                }
            }

            if (updated)
            {
                string msg = "Slack notifications enabled";
                const string caption = "Code Time";
                Task.Delay(0).ContinueWith((task) => { MessageBox.Show(msg, caption, MessageBoxButtons.OK); });

                slackDndStatus = null;
                PackageManager.RebuildFlowButtons();
            }
        }

        public static async Task<DndStatus> GetSlackDndInfo()
        {
            if (slackDndStatus != null)
            {
                return slackDndStatus;
            }
            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                string integration_id = workspace.integration_id;
                if (string.IsNullOrEmpty(integration_id))
                {
                    integration_id = await UpdateWorkspaceIntegrationId(workspace);
                }
                slackDndStatus = await slackClient.Dnd.Info(integration_id);
                if (slackDndStatus != null && slackDndStatus.OK)
                {
                    break;
                }
            }
            return slackDndStatus;
        }

        // returns "active" or "away"
        public static async Task<string> GetSlackPresence()
        {
            if (slackPresence != null)
            {
                return slackPresence;
            }

            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                string integration_id = workspace.integration_id;
                if (string.IsNullOrEmpty(integration_id))
                {
                    integration_id = await UpdateWorkspaceIntegrationId(workspace);
                }
                PresenceResponse resp = await slackClient.Users.GetPresence(integration_id);
                if (resp != null && resp.OK)
                {
                    slackPresence = resp.Presence;
                    break;
                }
            }
            return slackPresence;
        }

        // accepts "auto" or "away"
        public static async Task UpdateSlackPresence(string presence)
        {
            bool isRegistered = CheckRegistration(true);
            if (!isRegistered)
            {
                return;
            }

            bool isConnected = CheckSlackConnection(true);
            if (!isConnected)
            {
                return;
            }

            bool updated = false;
            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                WebApiResponse resp = await slackClient.Users.SetPresence(presence);
                if (resp != null && resp.OK)
                {
                    updated = true;
                }
            }

            if (updated)
            {
                string msg = "Slack presence updated";
                const string caption = "Code Time";
                Task.Delay(0).ContinueWith((task) => { MessageBox.Show(msg, caption, MessageBoxButtons.OK); });

                slackPresence = null;
                PackageManager.RebuildFlowButtons();
            }
        }

        public static async Task UpdateSlackStatusMessage()
        {
            bool isRegistered = CheckRegistration(true);
            if (!isRegistered)
            {
                return;
            }

            bool isConnected = CheckSlackConnection(true);
            if (!isConnected)
            {
                return;
            }

            string[] options = new string[] { "Clear status message", "Update state message"};
            OptionListDialog dialog = new OptionListDialog(options);
            dialog.ShowDialog();

            string selection = dialog.getSelection();

            string message = "";
            string value = "";
            if (selection.Contains("Update"))
            {
                DialogResult res = InputBox.Show("Slack status", "Enter a message to appear in your Slack profile status", ref value);
                if (res == DialogResult.OK)
                {
                    message = value;
                }
            }

            bool updated = false;
            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                UserProfileSetRequest req = new UserProfileSetRequest();
                UserProfileSet profile = new UserProfileSet();
                profile.StatusText = message;
                profile.StatusEmoji = "";
                if (!string.IsNullOrEmpty(message))
                {
                    profile.StatusExpiration = 0;
                }
                req.Profile = profile;
                UserProfileResponse resp = await slackClient.Users.Profile.Set(req);
                if (resp != null && resp.OK)
                {
                    updated = true;
                }
            }

            if (updated)
            {
                string msg = "Slack status message updated";
                const string caption = "Code Time";
                Task.Delay(0).ContinueWith((task) => { MessageBox.Show(msg, caption, MessageBoxButtons.OK); });

                slackStatusText = null;
                PackageManager.RebuildFlowButtons();
            }
        }

        public static async Task<string> GetSlackStatusMessage()
        {
            if (slackStatusText != null)
            {
                return slackStatusText;
            }

            foreach (Integration workspace in GetSlackWorkspaces())
            {
                SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
                UserProfileResponse resp = await slackClient.Users.Profile.Get();
                if (resp != null && resp.OK)
                {
                    slackStatusText = resp.Profile.StatusText;
                    break;
                }
            }

            return slackStatusText;
        }

        // -----------------------------------------------------------
        // PRIVATE METHODS
        // -----------------------------------------------------------

        private static async void RefetchSlackConnectStatusLazily(int try_count)
        {
            connectTryCount = try_count;
            bool foundNewIntegration = await GetSlackAuth();
            if (!foundNewIntegration)
            {
                if (connectTryCount > 0)
                {
                    connectTryCount -= 1;
                    Task.Delay(1000 * 10).ContinueWith((task) => { RefetchSlackConnectStatusLazily(connectTryCount); });
                } else
                {
                    FileManager.setAuthCallbackState(null);
                    connectTryCount = 0;
                }
            } else
            {
                FileManager.setAuthCallbackState(null);
                connectTryCount = 0;

                string msg = "Successfully connected to Slack.";
                const string caption = "Code Time";
                Task.Delay(0).ContinueWith((task) => { MessageBox.Show(msg, caption, MessageBoxButtons.OK); });

                // refresh the tree view
                clearSlackInfoCache();
                PackageManager.RebuildTreeAsync();
            }
        }

        private static bool CheckRegistration(bool showSignup)
        {
            string name = FileManager.getItemAsString("name");
            if (string.IsNullOrEmpty(name))
            {
                // the user is not registerd
                if (showSignup)
                {
                    _ = Task.Delay(0).ContinueWith((task) => { InitiateSignupFlow(); });
                }
                return false;
            }
            return true;
        }

        private static bool CheckSlackConnection(bool showConnect)
        {
            if (!HasSlackWorkspaces())
            {
                if (showConnect)
                {
                    _ = Task.Delay(0).ContinueWith((task) => { InitiateSlackConnectFlow(); });
                }
                return false;
            }
            return true;
        }

        private static void InitiateSignupFlow()
        {
            string msg = "Connecting Slack requires a registered account. Sign up or log in to continue.";
            DialogResult res = MessageBox.Show(msg, "Registration", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            if (res == DialogResult.OK)
            {
                // show the sign up flow
                SwitchAccountDialog dialog = new SwitchAccountDialog();
                dialog.ShowDialog();

                string authType = dialog.getSelection();
                if (!string.IsNullOrEmpty(authType))
                {
                    SoftwareCoUtil.launchLogin(authType.ToLower(), true);
                }
            }
        }

        private static void InitiateSlackConnectFlow()
        {
            string msg = "Connect a Slack workspace to continue.";
            DialogResult res = MessageBox.Show(msg, "Slack connect", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            if (res == DialogResult.OK)
            {
                // show the sign up flow
                SwitchAccountDialog dialog = new SwitchAccountDialog();
                dialog.ShowDialog();

                string authType = dialog.getSelection();
                if (!string.IsNullOrEmpty(authType))
                {
                    ConnectSlackWorkspace();
                }
            }
        }

        private async static Task<string> UpdateWorkspaceIntegrationId(Integration workspace)
        {
            string integration_id = "";
            List<Integration> integrations = FileManager.GetIntegrations();

            SlackWebApiClient slackClient = new SlackWebApiClient(workspace.access_token);
            UserIdentityResponse identityResp = await slackClient.Users.Identity();
            if (identityResp != null && identityResp.User != null)
            {
                integration_id = identityResp.User.ID;
                foreach (Integration integration in integrations)
                {
                    if (integration.authId.Equals(workspace.authId))
                    {
                        integration.integration_id = integration_id;
                        FileManager.syncIntegrations(integrations);
                        break;
                    }
                }
            }

            return integration_id;
        }
    }

}
