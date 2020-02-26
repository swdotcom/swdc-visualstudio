using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualStudio;
using System.Windows.Forms;
using Microsoft.VisualStudio.Threading;

namespace SoftwareCo
{
    public sealed class CodeMetricsTreeManager
    {
        private static readonly Lazy<CodeMetricsTreeManager> lazy = new Lazy<CodeMetricsTreeManager>(() => new CodeMetricsTreeManager());

        private SoftwareCoPackage package;

        public static CodeMetricsTreeManager Instance { get { return lazy.Value; } }

        private CodeMetricsTreeManager()
        {
            //
        }

        public void InjectAsyncPackage(SoftwareCoPackage package)
        {
            this.package = package;
        }

        public async void OpenCodeMetricsPaneAsync()
        {
            await package.OpenCodeMetricsPaneAsync();
        }
    }
}
