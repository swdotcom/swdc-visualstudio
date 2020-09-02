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

        private Image ClockImage = null;
        private Image PawImage = null;
        private Image RocketImage = null;

        public StatusBarButton()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public async Task UpdateDisplayAsync(string label, string iconName)
        {
            Image statusImg = null;
            if (PawImage == null)
            {
                PawImage = SoftwareCoUtil.CreateImage("cpaw.png");
            }

            // 3 types of images: clock, rocket, and paw
            if (!showingStatusbarMetrics)
            {
                label = "";
                iconName = "clock.png";
                if (ClockImage == null)
                {
                    ClockImage = SoftwareCoUtil.CreateImage(iconName);
                }
                statusImg = ClockImage;
            }
            else if (iconName == "rocket.png")
            {
                if (RocketImage == null)
                {
                    RocketImage = SoftwareCoUtil.CreateImage(iconName);
                }
                statusImg = RocketImage;
            }
            else
            {
                statusImg = PawImage;
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

                TimeIcon.Source = statusImg.Source;
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
