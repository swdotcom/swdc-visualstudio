using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace SoftwareCo
{
    /// <summary>
    /// Interaction logic for StatusBarButton.xaml
    /// </summary>
    public partial class StatusBarButton : UserControl
    {
        public StatusBarButton()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public async Task UpdateDisplayAsync(string label, string iconName)
        {
            await Dispatcher.BeginInvoke(new Action(() => {
                string tooltip = "Click to see more from Code Time.";
                string email = SoftwareCoUtil.getItemAsString("name");
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
                CodeMetricsTreeManager.Instance.OpenCodeMetricsPaneAsync();
                EventManager.Instance.CreateCodeTimeEvent("mouse", "click", "ShowTreeView");
            }
            catch (Exception e)
            {
                Logger.Error("Error launching the code metrics view", e);
            }
            
        }
    }
}
