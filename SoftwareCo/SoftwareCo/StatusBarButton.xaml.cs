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
