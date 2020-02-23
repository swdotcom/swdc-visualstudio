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
        }
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
    }
}
