#region License
// Copyright (c) 2015-2018, Sonatype Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Sonatype, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL SONATYPE BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.Win32;


using Task = System.Threading.Tasks.Task;

using NugetAuditor.VSIX.Properties;
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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#1110", "#1112", "1.0", IconResourceID = 1400)] // Info on this package for Help/About
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(GuidList.guidAuditPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSAsyncPackage : AsyncPackage
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="VSAsyncPackage"/> class.
        /// </summary>
        public VSAsyncPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
             
            _instance = this;

            ServiceLocator.InitializePackageServiceProvider(this);
        }
        #endregion

        #region Overriden members
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _uiCtx = SynchronizationContext.Current;

            // get the solution not building and not debugging cookie
            Guid guid = VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid;
            MonitorSelection.GetCmdUIContextCookie(ref guid, out _solutionNotBuildingAndNotDebuggingContextCookie);

            AuditManager.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();

            bool isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
            {
                AuditManager.QueueAuditSolutionPackages();
            }

            // Listen for subsequent solution events
            SolutionEvents.OnAfterOpenSolution += HandleOpenSolution;
        }

        #endregion

        #region Properties
        public static VSAsyncPackage Instance
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

        public int Option_CacheSync
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.CacheSync;
            }
        }

        internal IVsMonitorSelection MonitorSelection
        {
            get
            {
                if (this._vsMonitorSelection == null)
                {
                    this._vsMonitorSelection = ServiceLocator.GetInstance<IVsMonitorSelection>();

                    if (this._vsMonitorSelection == null)
                    {
                        throw new InvalidOperationException(string.Format(Resources.Culture, Resources.General_MissingService, typeof(IVsMonitorSelection).FullName));
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

        private bool HasActiveLoadedSupportedProject
        {
            get
            {
                return (ActiveLoadedSupportedProject != null);
            }
        }

        private EnvDTE.Project ActiveLoadedSupportedProject
        {
            get
            {
                var project = VsUtility.GetActiveProject(MonitorSelection);

                if (project != null
                    && !VsUtility.IsProjectUnloaded(project)
                    && VsUtility.IsProjectSupported(project))
                {
                    return project;
                }

                return null;
            }
        }
        #endregion

        #region Methods

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        private void HandleOpenSolution(object sender = null, EventArgs e = null)
        {
            // Handle the open solution and try to do as much work
            // on a background thread as possible
            AuditManager.QueueAuditSolutionPackages();
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
                    if (this._auditManager != null)
                    {
                        this._auditManager.Dispose();
                        this._auditManager = null;
                    }

                    GC.SuppressFinalize(this);
                }

                _vsMonitorSelection = null;
                _uiCtx = null;
                _instance = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Fields
        private static VSAsyncPackage _instance;
        private NugetAuditManager _auditManager;
        private SynchronizationContext _uiCtx;
        private uint _solutionNotBuildingAndNotDebuggingContextCookie;
        private IVsMonitorSelection _vsMonitorSelection;
        #endregion
    }
}
