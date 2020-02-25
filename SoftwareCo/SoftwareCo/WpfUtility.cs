using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SoftwareCo
{
    public static class WpfUtility
    {
        public static void InvokeIfRequired(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher == null)
            {
                return;
            }
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
                return;
            }
            action();
        }
    }
}
