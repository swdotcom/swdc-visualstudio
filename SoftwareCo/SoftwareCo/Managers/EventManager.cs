﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public sealed class EventManager
    {
        private static readonly Lazy<EventManager> lazy = new Lazy<EventManager>(() => new EventManager());

        private SoftwareCoPackage package;
        private bool showingStatusText = true;

        public static EventManager Instance { get { return lazy.Value; } }

        private EventManager()
        {
            //
        }

        public void InjectAsyncPackage(SoftwareCoPackage package)
        {
            this.package = package;
        }

        public bool IsShowingStatusText()
        {
            return showingStatusText;
        }

        public void ToggleStatusInfo()
        {
            showingStatusText = !showingStatusText;
            // update the tree view text
            package.RebuildMenuButtonsAsync();

            // update the status bar info
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
        }

    }
}
