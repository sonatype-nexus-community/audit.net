//------------------------------------------------------------------------------
// <copyright file="VSPackage1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Threading;

namespace NugetAuditor.VSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(GuidList.guidAuditPkgString)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : Package
    {
        private static VSPackage _instance;

        private NugetAuditManager _auditManager;

        private SynchronizationContext _uiCtx;
       
        private uint _solutionNotBuildingAndNotDebuggingContextCookie;

        private IVsMonitorSelection _vsMonitorSelection;

        internal IVsMonitorSelection MonitorSelection
        {
            get
            {
                if (this._vsMonitorSelection == null)
                {
                    this._vsMonitorSelection = ServiceLocator.GetInstance<IVsMonitorSelection>();

                    if (this._vsMonitorSelection == null)
                    {
                        throw new InvalidOperationException(string.Format(Properties.Resources.Culture, Properties.Resources.General_MissingService, typeof(IVsMonitorSelection).FullName));
                    }
                }
                return this._vsMonitorSelection;
            }
        }

        internal NugetAuditManager AuditManager
        {
            get
            {
                if (this._auditManager == null)
                {
                    this._auditManager = new NugetAuditManager(this);
                }
                return this._auditManager;
            }
        }

        public static void AssertOnMainThread()
        {
            ThreadHelper.ThrowIfNotOnUIThread("AssertOnMainThread");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VSPackage"/> class.
        /// </summary>
        public VSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.

            _instance = this;

            ServiceLocator.InitializePackageServiceProvider(this);
        }

        public static VSPackage Instance
        {
            get { return _instance; }
        }

        internal SynchronizationContext UICtx
        {
            get
            {
                return _uiCtx;
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _uiCtx = SynchronizationContext.Current;

            // get the solution not building and not debugging cookie
            Guid guid = VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid;
            MonitorSelection.GetCmdUIContextCookie(ref guid, out _solutionNotBuildingAndNotDebuggingContextCookie);

            AuditManager.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();
        }

        private void AddMenuCommandHandlers()
        {
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (mcs != null)
            {
                // menu command for "Audit NuGet packages"
                CommandID auditPackagesCommandID = new CommandID(GuidList.guidAuditCmdSet, PkgCmdIDList.cmdidAuditPackages);
                var auditPackagesCommand = new OleMenuCommand(InvokeAuditPackagesHandler, null, BeforeQueryStatusForAuditPackages, auditPackagesCommandID);
                // '$' - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                auditPackagesCommand.ParametersDescription = "$";
                mcs.AddCommand(auditPackagesCommand);

                // menu command for "Audit NuGet packages for solution"
                CommandID auditPackagesForSolutionCommandID = new CommandID(GuidList.guidAuditCmdSet, PkgCmdIDList.cmdidAuditPackagesForSolution);
                var auditPackagesForSolutionCommand = new OleMenuCommand(InvokeAuditPackagesForSolutionHandler, null, BeforeQueryStatusForAuditPackagesForSolution, auditPackagesForSolutionCommandID);
                // '$' - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                auditPackagesForSolutionCommand.ParametersDescription = "$";
                mcs.AddCommand(auditPackagesForSolutionCommand);
            }
        }

        private void BeforeQueryStatusForAuditPackages(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding() && HasActiveLoadedSupportedProject;
            // disable the menu if any audits are already running
            command.Enabled = !AuditManager.IsAuditRunning;
        }

        private void BeforeQueryStatusForAuditPackagesForSolution(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding();
            // disable the menu if any audits are already running
            command.Enabled = !AuditManager.IsAuditRunning;
        }

        private bool IsSolutionExistsAndNotDebuggingAndNotBuilding()
        {
            int pfActive;
            int result = MonitorSelection.IsCmdUIContextActive(_solutionNotBuildingAndNotDebuggingContextCookie, out pfActive);
            return (result == VSConstants.S_OK && pfActive > 0);
        }

        private bool HasActiveLoadedSupportedProject
        {
            get
            {
                return (ActiveLoadedSupportedProject != null);
            }
        }

        private Project ActiveLoadedSupportedProject
        {
            get
            {
                var project = AuditHelper.GetActiveProject(MonitorSelection);

                if (project != null 
                    && !AuditHelper.IsProjectUnloaded(project)
                    && AuditHelper.IsProjectSupported(project))
                {
                    return project;
                }

                return null;
            }
        }

        private void InvokeAuditPackagesHandler(object sender, EventArgs e)
        {
            AuditManager.AuditProjectPackages(ActiveLoadedSupportedProject);
        }
        
        private void InvokeAuditPackagesForSolutionHandler(object sender, EventArgs e)
        {
            AuditManager.AuditSolutionPackages();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (this._auditManager!=null)
                    {
                        this._auditManager.Dispose();
                        this._auditManager = null;
                    }

                    GC.SuppressFinalize(this);
                }

                _vsMonitorSelection = null;
                _uiCtx = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
