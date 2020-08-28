using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace SoftwareCo
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SoftwareOpenCodeMetricsTreeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4135;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>

        public static readonly Guid CommandSet = new Guid("76eda5aa-cf64-4fb5-9d52-06c48a00adbd");
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private static MenuCommand menuItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareTopFortyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SoftwareOpenCodeMetricsTreeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException("package");
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SoftwareOpenCodeMetricsTreeCommand Instance
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
                commandService.AddCommand(menuItem);
            }

            Instance = new SoftwareOpenCodeMetricsTreeCommand(package, commandService);
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
            try
            {
                PackageManager.OpenCodeMetricsPaneAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error launching the code metrics view", ex);
            }
        }
    }
}
