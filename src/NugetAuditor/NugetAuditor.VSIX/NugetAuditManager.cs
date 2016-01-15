#region License
// Copyright (c) 2015-2016, Vör Security Ltd.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Vör Security, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL VÖR SECURITY BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NugetAuditor.Lib;
using NugetAuditor.VSIX.EventSinks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NuGet.VisualStudio;
using Microsoft.VisualStudio;
using NugetAuditor.VSIX.Properties;

namespace NugetAuditor.VSIX
{
    internal class NugetAuditManager : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private VulnerabilityTaskProvider _taskProvider;

        private PackageReferenceMarkerProvider _markerProvider;

        private DTE _dte;
        private DocumentEvents _documentEvents;

        private IVsPackageInstallerEvents _packageInstallerEvents;
        private VsSelectionEvents _selectionEvents;

        private bool _auditRunning = false;

        public bool IsAuditRunning
        {
            get { return _auditRunning; }
        }

        public NugetAuditManager(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public void Initialize()
        {
            _taskProvider = new VulnerabilityTaskProvider(this._serviceProvider);
            _markerProvider = new PackageReferenceMarkerProvider();

            _selectionEvents = new EventSinks.VsSelectionEvents(VSPackage.Instance.MonitorSelection);
            _selectionEvents.SolutionOpened += SelectionEvents_SolutionOpened;

            _packageInstallerEvents = ServiceLocator.GetInstance<IVsPackageInstallerEvents>();

            if (_packageInstallerEvents == null)
            {
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.General_MissingService, typeof(IVsPackageInstallerEvents).FullName));
            }

            _packageInstallerEvents.PackageReferenceAdded += InstallerEvents_PackageReferenceAdded;
            _packageInstallerEvents.PackageReferenceRemoved += InstallerEvents_PackageReferenceRemoved;

            _dte = ServiceLocator.GetInstance<DTE>();

