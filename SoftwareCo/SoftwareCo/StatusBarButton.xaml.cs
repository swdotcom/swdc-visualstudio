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
                TimeLabel.Content = label;
                Image img = SoftwareCoUtil.CreateImage(iconName);
                TimeIcon.Source = img.Source;
            }));
        }

        private void LaunchCodeMetricsView(object sender, RoutedEventArgs args)
        {
            try
            {
                CodeMetricsTreeManager.Instance.OpenCodeMetricsPaneAsync();
            }
            catch (Exception e)
            {
                Logger.Error("Error launching the code metrics view", e);
            }
            
        }
    }
}
