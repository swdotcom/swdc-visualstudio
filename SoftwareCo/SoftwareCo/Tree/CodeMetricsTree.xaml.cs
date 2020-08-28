using System;
using System.Collections.Generic;
using System.IO;
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
        private TreeViewItem topKeystrokesParent;
        private TreeViewItem topCodetimeParent;

        public CodeMetricsTree()
        {
            InitializeComponent();
            Init();
        }

        private async Task Init()
        {
            // update the menu buttons
            RebuildMenuButtonsAsync();
            // update the metric nodes
            RebuildCodeMetricsAsync();
            // update the git metric nodes
            RebuildGitMetricsAsync();
            // update the contributor metric nodes
            RebuildContributorMetricsAsync();
        }

        public async Task RebuildMenuButtonsAsync()
        {
            SignupPanel.Children.Clear();
            List<StackPanel> signupPanels = BuildSignupPanels();
            if (signupPanels.Count > 0)
            {
                foreach (StackPanel panel in signupPanels)
                {
                    SignupPanel.Children.Add(panel);
                }
            }

            WebDashboardLabel.Content = "See advanced metrics";
            WebDashboardImage.Source = SoftwareCoUtil.CreateImage("cpaw.png").Source;

            // dashboard label
            DashboardLabel.Content = "View summary";
            DashboardImage.Source = SoftwareCoUtil.CreateImage("dashboard.png").Source;

            // Toggle status label
            if (!StatusBarButton.showingStatusbarMetrics)
            {
                ToggleStatusLabel.Content = "Show status bar metrics";
            } else {
                ToggleStatusLabel.Content = "Hide status bar metrics";
            }

            ToggleStatusImage.Source = SoftwareCoUtil.CreateImage("visible.png").Source;

            // Learn more label
            LearnMoreLabel.Content = "Learn more";
            LearnMoreImage.Source = SoftwareCoUtil.CreateImage("readme.png").Source;

            // Feedback label
            FeedbackLabel.Content = "Submit feedback";
            FeedbackImage.Source = SoftwareCoUtil.CreateImage("message.png").Source;
        }

        private List<StackPanel> BuildSignupPanels()
        {
            List<StackPanel> panels = new List<StackPanel>();
            string email = FileManager.getItemAsString("name");
            if (email == null || email.Equals(""))
            {
                panels.Add(BuildClickLabel("GoogleSignupPanel", "google.png", "Sign up with Google", GoogleConnectClickHandler));
                panels.Add(BuildClickLabel("GitHubSignupPanel", "github.png", "Sign up with GitHub", GitHubConnectClickHandler));
                panels.Add(BuildClickLabel("EmailSignupPanel", "icons8-envelope-16.png", "Sign up using email", EmailConnectClickHandler));
            }
            return panels;
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
            label.MouseDown += handler;
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

        public async Task RebuildCodeMetricsAsync()
        {
            if (!SoftwareCoPackage.INITIALIZED)
            {
                return;
            }
            SessionSummary summary = SessionSummaryManager.Instance.GetSessionSummayData();
            CodeTimeSummary ctSummary = TimeDataManager.Instance.GetCodeTimeSummary();

            string editortimeToday = "Today: " + SoftwareCoUtil.HumanizeMinutes(ctSummary.codeTimeMinutes);
            if (Editortime.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(Editortime, "editortime");
                UpdateNodeValue(parentItem, "editortimetodayval", editortimeToday, "rocket.png");

            }
            else
            {
                List<TreeViewItem> editortimeChildren = new List<TreeViewItem>();
                editortimeChildren.Add(BuildMetricNode("editortimetodayval", editortimeToday, "rocket.png"));
                TreeViewItem editorParent = BuildMetricNodes("editortime", "Code time", editortimeChildren);
                Editortime.Items.Add(editorParent);
            }

            string codetimeBoltIcon = ctSummary.activeCodeTimeMinutes > summary.averageDailyMinutes ? "bolt.png" : "bolt-grey.png";
            string codetimeToday = "Today: " + SoftwareCoUtil.HumanizeMinutes(ctSummary.activeCodeTimeMinutes);
            string codetimeAvg = "Your average: " + SoftwareCoUtil.HumanizeMinutes(summary.averageDailyMinutes);
            string codetimeGlobal = "Global average: " + SoftwareCoUtil.HumanizeMinutes(summary.globalAverageDailyMinutes);
            if (Codetime.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(Codetime, "codetime");
                UpdateNodeValue(parentItem, "codetimetodayval", codetimeToday, "rocket.png");
                UpdateNodeValue(parentItem, "codetimeavgval", codetimeAvg, codetimeBoltIcon);
                UpdateNodeValue(parentItem, "codetimeglobalval", codetimeGlobal, "global-grey.png");
            }
            else
            {
                List<TreeViewItem> codetimeChildren = new List<TreeViewItem>();
                codetimeChildren.Add(BuildMetricNode("codetimetodayval", codetimeToday, "rocket.png"));
                codetimeChildren.Add(BuildMetricNode("codetimeavgval", codetimeAvg, codetimeBoltIcon));
                codetimeChildren.Add(BuildMetricNode("codetimeglobalval", codetimeGlobal, "global-grey.png"));
                TreeViewItem codetimeParent = BuildMetricNodes("codetime", "Active code time", codetimeChildren);
                Codetime.Items.Add(codetimeParent);
            }

            string linesaddedBoltIcon = summary.currentDayLinesAdded > summary.averageDailyLinesAdded ? "bolt.png" : "bolt-grey.png";
            string linesaddedToday = "Today: " + SoftwareCoUtil.FormatNumber(summary.currentDayLinesAdded);
            string linesaddedAvg = "Your average: " + SoftwareCoUtil.FormatNumber(summary.averageDailyLinesAdded);
            string linesaddedGlobal = "Global average: " + SoftwareCoUtil.FormatNumber(summary.globalAverageLinesAdded);
            if (Linesadded.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(Linesadded, "linesadded");
                UpdateNodeValue(parentItem, "linesaddedtodayval", linesaddedToday, "rocket.png");
                UpdateNodeValue(parentItem, "linesaddedavgval", linesaddedAvg, linesaddedBoltIcon);
                UpdateNodeValue(parentItem, "linesaddedglobalval", linesaddedGlobal, "global-grey.png");
            }
            else
            {
                List<TreeViewItem> linesaddedChildren = new List<TreeViewItem>();
                linesaddedChildren.Add(BuildMetricNode("linesaddedtodayval", linesaddedToday, "rocket.png"));
                linesaddedChildren.Add(BuildMetricNode("linesaddedavgval", linesaddedAvg, linesaddedBoltIcon));
                linesaddedChildren.Add(BuildMetricNode("linesaddedglobalval", linesaddedGlobal, "global-grey.png"));
                TreeViewItem linesaddedParent = BuildMetricNodes("linesadded", "Lines added", linesaddedChildren);
                Linesadded.Items.Add(linesaddedParent);
            }

            string linesremovedBoltIcon = summary.currentDayLinesRemoved > summary.averageDailyLinesRemoved ? "bolt.png" : "bolt-grey.png";
            string linesremovedToday = "Today: " + SoftwareCoUtil.FormatNumber(summary.currentDayLinesRemoved);
            string linesremovedAvg = "Your average: " + SoftwareCoUtil.FormatNumber(summary.averageDailyLinesRemoved);
            string linesremovedGlobal = "Global average: " + SoftwareCoUtil.FormatNumber(summary.globalAverageLinesRemoved);
            if (Linesremoved.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(Linesremoved, "linesremoved");
                UpdateNodeValue(parentItem, "linesremovedtodayval", linesremovedToday, "rocket.png");
                UpdateNodeValue(parentItem, "linesremovedavgval", linesremovedAvg, linesremovedBoltIcon);
                UpdateNodeValue(parentItem, "linesremovedglobalval", linesremovedGlobal, "global-grey.png");
            }
            else
            {
                List<TreeViewItem> linesremovedChildren = new List<TreeViewItem>();
                linesremovedChildren.Add(BuildMetricNode("linesremovedtodayval", linesremovedToday, "rocket.png"));
                linesremovedChildren.Add(BuildMetricNode("linesremovedavgval", linesremovedAvg, linesremovedBoltIcon));
                linesremovedChildren.Add(BuildMetricNode("linesremovedglobalval", linesremovedGlobal, "global-grey.png"));
                TreeViewItem linesremovedParent = BuildMetricNodes("linesremoved", "Lines removed", linesremovedChildren);
                Linesremoved.Items.Add(linesremovedParent);
            }

            string keystrokesBoltIcon = summary.currentDayLinesRemoved > summary.averageDailyLinesRemoved ? "bolt.png" : "bolt-grey.png";
            string keystrokesToday = "Today: " + SoftwareCoUtil.FormatNumber(summary.currentDayKeystrokes);
            string keystrokesAvg = "Your average: " + SoftwareCoUtil.FormatNumber(summary.averageDailyKeystrokes);
            string keystrokesGlobal = "Global average: " + SoftwareCoUtil.FormatNumber(summary.globalAverageDailyKeystrokes);
            if (Keystrokes.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(Keystrokes, "keystrokes");
                UpdateNodeValue(parentItem, "keystrokestodayval", keystrokesToday, "rocket.png");
                UpdateNodeValue(parentItem, "keystrokesavgval", keystrokesAvg, keystrokesBoltIcon);
                UpdateNodeValue(parentItem, "keystrokesglobalval", keystrokesGlobal, "global-grey.png");
            }
            else
            {
                List<TreeViewItem> keystrokeChildren = new List<TreeViewItem>();
                keystrokeChildren.Add(BuildMetricNode("keystrokestodayval", keystrokesToday, "rocket.png"));
                keystrokeChildren.Add(BuildMetricNode("keystrokesavgval", keystrokesAvg, keystrokesBoltIcon));
                keystrokeChildren.Add(BuildMetricNode("keystrokesglobalval", keystrokesGlobal, "global-grey.png"));
                TreeViewItem keystrokesParent = BuildMetricNodes("keystrokes", "Keystrokes", keystrokeChildren);
                Keystrokes.Items.Add(keystrokesParent);
            }

            // get the top keystrokes and code time files
            List<FileChangeInfo> topKeystrokeFiles = FileChangeInfoDataManager.Instance.GetTopKeystrokeFiles();
            if (topKeystrokeFiles.Count == 0)
            {
                TopKeystrokeFiles.Visibility = Visibility.Hidden;
            }
            else
            {
                TopKeystrokeFiles.Visibility = Visibility.Visible;
                // add or update

                if (TopKeystrokeFiles.HasItems)
                {
                    /**
                    TreeViewItem parentItem = await GetParent(TopKeystrokeFiles, "topcodetimefiles");
                    foreach (FileChangeInfo changeInfo in topKeystrokeFiles)
                    {
                        string keystrokeNumStr = SoftwareCoUtil.FormatNumber(changeInfo.keystrokes);
                        string label = changeInfo.name + " | " + keystrokeNumStr;
                        UpdateNodeValue(parentItem, "topkeystrokes-" + changeInfo.name, label, "files.png");
                    }
                    **/
                }
                List<TreeViewItem> topKeystrokeChildren = new List<TreeViewItem>();
                foreach (FileChangeInfo changeInfo in topKeystrokeFiles)
                {
                    string keystrokeNumStr = SoftwareCoUtil.FormatNumber(changeInfo.keystrokes);
                    string label = changeInfo.name + " | " + keystrokeNumStr;
                    topKeystrokeChildren.Add(BuildMetricNode("topkeystrokes-" + changeInfo.name, label, "files.png"));
                }
                if (topKeystrokesParent == null)
                {
                    topKeystrokesParent = BuildMetricNodes("topkeystrokesfiles", "Top files by keystrokes", topKeystrokeChildren);
                    TopKeystrokeFiles.Items.Add(topKeystrokesParent);
                }
                else
                {
                    topKeystrokesParent.Items.Clear();
                    foreach (TreeViewItem item in topKeystrokeChildren)
                    {
                        topKeystrokesParent.Items.Add(item);
                    }
                }

            }
            List<FileChangeInfo> topCodetimeFiles = FileChangeInfoDataManager.Instance.GetTopCodeTimeFiles();
            if (topCodetimeFiles.Count == 0)
            {
                TopCodeTimeFiles.Visibility = Visibility.Hidden;
            }
            else
            {
                TopCodeTimeFiles.Visibility = Visibility.Visible;
                // add or update

                if (TopCodeTimeFiles.HasItems)
                {
                    /**
                    TreeViewItem parentItem = await GetParent(TopCodeTimeFiles, "topcodetimefiles");
                    foreach (FileChangeInfo changeInfo in topCodetimeFiles)
                    {
                        string codetimeMinStr = SoftwareCoUtil.HumanizeMinutes(changeInfo.duration_seconds / 60);
                        string label = changeInfo.name + " | " + codetimeMinStr;
                        UpdateNodeValue(parentItem, "topcodetime-" + changeInfo.name, label, "files.png");
                    }
                    **/
                }
                List<TreeViewItem> topCodetimeFilesChildren = new List<TreeViewItem>();
                foreach (FileChangeInfo changeInfo in topCodetimeFiles)
                {
                    string codetimeMinStr = SoftwareCoUtil.HumanizeMinutes(changeInfo.duration_seconds / 60);
                    string label = changeInfo.name + " | " + codetimeMinStr;
                    topCodetimeFilesChildren.Add(BuildMetricNode("topcodetime-" + changeInfo.name, label, "files.png"));
                }
                if (topCodetimeParent == null)
                {
                    topCodetimeParent = BuildMetricNodes("topcodetimefiles", "Top files by code time", topCodetimeFilesChildren);
                    TopCodeTimeFiles.Items.Add(topCodetimeParent);
                }
                else
                {
                    topCodetimeParent.Items.Clear();
                    foreach (TreeViewItem item in topCodetimeFilesChildren)
                    {
                        topCodetimeParent.Items.Add(item);
                    }
                }

            }
        }

        public async Task RebuildContributorMetricsAsync()
        {
            string dir = await PackageManager.GetSolutionDirectory();

            RepoResourceInfo resourceInfo = GitUtilManager.GetResourceInfo(dir, true);

            // clear the children
            ContributorsMetricsPanel.Children.Clear();

            if (resourceInfo != null && resourceInfo.identifier != null)
            {
                StackPanel identifierPanel = BuildClickLabel("IdentifierPanel", "github.png", resourceInfo.identifier, RepoIdentifierClickHandler);
                ContributorsMetricsPanel.Children.Add(identifierPanel);

                // build the repo contributors
            }
        }

        public async Task RebuildGitMetricsAsync()
        {

            string dir = await PackageManager.GetSolutionDirectory();

            string name = "";
            try
            {
                FileInfo fi = new FileInfo(dir);
                name = fi.Name;
            }
            catch (Exception e)
            {
                //
            }

            CommitChangeStats uncommited = GitUtilManager.GetUncommitedChanges(dir);
            string uncommittedInsertions = "Insertion(s): " + uncommited.insertions;
            string uncommittedDeletions = "Deletion(s): " + uncommited.deletions;
            if (Uncommitted.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(Uncommitted, "uncommitted");
                UpdateNodeValue(parentItem, "uncommittedinsertions", uncommittedInsertions, "insertion.png");
                UpdateNodeValue(parentItem, "uncommitteddeletions", uncommittedDeletions, "deletion.png");
            }
            else
            {
                List<TreeViewItem> uncommitedChilren = new List<TreeViewItem>();
                uncommitedChilren.Add(BuildMetricNode("uncommittedinsertions", uncommittedInsertions, "insertion.png"));
                uncommitedChilren.Add(BuildMetricNode("uncommitteddeletions", uncommittedDeletions, "deletion.png"));
                TreeViewItem uncommittedParent = BuildMetricNodes("uncommitted", "Open changes", uncommitedChilren);
                Uncommitted.Items.Add(uncommittedParent);
            }

            string email = GitUtilManager.GetUsersEmail(dir);
            CommitChangeStats todaysStats = GitUtilManager.GetTodaysCommits(dir, email);
            string committedInsertions = "Insertion(s): " + todaysStats.insertions;
            string committedDeletions = "Deletion(s): " + todaysStats.deletions;
            string committedCount = "Commit(s): " + todaysStats.commitCount;
            string committedFilecount = "Files changed: " + todaysStats.fileCount;
            if (CommittedToday.HasItems)
            {
                // update
                TreeViewItem parentItem = await GetParent(CommittedToday, "committed");
                UpdateNodeValue(parentItem, "committedinsertions", committedInsertions, "insertion.png");
                UpdateNodeValue(parentItem, "committeddeletions", committedDeletions, "deletion.png");
                UpdateNodeValue(parentItem, "committedcount", committedCount, "commit.png");
                UpdateNodeValue(parentItem, "committedfilecount", committedFilecount, "files.png");
            }
            else
            {
                List<TreeViewItem> committedChilren = new List<TreeViewItem>();
                committedChilren.Add(BuildMetricNode("committedinsertions", committedInsertions, "insertion.png"));
                committedChilren.Add(BuildMetricNode("committeddeletions", committedDeletions, "deletion.png"));
                committedChilren.Add(BuildMetricNode("committedcount", committedCount, "commit.png"));
                committedChilren.Add(BuildMetricNode("committedfilecount", committedFilecount, "files.png"));
                TreeViewItem committedParent = BuildMetricNodes("committed", "Committed today", committedChilren);
                CommittedToday.Items.Add(committedParent);
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

        private void ConnectClickHandler(string loginType)
        {
            SoftwareCoUtil.launchLogin(loginType);
        }

        private void LaunchWebDashboard(object sender, MouseButtonEventArgs args)
        {
            SoftwareCoUtil.launchWebDashboard();
            UIElementEntity entity = new UIElementEntity();
            entity.color = "blue";
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_web_metrics_btn";
            entity.cta_text = "See rich data visualizations in the web app";
            entity.icon_name = "paw";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
        }

        private void DashboardClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
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

        public void ToggleClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            StatusBarButton.showingStatusbarMetrics = !StatusBarButton.showingStatusbarMetrics;
            RebuildMenuButtonsAsync();
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
        }

        private void LearnMoreClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            DashboardManager.Instance.LaunchReadmeFileAsync();
            UIElementEntity entity = new UIElementEntity();
            entity.color = "yellow";
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_learn_more_btn";
            entity.cta_text = "View the Code Time Readme to learn more";
            entity.icon_name = "document";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
        }

        private void FeedbackClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            SoftwareCoUtil.launchMailToCody();
            UIElementEntity entity = new UIElementEntity();
            entity.color = null;
            entity.element_location = "ct_menu_tree";
            entity.element_name = "ct_submit_feedback_btn";
            entity.cta_text = "Send us an email";
            entity.icon_name = "envelop";
            TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
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
            return item;
        }
    }
}
