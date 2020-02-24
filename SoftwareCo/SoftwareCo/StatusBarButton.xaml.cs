using System;
using System.Windows;
using System.Windows.Controls;

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

        public void UpdateDisplay(string label, string iconName)
        {
            TimeLabel.Content = label;
            Image img = SoftwareCoUtil.CreateImage(iconName);
            TimeIcon.Source = img.Source;
        }

        private void LaunchCodeMetricsView(object sender, RoutedEventArgs args)
        {
            try
            {
                CodeMetricsTreeManager.Instance.OpenCodeMetricsPane();
            }
            catch (Exception e)
            {
                Logger.Error("Error launching the code metrics view", e);
            }
            
        }
    }
}
