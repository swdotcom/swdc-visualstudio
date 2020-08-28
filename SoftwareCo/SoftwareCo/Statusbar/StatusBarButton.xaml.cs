using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SoftwareCo
{
    /// <summary>
    /// Interaction logic for StatusBarButton.xaml
    /// </summary>
    public partial class StatusBarButton : UserControl
    {

        public static Boolean showingStatusbarMetrics = true;
        public StatusBarButton()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public async Task UpdateDisplayAsync(string label, string iconName)
        {
            if (!showingStatusbarMetrics)
            {
                label = "";
                iconName = "clock.png";
            }

            if (string.IsNullOrEmpty(iconName))
            {
                iconName = "cpaw.png";
            }

            await Dispatcher.BeginInvoke(new Action(() =>
            {
                string tooltip = "Active code time today. Click to see more from Code Time.";
                string email = FileManager.getItemAsString("name");
                if (email != null)
                {
                    tooltip += " Logged in as " + email;
                }
                TimeLabel.Content = label;
                TimeLabel.ToolTip = "Code time today";

                Image img = SoftwareCoUtil.CreateImage(iconName);
                TimeIcon.Source = img.Source;
                TimeIcon.ToolTip = tooltip;
            }));
        }

        private void LaunchCodeMetricsView(object sender, RoutedEventArgs args)
        {
            try
            {
                PackageManager.OpenCodeMetricsPaneAsync();

                UIElementEntity entity = new UIElementEntity();
                entity.color = null;
                entity.element_location = "ct_menu_tree";
                entity.element_name = "ct_status_bar_metrics_btn";
                entity.cta_text = "status bar metrics";
                entity.icon_name = "clock";
                TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);
            }
            catch (Exception e)
            {
                Logger.Error("Error launching the code metrics view", e);
            }

        }
    }
}
