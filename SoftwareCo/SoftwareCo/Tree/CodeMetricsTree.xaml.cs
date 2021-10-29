using Slack.NetStandard.WebApi.Dnd;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SoftwareCo
{
    /// <summary>
    /// Interaction logic for CodeMetricsTree.xaml
    /// </summary>
    public partial class CodeMetricsTree : UserControl
    {

        private static IDictionary<string, bool> expandMap = new Dictionary<string, bool>();

        public CodeMetricsTree()
        {
            InitializeComponent();

            // hide the separators until the panel has rendered
            Seperator1.Visibility = Visibility.Hidden;
            Seperator2.Visibility = Visibility.Hidden;

            // update the menu buttons
            RebuildAccountButtons();

            // update the metric nodes
            RebuildFlowButtonsAsync();

            RebuildStatsButtonsAsync();
        }

        public async Task RebuildAccountButtons()
        {
            AccountPanel.Children.Clear();
            string email = FileManager.getItemAsString("name");
            if (string.IsNullOrEmpty(email))
            {
                AccountPanel.Children.Add(BuildClickLabel("SignUpPanel", "paw.png", "Sign up", SwitchAccountsClickHandler));
                AccountPanel.Children.Add(BuildClickLabel("LogInPanel", "paw.png", "Log up", SwitchAccountsClickHandler));
            } else
            {
                string authType = FileManager.getItemAsString("authType");
                string authIcon = "google.png";
                if (string.IsNullOrEmpty(authType))
                {
                    authIcon = "email.png";
                }
                else if (authType.ToLower().Equals("github"))
                {
                    authIcon = "github.png";
                }
                AccountPanel.Children.Add(BuildLabelItem("LoggedInPanel", authIcon, email));
                AccountPanel.Children.Add(BuildClickLabel("SwitchAccountPanel", "paw.png", "Switch account", SwitchAccountsClickHandler));
            }

            // Learn more label
            AccountPanel.Children.Add(BuildClickLabel("LearnMorePanel", "readme.png", "Documentation", LearnMoreClickHandler));

            // Feedback label
            AccountPanel.Children.Add(BuildClickLabel("SubmitIssuePanel", "message.png", "Submit an issue", FeedbackClickHandler));

            // Toggle status label
            string toggleStatusLabel = "Hide status bar metrics";
            if (!StatusBarButton.showingStatusbarMetrics)
            {
                toggleStatusLabel = "Show status bar metrics";
            }
            AccountPanel.Children.Add(BuildClickLabel("ToggleStatusMetricsPanel", "visible.png", toggleStatusLabel, ToggleClickHandler));

            // slack workspace tree
            SlackWorkspaceTree.Items.Clear();
            List<TreeViewItem> workspaceChildren = new List<TreeViewItem>();
            foreach (Integration workspace in SlackManager.GetSlackWorkspaces())
            {
                workspaceChildren.Add(CodeMetricsTreeProvider.BuildContextItemButton(workspace.authId, workspace.team_domain + " (" + workspace.team_name + ")", "deletion.png", RemoveWorkspaceClickHandler));
            }
            workspaceChildren.Add(CodeMetricsTreeProvider.BuildTreeItem("AddWorkspaceItem", "Add workspace", "add.png", AddWorkspaceClickHandler));
            TreeViewItem workspacesParent = BuildMetricNodes("workspaces", "Slack workspaces", workspaceChildren);
            SlackWorkspaceTree.Items.Add(workspacesParent);
        }

        public async Task RebuildFlowButtonsAsync()
        {
            Seperator1.Visibility = Visibility.Visible;

            FlowPanel.Children.Clear();

            if (FileManager.IsInFlow())
            {
                FlowPanel.Children.Add(BuildClickLabel("FlowModePanel", "dot.png", "Exit Flow Mode", ExitFlowModeHandler));
            }
            else
            {
                FlowPanel.Children.Add(BuildClickLabel("FlowModePanel", "dot-outlined.png", "Enter Flow Mode", EnterFlowModeHandler));
            }
        }

        public async Task RebuildStatsButtonsAsync() {

            Seperator2.Visibility = Visibility.Visible;

            StatsPanel.Children.Clear();

            // settings button
            StatsPanel.Children.Add(BuildClickLabel("DashboardPanel", "files.png", "Settings", SettingsClickHandler));

            // dashboard button
            StatsPanel.Children.Add(BuildClickLabel("DashboardPanel", "dashboard.png", "Dashboard Summary", DashboardClickHandler));

            // more at software button
            StatsPanel.Children.Add(BuildClickLabel("WebAnalyticsPanel", "paw.png", "More data at Software.com", LaunchWebDashboard));
        }

        private StackPanel BuildClickLabel(string panelName, string iconName, string content, MouseButtonEventHandler handler)
        {
            StackPanel panel = new StackPanel();
            panel.Name = panelName;
            panel.Orientation = Orientation.Horizontal;
            panel.Margin = new Thickness(5, 0, 0, 0);

            Image img = new Image();
            img.Width = 15;
            img.Height = 15;
            img.Source = SoftwareCoUtil.CreateImage(iconName).Source;
            panel.Children.Add(img);

            Label label = new Label();
            label.Content = content;
            if (handler != null)
            {
                label.MouseDown += handler;
            }
            label.Foreground = Brushes.DarkCyan;
            label.Background = Brushes.Transparent;
            label.BorderThickness = new Thickness(0d);
            label.Cursor = Cursors.Hand;
            panel.Children.Add(label);
            return panel;
        }

        private StackPanel BuildLabelItem(string panelName, string iconName, string content)
        {
            StackPanel panel = new StackPanel();
            panel.Name = panelName;
            panel.Orientation = Orientation.Horizontal;
            panel.Margin = new Thickness(5, 0, 0, 0);

            Image img = new Image();
            img.Width = 15;
            img.Height = 15;
            img.Source = SoftwareCoUtil.CreateImage(iconName).Source;
            panel.Children.Add(img);

            Label label = new Label();
            label.Content = content;
            label.Foreground = Brushes.DarkCyan;
            label.Background = Brushes.Transparent;
            label.BorderThickness = new Thickness(0d);
            panel.Children.Add(label);
            return panel;
        }

        private async Task<TreeViewItem> GetParent(TreeView treeView, string parentId)
        {
            foreach (CodeMetricsTreeItem item in treeView.Items)
            {
                if (item.ItemId.Equals(parentId))
                {
                    return item;
                }
            }
            return null;
        }

        private async Task UpdateNodeValue(TreeViewItem viewItem, string id, string value, string iconName = null)
        {
            if (viewItem == null)
            {
                return;
            }
            foreach (CodeMetricsTreeItem child in viewItem.Items)
            {
                if (child.ItemId.Equals(id))
                {
                    StackPanel stack = (StackPanel)child.Header;

                    foreach (object obj in stack.Children)
                    {
                        if (obj is Label)
                        {
                            ((Label)obj).Content = value;
                        }
                        else if (iconName != null && obj is Image)
                        {
                            Image img = SoftwareCoUtil.CreateImage(iconName);
                            ((Image)obj).Source = img.Source;
                        }
                    }
                }
            }
        }

        private void UpdateSlackStatusHandler(object sender, MouseButtonEventArgs args)
        {
            SlackManager.UpdateSlackStatusMessage();
        }

        private void PauseSlackNotificationsHandler(object sender, MouseButtonEventArgs args)
        {
            SlackManager.PauseSlackNotificationsAsync();
        }

        private void EnableSlackNotificationsHandler(object sender, MouseButtonEventArgs args)
        {
            SlackManager.EnableSlackNotifications();
        }

        private void SetAwayPresenceHandler(object sender, MouseButtonEventArgs args)
        {
            SlackManager.UpdateSlackPresence("away");
        }

        private void SetActivePresenceHandler(object sender, MouseButtonEventArgs args)
        {
            SlackManager.UpdateSlackPresence("auto");
        }

        private void SwitchAccountsClickHandler(object sender, MouseButtonEventArgs args)
        {
            SwitchAccountDialog dialog = new SwitchAccountDialog();
            dialog.ShowDialog();

            string authType = dialog.getSelection();
            if (!string.IsNullOrEmpty(authType))
            {
                ConnectClickHandler(authType.ToLower(), true);
            }

        }

        private void GoogleConnectClickHandler(object sender, MouseButtonEventArgs args)
        {
            ConnectClickHandler("google");
        }

        private void GitHubConnectClickHandler(object sender, MouseButtonEventArgs args)
        {
            ConnectClickHandler("github");
        }

        private void EmailConnectClickHandler(object sender, MouseButtonEventArgs args)
        {
            ConnectClickHandler("email");
        }

        private void ConnectClickHandler(string loginType, bool switch_account = false)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            SoftwareCoUtil.launchLogin(loginType, switch_account);
        }

        private void LaunchWebDashboard(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            SoftwareCoUtil.launchWebDashboard();
            UIElementEntity entity = new UIElementEntity();
            entity.color = "blue";
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_web_metrics_btn";
            entity.cta_text = "See rich data visualizations in the web app";
            entity.icon_name = "paw";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
        }

        private void ExitFlowModeHandler(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            FlowManager.Instance.DisableFlow();
        }

        private void EnterFlowModeHandler(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            FlowManager.Instance.EnableFlow(false);
        }

        private void SettingsClickHandler(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            DashboardManager.Instance.LaunchSettingsView();
        }

        private void DashboardClickHandler(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            DashboardManager.Instance.LaunchCodeTimeDashboardAsync();
            UIElementEntity entity = new UIElementEntity();
            entity.color = "white";
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_summary_btn";
            entity.cta_text = "View your summary report";
            entity.icon_name = "guage";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
        }

        private void RepoIdentifierClickHandler(object sender, MouseButtonEventArgs args)
        {
            ReportManager.Instance.DisplayProjectContributorSummaryDashboard();
        }

        public void ToggleClickHandler(object sender, MouseButtonEventArgs args)
        {
            StatusBarButton.showingStatusbarMetrics = !StatusBarButton.showingStatusbarMetrics;
            RebuildAccountButtons();
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
        }

        public void AddWorkspaceClickHandler(object sender, MouseButtonEventArgs args)
        {
            SlackManager.ConnectSlackWorkspace();
        }

        private void LearnMoreClickHandler(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            DashboardManager.Instance.LaunchReadmeFileAsync();
            UIElementEntity entity = new UIElementEntity();
            entity.color = "yellow";
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_learn_more_btn";
            entity.cta_text = "View the Code Time Readme to learn more";
            entity.icon_name = "document";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
        }

        private void FeedbackClickHandler(object sender, MouseButtonEventArgs args)
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            SoftwareCoUtil.launchMailToCody();
            UIElementEntity entity = new UIElementEntity();
            entity.color = null;
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_submit_feedback_btn";
            entity.cta_text = "Send us an email";
            entity.icon_name = "envelop";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
        }

        private void RemoveWorkspaceClickHandler(object sender, MouseButtonEventArgs args)
        {
            try
            {
                Image deleteImage = (Image)args.Source;
                if (deleteImage != null)
                {
                    SlackManager.DisconnectSlackAuth(deleteImage.Name);
                }
            }
            catch (Exception e) { };
        }

        private TreeViewItem BuildMetricNode(string id, string label, string iconName = null)
        {
            TreeViewItem item = CodeMetricsTreeProvider.BuildTreeItem(id, label, iconName);
            return item;
        }

        private TreeViewItem BuildMetricNodes(string id, string label, List<TreeViewItem> children)
        {
            TreeViewItem item = CodeMetricsTreeProvider.BuildTreeItem(id, label);
            foreach (TreeViewItem child in children)
            {
                item.Items.Add(child);
            }
            item.Expanded += OnExpanded;
            item.Collapsed += OnCollapsed;
            if (expandMap.ContainsKey(id))
            {
                item.IsExpanded = true;
            }
            return item;
        }

        private void OnExpanded(object sender, RoutedEventArgs args)
        {
            try
            {
                CodeMetricsTreeItem treeItem = (CodeMetricsTreeItem)sender;
                expandMap.Add(treeItem.ItemId, true);

            }
            catch (Exception e) { };
        }

        private void OnCollapsed(object sender, RoutedEventArgs args)
        {
            try
            {
                CodeMetricsTreeItem treeItem = (CodeMetricsTreeItem)sender;
                expandMap.Remove(treeItem.ItemId);

            }
            catch (Exception e) { };
        }
    }
}
