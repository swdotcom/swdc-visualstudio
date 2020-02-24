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

        public string TimeLabel { get; set; }

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

        private void ColumnDefinition_TouchEnter(object sender, System.Windows.Input.TouchEventArgs e)
        {

        }
    }
}
