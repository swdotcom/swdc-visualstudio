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

        private void Init()
        {
            // update the menu buttons
            RebuildMenuButtons();
            // update the metric nodes
            RebuildMetricNodes();
        }

        public void RebuildMenuButtons()
        {
            // connect label
            ConnectLabel.Content = "See advanced metrics";
            ConnectImage.Source = SoftwareCoUtil.CreateImage("cpaw.png").Source;

            // dashboard label
            DashboardLabel.Content = "Generate dashboard";
            DashboardImage.Source = SoftwareCoUtil.CreateImage("dashboard.png").Source;

            // Toggle status label
            if (SoftwareCoPackage.IsStatusInfoShowing())
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

        public void RebuildMetricNodes()
        {
            SessionSummary summary = SessionSummaryManager.Instance.GetSessionSummayData();
            long wcTimeMin = WallclockManager.Instance.GetWcTimeInMinutes();

            List<TreeViewItem> editortimeChildren = new List<TreeViewItem>();
            editortimeChildren.Add(BuildMetricNode("editortimetodayval", "Today: " + SoftwareCoUtil.HumanizeMinutes(wcTimeMin), "rocket.png"));
            TreeViewItem editorParent = BuildMetricNodes("editortime", "Editor time", editortimeChildren);
            Editortime.Items.Add(editorParent);

            List<TreeViewItem> codetimeChildren = new List<TreeViewItem>();
            codetimeChildren.Add(BuildMetricNode("codetimetodayval", "Today: " + SoftwareCoUtil.HumanizeMinutes(summary.currentDayMinutes), "rocket.png"));
            string codetimeBoltIcon = summary.currentDayMinutes > summary.averageDailyMinutes ? "bolt.png" : "bolt-grey.png";
            codetimeChildren.Add(BuildMetricNode("codetimeavgval", "Your average: " + SoftwareCoUtil.HumanizeMinutes(summary.averageDailyMinutes), codetimeBoltIcon));
            codetimeChildren.Add(BuildMetricNode("codetimeglobalval", "Global average: " + SoftwareCoUtil.HumanizeMinutes(summary.globalAverageDailyMinutes), "global-grey.png"));
            TreeViewItem codetimeParent = BuildMetricNodes("codetime", "Code time", codetimeChildren);
            Codetime.Items.Add(codetimeParent);

            List<TreeViewItem> linesaddedChildren = new List<TreeViewItem>();
            linesaddedChildren.Add(BuildMetricNode("linesaddedtodayval", "Today: " + summary.currentDayLinesAdded.ToString("F"), "rocket.png"));
            string linesaddedBoltIcon = summary.currentDayLinesAdded > summary.averageDailyLinesAdded ? "bolt.png" : "bolt-grey.png";
            linesaddedChildren.Add(BuildMetricNode("linesaddedavgval", "Your average: " + summary.averageDailyLinesAdded.ToString("F"), linesaddedBoltIcon));
            linesaddedChildren.Add(BuildMetricNode("linesaddedglobalval", "Global average: " + summary.globalAverageLinesAdded.ToString("F"), "global-grey.png"));
            TreeViewItem linesaddedParent = BuildMetricNodes("linesadded", "Lines added", linesaddedChildren);
            Linesadded.Items.Add(linesaddedParent);

            List<TreeViewItem> linesremovedChildren = new List<TreeViewItem>();
            linesremovedChildren.Add(BuildMetricNode("linesremovedtodayval", "Today: " + summary.currentDayLinesRemoved.ToString("F"), "rocket.png"));
            string linesremovedBoltIcon = summary.currentDayLinesRemoved > summary.averageDailyLinesRemoved ? "bolt.png" : "bolt-grey.png";
            linesremovedChildren.Add(BuildMetricNode("linesremovedavgval", "Your average: " + summary.averageDailyLinesRemoved.ToString("F"), linesremovedBoltIcon));
            linesremovedChildren.Add(BuildMetricNode("linesremovedglobalval", "Global average: " + summary.globalAverageLinesRemoved.ToString("F"), "global-grey.png"));
            TreeViewItem linesremovedParent = BuildMetricNodes("linesremoved", "Lines removed", linesremovedChildren);
            Linesremoved.Items.Add(linesremovedParent);

            List<TreeViewItem> keystrokeChildren = new List<TreeViewItem>();
            keystrokeChildren.Add(BuildMetricNode("keystrokestodayval", "Today: " + summary.currentDayKeystrokes.ToString("F"), "rocket.png"));
            string keystrokesBoltIcon = summary.currentDayKeystrokes > summary.averageDailyKeystrokes ? "bolt.png" : "bolt-grey.png";
            keystrokeChildren.Add(BuildMetricNode("keystrokesavgval", "Your average: " + summary.averageDailyKeystrokes.ToString("F"), keystrokesBoltIcon));
            keystrokeChildren.Add(BuildMetricNode("keystrokesglobalval", "Global average: " + summary.globalAverageDailyKeystrokes.ToString("F"), "global-grey.png"));
            TreeViewItem keystrokesParent = BuildMetricNodes("keystrokes", "Keystrokes", keystrokeChildren);
            Keystrokes.Items.Add(keystrokesParent);
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
            SoftwareCoPackage.ToggleStatusInfo();
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
