using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SoftwareCo
{
    /// <summary>
    /// Interaction logic for CodeMetricsTree.xaml
    /// </summary>
    public partial class CodeMetricsTree : UserControl
    {
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
        }

        public async Task RebuildMenuButtonsAsync()
        {
            // connect label
            ConnectLabel.Content = "See advanced metrics";
            ConnectImage.Source = SoftwareCoUtil.CreateImage("cpaw.png").Source;

            // dashboard label
            DashboardLabel.Content = "Generate dashboard";
            DashboardImage.Source = SoftwareCoUtil.CreateImage("dashboard.png").Source;

            // Toggle status label
            if (EventManager.Instance.IsShowingStatusText())
            {
                ToggleStatusLabel.Content = "Hide status bar metrics";
            } else
            {
                ToggleStatusLabel.Content = "Show status bar metrics";
            }
            ToggleStatusImage.Source = SoftwareCoUtil.CreateImage("visible.png").Source;

            // Learn more label
            LearnMoreLabel.Content = "Learn more";
            LearnMoreImage.Source = SoftwareCoUtil.CreateImage("readme.png").Source;

            // Feedback label
            FeedbackLabel.Content = "Submit feedback";
            FeedbackImage.Source = SoftwareCoUtil.CreateImage("message.png").Source;
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
                        } else if (iconName != null && obj is Image)
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
            SessionSummary summary = SessionSummaryManager.Instance.GetSessionSummayData();
            long wcTimeMin = WallclockManager.Instance.GetWcTimeInMinutes();

            string editortimeToday = "Today: " + SoftwareCoUtil.HumanizeMinutes(wcTimeMin);
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
                TreeViewItem editorParent = BuildMetricNodes("editortime", "Editor time", editortimeChildren);
                Editortime.Items.Add(editorParent);
            }

            string codetimeBoltIcon = summary.currentDayMinutes > summary.averageDailyMinutes ? "bolt.png" : "bolt-grey.png";
            string codetimeToday = "Today: " + SoftwareCoUtil.HumanizeMinutes(summary.currentDayMinutes);
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
                TreeViewItem codetimeParent = BuildMetricNodes("codetime", "Code time", codetimeChildren);
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
                TreeViewItem parentItem = await GetParent(Codetime, "linesremoved");
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
            if (Linesremoved.HasItems)
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
        }

        private void ConnectClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            object name = SoftwareCoUtil.getItem("name");
            if (name != null && !name.ToString().Equals(""))
            {
                // logged in
                SoftwareCoUtil.launchWebDashboard();
            }
            else
            {
                SoftwareCoUtil.launchLogin();
            }
        }

        private void DashboardClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            DashboardManager.Instance.LaunchCodeTimeDashboardAsync();
        }

        private void ToggleClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            EventManager.Instance.ToggleStatusInfo();
        }

        private void LearnMoreClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            //
        }

        private void FeedbackClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            //
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
