using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
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
        }

        public static SoftwareCoPackage GetAsyncPackage()
        {
            return package;
        }

        [STAThread]
        public static async Task RebuildTreeAsync()
        {
            if (package == null || !SoftwareCoPackage.INITIALIZED)
            {
                return;
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
                if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
                {
                    _codeMetricsWindow.RebuildTree();
                }
            }
            catch (Exception) { }
        }

        [STAThread]
        public static async Task RebuildFlowButtons()
        {
            if (package == null || !SoftwareCoPackage.INITIALIZED)
            {
                return;
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
                if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
                {
                    _codeMetricsWindow.RebuildFlowButtons();
                }
            }
            catch (Exception) { }
        }

        [STAThread]
        public static async Task OpenCodeMetricsPaneAsync()
        {
            if (package == null || !SoftwareCoPackage.INITIALIZED)
            {
                return;
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                ToolWindowPane window = package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
                if (window != null && window.Frame != null)
                {
                    IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                    windowFrame.Show();
                }
            }
            catch (Exception) { }
        }

        [STAThread]
        public static async Task ToggleStatusbarMetrics()
        {
            if (package == null || !SoftwareCoPackage.INITIALIZED)
            {
                return;
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                _codeMetricsWindow = (CodeMetricsToolPane)package.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
                if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
                {
                    _codeMetricsWindow.ToggleClickHandler();
                }
            }
            catch (Exception) { }
        }

        [STAThread]
        public static async Task UpdateStatusBarButtonText(string text, string iconName = null)
        {
            if (package == null || !SoftwareCoPackage.INITIALIZED)
            {
                return;
            }

            try {
                if (!_addedStatusBarButton)
                {
                    await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    // initialize it
                    await InitializeStatusBar();
                }
                if (_statusBarButton != null)
                {
                    _statusBarButton.UpdateDisplayAsync(text, iconName);
                }
            } catch (Exception)
            {
            }
        }

        [STAThread]
        public static async Task InitializeStatusBar()
        {

            if (package == null || _statusBarButton != null || _addedStatusBarButton)
            {
                return;
            }

            if (_statusBarButton == null)
            {
                _statusBarButton = new StatusBarButton();
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                DockPanel statusBarObj = FindChildControl<DockPanel>(Application.Current.MainWindow, "StatusBarPanel");
                if (statusBarObj != null)
                {
                    statusBarObj.Children.Insert(0, _statusBarButton);
                    _addedStatusBarButton = true;
                }
            }
            catch (Exception e)
            {
                Logger.Warning("Error initializing code time status bar metrics: " + e.Message);
            }
        }

        public static async Task<string> GetSolutionDirectory()
        {
            if (!string.IsNullOrEmpty(_solutionDirectory))
            {
                return _solutionDirectory;
            }

            if (package == null || ObjDte == null)
            {
                return "";
            }
            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
                {
                    _solutionDirectory = ObjDte.Solution.FullName;
                    if (_solutionDirectory.LastIndexOf(".sln") == _solutionDirectory.Length - ".sln".Length)
                    {
                        _solutionDirectory = _solutionDirectory.Substring(0, _solutionDirectory.LastIndexOf("\\"));
                    }
                }

                return _solutionDirectory;
            } catch (Exception) { }

            return "";
        }

        public static async Task<Document> GetActiveDocument()
        {
            if (package == null)
            {
                return null;
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (ObjDte != null && ObjDte.ActiveWindow != null)
                {
                    return ObjDte.ActiveWindow.Document;
                }
            }
            catch (Exception) { }
            return null;
        }

        public static async Task<string> GetActiveDocumentFileName()
        {

            if (package == null)
            {
                return "";
            }
            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (ObjDte != null && ObjDte.ActiveWindow != null && ObjDte.ActiveWindow.Document != null)
                {
                    return ObjDte.ActiveWindow.Document.FullName;
                }
            }
            catch (Exception) { }
            return "";
        }

        public static async Task<string> GetActiveDocumentSyntax()
        {
            if (package == null)
            {
                return "";
            }

            try
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (ObjDte != null && ObjDte.ActiveWindow != null && ObjDte.ActiveWindow.Document != null)
                {
                    return ObjDte.ActiveWindow.Document.Language;
                }
            }
            catch (Exception) { }
            return "";
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
