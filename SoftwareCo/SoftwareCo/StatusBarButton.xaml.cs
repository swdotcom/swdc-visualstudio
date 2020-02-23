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

        private void LaunchDashBoard(object sender, RoutedEventArgs e)
        {
            try
            {
                SoftwareCoPackage.LaunchCodeTimeDashboardAsync();
            }
            catch (Exception ex)
            {
               
            }
            
        }

        private void ColumnDefinition_TouchEnter(object sender, System.Windows.Input.TouchEventArgs e)
        {

        }
    }
}
