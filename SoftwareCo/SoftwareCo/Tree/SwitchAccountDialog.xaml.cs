using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SoftwareCo
{
    /// <summary>
    /// Interaction logic for SwitchAccountDialog.xaml
    /// </summary>
    public partial class SwitchAccountDialog : Window
    {
        private string selection = null;
        private bool okSelected = false;

        public SwitchAccountDialog()
        {
            InitializeComponent();
        }

        private void selectionChanged_Click(object sender, SelectionChangedEventArgs args)
        {
            clear_errorMsg();
            this.selection = ((sender as ListBox).SelectedItem as ListBoxItem).Content.ToString();
        }

        private void clear_errorMsg()
        {
            ErrMessage.Text = "";
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.selection))
            {
                ErrMessage.Text = "Select an authentication type to continue";
                return;
            }
            // Close dialog box
            okSelected = true;
            Close();
        }

        private void window_Closing(object sender, CancelEventArgs args)
        {
            clear_errorMsg();
            // null the selection if its the dialog closing itself
            if (!okSelected)
            {
                this.selection = null;
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            clear_errorMsg();
            // Close dialog box
            this.selection = null;
            Close();
        }

        public string getSelection() {
            return this.selection;
        }
    }
}
