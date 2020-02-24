﻿using System;
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
        }

        public void RebuildMenuButtons()
        {
            // connect label
            ConnectLabel.Content = "See advanced metrics";
            ConnectImage.Source = SoftwareCoUtil.CreateImage("../Resources/cpaw.png").Source;

            // dashboard label
            DashboardLabel.Content = "Generate dashboard";
            DashboardImage.Source = SoftwareCoUtil.CreateImage("../Resources/dashboard.png").Source;

            // Toggle status label
            if (SoftwareCoPackage.IsStatusInfoShowing())
            {
                ToggleStatusLabel.Content = "Hide status bar metrics";
            } else
            {
                ToggleStatusLabel.Content = "Show status bar metrics";
            }
            ToggleStatusImage.Source = SoftwareCoUtil.CreateImage("../Resources/visible.png").Source;

            // Learn more label
            LearnMoreLabel.Content = "Learn more";
            LearnMoreImage.Source = SoftwareCoUtil.CreateImage("../Resources/readme.png").Source;

            // Feedback label
            FeedbackLabel.Content = "Submit feedback";
            FeedbackImage.Source = SoftwareCoUtil.CreateImage("../Resources/message.png").Source;
        }

        private void ConnectClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            //
        }

        private void DashboardClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            //
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
    }
}
