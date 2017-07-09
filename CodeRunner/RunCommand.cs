//------------------------------------------------------------------------------
// <copyright file="RunCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeRunner
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RunCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b62d762c-0f40-4249-94cb-7a09ca719bda");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private RunCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Run, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RunCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
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
        public static void Initialize(Package package)
        {
            Instance = new RunCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Run(object sender, EventArgs e)
        {
            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;
            string fileName = null;

            if (dte.ActiveWindow.Type == vsWindowType.vsWindowTypeDocument)
            {
                fileName = dte.ActiveDocument.FullName;
            }
            else if (dte.SelectedItems.Count == 1)
            {
                fileName = dte.SelectedItems?.Item(1)?.ProjectItem?.FileNames[0];
            }

            if (string.IsNullOrEmpty(fileName))
                return;

            string ext = Path.GetExtension(fileName);

            AppInsightsClient.trackEvent(ext);

            var extMapping = new Dictionary<string, string>()
            {
                {".js", "node"},
                {".php", "php"},
                {".py", "python"},
                {".pl", "perl"},
                {".rb", "ruby"},
                {".go", "go run"},
                {".lua", "lua"},
                {".groovy", "groovy"},
                {".scala", "scala"},
                {".vbs", "cscript //Nologo"}
            };

            if (!extMapping.ContainsKey(ext))
            {
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    $"The file type \"{ext}\" is not supported.",
                    "File type not supported",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var start = new ProcessStartInfo("cmd", $"/k {extMapping[ext]} {Path.GetFileName(fileName)}")
            {
                WorkingDirectory = Path.GetDirectoryName(fileName),
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            using (var proc = System.Diagnostics.Process.Start(start))
            {
                proc.WaitForExit();
            }
        }
    }
}
