using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace SoftwareCo
{
    /// <summary>
    /// Command handler for the web dashboard
    /// </summary>
    internal sealed class SoftwareLaunchCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("76eda5aa-cf64-4fb5-9d52-06c48a00adbd");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private static MenuCommand menuItem;
        private static bool loggedIn = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareLaunchCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SoftwareLaunchCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException("package");
            UpdateMenuItemVisibility();
        }

        public static async void UpdateEnabledState(bool loggedInVal)
        {
            loggedIn = loggedInVal;
            UpdateMenuItemVisibility();
        }

        private static void UpdateMenuItemVisibility()
        {
            if (menuItem != null)
            {
                if (loggedIn)
                {
                    menuItem.Enabled = true;
                    menuItem.Visible = true;
                }
                else
                {
                    menuItem.Enabled = false;
                    menuItem.Visible = false;
                }
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SoftwareLaunchCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                menuItem = new MenuCommand(Execute, menuCommandID);
                menuItem.Enabled = false;
                menuItem.Visible = false;
                commandService.AddCommand(menuItem);
            }
            Instance = new SoftwareLaunchCommand(package, commandService);
            UpdateMenuItemVisibility();
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void Execute(object sender, EventArgs e)
        {
            SoftwareCoUtil.launchWebDashboard();
        }
    }
}
