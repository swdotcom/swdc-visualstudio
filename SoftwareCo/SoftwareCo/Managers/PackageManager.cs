using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

namespace SoftwareCo
{
    class PackageManager
    {
        private static SoftwareCoPackage package;
        private static CodeMetricsToolPane _codeMetricsWindow;
        private static StatusBarButton _statusBarButton;
        private static bool _addedStatusBarButton = false;
        private static string _solutionDirectory = "";
        private static DTE ObjDte;

        public static void initialize(SoftwareCoPackage mainPackage, DTE dte)
        {
            package = mainPackage;
            ObjDte = dte;
            _statusBarButton = new StatusBarButton();

            // initialize the status bar before we fetch the summary data
            InitializeStatusBar();
        }

        public static async Task RebuildMenuButtonsAsync()
        {
            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildMenuButtons();
            }

            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.RebuildMenuButtons();
        }

        public static async Task RebuildCodeMetricsAsync()
        {
            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildCodeMetrics();
            }

            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.RebuildCodeMetrics();
        }

        public static async Task RebuildGitMetricsAsync()
        {
            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildGitMetricsAsync();
            }

            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.RebuildGitMetricsAsync();
        }

        public static async Task OpenCodeMetricsPaneAsync()
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            ToolWindowPane window = package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            windowFrame.Show();
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public static async Task ToggleStatusbarMetrics()
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildGitMetricsAsync();
            }

            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.ToggleClickHandler();
        }

        public static async Task UpdateStatusBarButtonText(String text, String iconName = null)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            await InitializeStatusBar();

            _statusBarButton.UpdateDisplayAsync(text, iconName);
        }

        public static async Task InitializeStatusBar()
        {
            if (_addedStatusBarButton)
            {
                return;
            }
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            DockPanel statusBarObj = FindChildControl<DockPanel>(System.Windows.Application.Current.MainWindow, "StatusBarPanel");
            if (statusBarObj != null)
            {
                statusBarObj.Children.Insert(0, _statusBarButton);
                _addedStatusBarButton = true;
            }
        }

        public static async Task<string> GetSolutionDirectory()
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
            {
                _solutionDirectory = Path.GetDirectoryName(ObjDte.Solution.FileName);
            }
            return _solutionDirectory;
        }

        public static async Task<string> GetActiveDocumentFileName() {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            return ObjDte.ActiveWindow.Document.FullName;
        }

        public static async Task<string> GetActiveDocumentSyntax()
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            return ObjDte.ActiveWindow.Document.Language;
        }

        public static T FindChildControl<T>(DependencyObject parent, string childName)
          where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                T childType = child as T;
                if (childType == null)
                {

                    foundChild = FindChildControl<T>(child, childName);


                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {

                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {

                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
    }
}