            if (_dte == null)
            {
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.General_MissingService, typeof(DTE).FullName));
            }

            _documentEvents = _dte.Events.DocumentEvents;

            _documentEvents.DocumentOpened += OnDocumentOpened;
            _documentEvents.DocumentClosing += OnDocumentClosing;
        }

        private void SelectionEvents_SolutionOpened(object sender, EventArgs e)
        {
            AuditSolutionPackages();
        }

        private void InstallerEvents_PackageReferenceAdded(IVsPackageMetadata metadata)
        {
            this.AuditPackage(metadata);
        }

        private void InstallerEvents_PackageReferenceRemoved(IVsPackageMetadata metadata)
        {
            var packageId = new Lib.PackageId(metadata.Id, metadata.VersionString);

            var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => packageId.Equals(x.PackageId));

            RemoveTasks(tasks);
        }

        private void OnDocumentClosing(Document document)
        {
            if (document != null)
            {
                RemoveMarkers(document.FullName);
            }
        }

        private void OnDocumentOpened(Document document)
        {
            if (document != null)
            {
                CreateMarkers(document.FullName);
            }
        }

        private void RemoveMarkers(string documentPath)
        {
            _markerProvider.RemoveMarkers(documentPath);
        }

        private void CreateMarkers(string documentPath)
        {
            _markerProvider.CreateMarkers(documentPath);
        }

        private void RemoveTasks(IEnumerable<VulnerabilityTask> tasks)
        {
            _taskProvider.SuspendRefresh();

            foreach (var task in tasks.ToArray())
            {
                _taskProvider.Tasks.Remove(task);
            }

            _markerProvider.RefreshMarkers();

            _taskProvider.ResumeRefresh();
        }

        private int EnsurePackageSubcategory(PackageId package)
        {
            var category = package.ToString();

            var index = _taskProvider.Subcategories.IndexOf(category);

            if (index < 0)
            {
                index = _taskProvider.Subcategories.Add(category);
            }

            return index;                             
        }

        private void ProcessAuditResults(ProjectInfo projectInfo, IEnumerable<Lib.AuditResult> auditResults)
        {
            foreach (var packageReference in projectInfo.PackageReferencesFile.GetPackageReferences())
            {
                var auditResult = auditResults.Where(x => x.PackageId.Equals(packageReference)).FirstOrDefault();

                if (auditResult == null
                    || auditResult.Status == AuditStatus.NoKnownVulnerabilities
                    || auditResult.Status == AuditStatus.UnknownPackage
                    || auditResult.Status == AuditStatus.UnknownSource)
                {
                    continue;
                }

                if (packageReference.Ignore)
                {
                    WriteLine("Skipping ignored package '{0}'.", packageReference.PackageId);
                    continue;
                }

                foreach (var vulnerability in auditResult.Vulnerabilities)
                {
                    var affecting = vulnerability.AffectsVersion(packageReference.VersionString);

                    var task = new VulnerabilityTask(packageReference)
                    {
                        Priority = affecting ? TaskPriority.Normal : TaskPriority.Low,
                        ErrorCategory = affecting ? TaskErrorCategory.Error : TaskErrorCategory.Message,
                        Text = string.Format("{0}: {1}\n{2}", packageReference.PackageId, vulnerability.Title, vulnerability.Summary),
                        HierarchyItem = projectInfo.ProjectHierarchy,
                        Category = TaskCategory.Misc,
                        Document = packageReference.File,
                        Line = packageReference.StartLine,
                        Column = packageReference.StartPos,
                        SubcategoryIndex = EnsurePackageSubcategory(packageReference),
                        HelpKeyword = vulnerability.CveId,
                    };

                    task.Navigate += Task_Navigate;
                    task.Removed += Task_Removed;
                    task.Help += Task_Help;

                    _taskProvider.Tasks.Add(task);
                }

                _markerProvider.EnsureMarkerClient(packageReference, () => _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => x.PackageReference.Equals(packageReference)));
            }

            CreateMarkers(projectInfo.PackageReferencesFile.Path);
        }

        private void Task_Help(object sender, EventArgs e)
        {
            var task = sender as VulnerabilityTask;

            if (task != null)
            {
                var url = string.Format("http://cve.mitre.org/cgi-bin/cvename.cgi?name={0}", task.HelpKeyword);

                VsShellUtilities.OpenBrowser(url);
            }
        }

        private void Task_Removed(object sender, EventArgs e)
        {
            var task = sender as VulnerabilityTask;

            if (task != null)
            {
                task.Removed -= Task_Removed;
                task.Navigate -= Task_Navigate;
                task.Help -= Task_Help;
            }
        }

        private void Task_Navigate(object sender, EventArgs e)
        {
            var task = sender as VulnerabilityTask;

            if (task != null)
            {
                _taskProvider.Navigate(task, new Guid(LogicalViewID.TextView));
            }
        }

        private void OnAuditCompleted(object sender, AuditCompletedEventArgs e)
        {
            VSPackage.AssertOnMainThread();

            if (e.Exception != null)
            {
                WriteLine(Resources.AuditingPackageError, e.Exception.Message);
                ExceptionHelper.WriteToActivityLog(e.Exception);
            }
            else
            {
                var vulnerableCount = e.Results.Count(x => x.Status == AuditStatus.HasVulnerabilities);

                if (vulnerableCount > 0)
                {
                    WriteLine(Resources.VulnerabilitiesFound, vulnerableCount);
                }
                else
                {
                    WriteLine(Resources.NoVulnarebilitiesFound);
                }

                _taskProvider.SuspendRefresh();

                foreach (Project project in VsUtility.GetAllSupportedProjects(_dte.Solution))
                {
                    using (var projectInfo = new ProjectInfo(project))
                    {
                        ProcessAuditResults(projectInfo, e.Results);
                        
                    }
                }

                _taskProvider.ResumeRefresh();

                if (vulnerableCount > 0)
                {
                    _taskProvider.BringToFront();
                }
            }
        }

        public void AuditPackage(IVsPackageMetadata package)
        {
            ClearOutput(true);

            WriteLine(Resources.AuditingPackage, package);

            bool started = RunAudit(new[] { package }, OnAuditCompleted);

            if (started)
            {
                //remove tasks related to this package
                var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => package.Equals(x.PackageId));

                RemoveTasks(tasks);
            }
        }

        public void AuditProjectPackages(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            ClearOutput(true);

            var packages = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages(project).ToList();

            WriteLine(Resources.AuditingPackagesInProject, packages.Count, project.Name);

            bool started = RunAudit(packages, OnAuditCompleted);

            if (started)
            {
                //remove tasks related to this project 
                using (var projectInfo = new ProjectInfo(project))
                {
                    var path = projectInfo.PackageReferencesFile.Path;
                    var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>()
                                    .Where(x => x.Document.Equals(path, StringComparison.InvariantCultureIgnoreCase));

                    RemoveTasks(tasks);
                }
            }
        }

        public void AuditSolutionPackages()
        {
            ClearOutput(true);

            var packages = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages().ToList();

            var solutionName = VsUtility.GetSolutionName(_dte.Solution);

            WriteLine(Resources.AuditingPackagesInSolution, packages.Count, solutionName);

            bool started = RunAudit(packages, OnAuditCompleted);

            if (started)
            {
                //remove all tasks
                RemoveTasks(_taskProvider.Tasks.OfType<VulnerabilityTask>());
            }
        }

        private bool RunAudit(IEnumerable<IVsPackageMetadata> packages, EventHandler<AuditCompletedEventArgs> completedHandler)
        {
            if (IsAuditRunning)
            {
                return false;
            }

            _auditRunning = true;

            // Now we will queue a delegate that will be run on a worker thread.
            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    // !! WORKER THREAD CONTEXT !!
                    Exception exception = null;
                    object results = null;

                    try
                    {
                        var packageIds = packages.Select(x => new PackageId(x.Id, x.VersionString));

                        results = Lib.NugetAuditor.AuditPackages(packageIds);
                    }
                    catch (Exception ex)
                    {
                        // Just record the exception, we will handle it later.
                        exception = ex;
                    }

                    // Here we are still in the worker thread context. The completion event must be executed in
                    // the same thread context as caller of RunAsync(). To change the thread context we use the
                    // stored synchronization context.
                    VSPackage.Instance.UICtx.Send((x) =>
                    {
                        // !! MAIN THREAD CONTEXT !!
                        // Back to main thread. From here we can safely update our internal state and invoke the
                        // completion event.

                        // Reset process and running flag.
                        _auditRunning = false;

                        // notify event subscribers (if any).
                        if (completedHandler != null)
                        {
                            var eventArgs = new AuditCompletedEventArgs(x as IEnumerable<Lib.AuditResult>, exception);
                            completedHandler(null, eventArgs);
                        }
                    }, results);
                });

            return true;
        }

        #region Output Window

        private IVsOutputWindowPane GetOutputPane()
        {
            return VSPackage.Instance.GetOutputPane(VSConstants.SID_SVsGeneralOutputWindowPane, "Audit.Net");
        }

        private void ClearOutput(bool activate = false)
        {
            var pane = GetOutputPane();

            if (pane != null)
            {
                ErrorHandler.ThrowOnFailure(pane.Clear());

                if (activate)
                {
                    ErrorHandler.ThrowOnFailure(pane.Activate());
                }
            }
        }

        private void WriteLine(string format, params object[] args)
        {
            var pane = GetOutputPane();

            if (pane != null)
            {
                var msg = string.Format(System.Globalization.CultureInfo.CurrentCulture, format, args);
                pane.OutputString(msg);
                pane.OutputString(Environment.NewLine);
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_markerProvider != null)
                    {
                        _markerProvider.Dispose();
                    }

                    if (_packageInstallerEvents != null)
                    {
                        _packageInstallerEvents.PackageReferenceAdded -= InstallerEvents_PackageReferenceAdded;
                        _packageInstallerEvents.PackageReferenceRemoved -= InstallerEvents_PackageReferenceRemoved;
                    }

                    if (_documentEvents != null)
                    {
                        _documentEvents.DocumentOpened -= OnDocumentOpened;
                        _documentEvents.DocumentClosing -= OnDocumentClosing;
                    }

                    if (_selectionEvents != null)
                    {
                        _selectionEvents.SolutionOpened -= SelectionEvents_SolutionOpened;
                        _selectionEvents.Dispose();
                    }

                    if (_taskProvider != null)
                    {
                        _taskProvider.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                //_packageReferenceMarkerClients = null;
                _markerProvider = null;
                _packageInstallerEvents = null;
                _documentEvents = null;
                _selectionEvents = null;
                _dte = null;
                _taskProvider = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~NugetAuditManager()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
             GC.SuppressFinalize(this);
        }
        #endregion
    }
}
