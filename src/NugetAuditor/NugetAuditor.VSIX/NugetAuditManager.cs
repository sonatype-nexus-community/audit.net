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
using NuGet.Versioning;

namespace NugetAuditor.VSIX
{
    internal class NugetAuditManager : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private VulnerabilityTaskProvider _taskProvider;

        private PackageReferenceMarkerProvider _markerProvider;

        //private Dictionary<string, PackageReferenceMarkerClient> _packageReferenceMarkerClients;
        
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

            //_packageReferenceMarkerClients = new Dictionary<string, PackageReferenceMarkerClient>();

            _selectionEvents = new EventSinks.VsSelectionEvents(VSPackage.Instance.MonitorSelection);
            _selectionEvents.SolutionOpened += SelectionEvents_SolutionOpened;

            _packageInstallerEvents = ServiceLocator.GetInstance<IVsPackageInstallerEvents>();

            if (_packageInstallerEvents == null)
            {
                throw new InvalidOperationException(string.Format(Properties.Resources.Culture, Properties.Resources.General_MissingService, typeof(IVsPackageInstallerEvents).FullName));
            }

            _packageInstallerEvents.PackageReferenceAdded += InstallerEvents_PackageReferenceAdded;
            _packageInstallerEvents.PackageReferenceRemoved += InstallerEvents_PackageReferenceRemoved;

            _dte = ServiceLocator.GetInstance<DTE>();

            if (_dte == null)
            {
                throw new InvalidOperationException(string.Format(Properties.Resources.Culture, Properties.Resources.General_MissingService, typeof(DTE).FullName));
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
            var package = new Lib.PackageId(metadata.Id, metadata.VersionString);

            this.AuditPackage(package);
        }

        private void InstallerEvents_PackageReferenceRemoved(IVsPackageMetadata metadata)
        {
            var packageId = new Lib.PackageId(metadata.Id, metadata.VersionString);

            var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => x.PackageId == packageId);

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
                AddMarkers(document.FullName);
            }
        }

        private void RemoveMarkers(string documentPath)
        {
            _markerProvider.RemoveMarkers(documentPath);
        }

        //private void RemoveMarkers(IEnumerable<PackageReference> packageReferences)
        //{
        //    foreach (var packageReference in packageReferences)
        //    {
        //        PackageReferenceMarkerClient client;

        //        if (_packageReferenceMarkerClients.TryGetValue(packageReference.ToString(), out client))
        //        {
        //            client.RemoveTextLineMarker();
        //        }
        //    }
        //}

        //private PackageReferenceMarkerClient CreateMarkerClient(PackageReference packageReference)
        //{
        //    string key = packageReference.ToString();

        //    PackageReferenceMarkerClient client;

        //    if (!_packageReferenceMarkerClients.TryGetValue(key, out client))
        //    {
        //        client = new PackageReferenceMarkerClient(packageReference, () => _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => x.PackageReference == packageReference));

        //        _packageReferenceMarkerClients[key] = client;
        //    }

        //    return client;
        //}

        private void AddMarkers(string documentPath)
        {
            //var textLines = AuditHelper.GetDocumentTextLines(documentPath);

            //// if document is opened
            //if (textLines != null)
            //{
                _markerProvider.CreateMarkers(documentPath);

                //var packageReferences = _taskProvider.Tasks.OfType<VulnerabilityTask>()
                //           .Where(x => x.Document.Equals(documentPath, StringComparison.InvariantCultureIgnoreCase))
                //           .Select(x => x.PackageReference).Distinct();

                //foreach (var packageReference in packageReferences)
                //{
                //    var client = CreateMarkerClient(packageReference);

                //    client.CreateTextLineMarker(textLines);
                //}
            //}
        }

        private void RemoveTasks(IEnumerable<VulnerabilityTask> tasks)
        {
           // RemoveMarkers(tasks);

            _taskProvider.SuspendRefresh();

            foreach (var task in tasks.ToArray())
            {
                _taskProvider.Tasks.Remove(task);
            }

            _taskProvider.ResumeRefresh();
            _markerProvider.RefreshMarkers();
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

        private void AddResults(ProjectInfo projectInfo, IEnumerable<Lib.AuditResult> auditResults)
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
                    var vulnerable = auditResult.AffectingVulnerabilities.Contains(vulnerability);

                    var task = new VulnerabilityTask(packageReference)
                    {
                        Priority = vulnerable ? TaskPriority.Normal : TaskPriority.Low,
                        ErrorCategory = vulnerable ? TaskErrorCategory.Error : TaskErrorCategory.Message,
                        Text = string.Format("{0}: {1}\n{2}", packageReference.PackageId, vulnerability.Title, vulnerability.Summary),
                        HierarchyItem = projectInfo.ProjectHierarchy,
                        Category = vulnerable ? TaskCategory.CodeSense : TaskCategory.Comments,
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

                _markerProvider.Register(packageReference, () => _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => x.PackageReference.Equals(packageReference)));
            }
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
                WriteLine(Properties.Resources.AuditingPackageError, e.Exception.Message);
                ExceptionHelper.WriteToActivityLog(e.Exception);
            }
            else
            {
                var vulnerableCount = e.Results.Count(x => x.Status == AuditStatus.Vulnerable);

                if (vulnerableCount > 0)
                {
                    WriteLine(Properties.Resources.VulnerabilitiesFound, vulnerableCount);
                }
                else
                {
                    WriteLine(Properties.Resources.NoVulnarebilitiesFound);
                }

                _taskProvider.SuspendRefresh();

                foreach (Project project in AuditHelper.GetAllSupportedProjects(_dte.Solution))
                {
                    using (var projectInfo = new ProjectInfo(project))
                    {
                        AddResults(projectInfo, e.Results);
                        AddMarkers(projectInfo.PackageReferencesFile.Path);
                    }
                }

                _taskProvider.ResumeRefresh();

                if (vulnerableCount > 0)
                {
                    _taskProvider.BringToFront();
                }
            }
        }

        public void AuditPackage(PackageId package)
        {
            ClearOutput(true);

            WriteLine(Properties.Resources.AuditingPackage, package);

            bool started = RunAudit(new[] { package }, OnAuditCompleted);

            if (started)
            {
                //remove tasks related to this package
                var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>()
                    .Where(x => x.PackageId == package);

                RemoveTasks(tasks);
            }
        }

        //public void ToggleIgnorePackageReference(PackageReference packageReference)
        //{
        //    var file = new PackageReferencesFile(packageReference.File);

        //    if (packageReference.Ignore == false)
        //    {
        //        file.ToggleIgnorePackageReference(packageReference);

        //        //remove tasks related to this reference
        //        var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>()
        //            .Where(x => x.PackageReference == packageReference);

        //        RemoveTasks(tasks);
        //    }
        //    else
        //    {
        //        AuditPackage(packageReference);
        //    }
            
        //}

        public void AuditProjectPackages(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            ClearOutput(true);

            var packages = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages(project).Select(x=> new PackageId(x.Id, x.VersionString)).ToList();

            WriteLine(Properties.Resources.AuditingPackagesInProject, packages.Count, project.Name);

            bool started = RunAudit(packages, OnAuditCompleted);

            if (started)
            {
                //remove tasks related to this project 
                using (var projectInfo = new ProjectInfo(project))
                {
                    var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>()
                                    .Where(x => x.Document.Equals(projectInfo.PackageReferencesFile.Path, StringComparison.InvariantCultureIgnoreCase));

                    RemoveTasks(tasks);
                }
            }
        }

        public void AuditSolutionPackages()
        {
            ClearOutput(true);

            var packages = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages().Select(x => new PackageId(x.Id, x.VersionString)).ToList();

            var solutionName = AuditHelper.GetSolutionName(_dte.Solution);

            WriteLine(Properties.Resources.AuditingPackagesInSolution, packages.Count, solutionName);

            bool started = RunAudit(packages, OnAuditCompleted);

            if (started)
            {
                //remove all tasks
                RemoveTasks(_taskProvider.Tasks.OfType<VulnerabilityTask>());
            }
        }

        private bool RunAudit(IEnumerable<PackageId> packages, EventHandler<AuditCompletedEventArgs> completedHandler)
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
                        //var packageIds = packages.Select(x => new PackageId(x.Id, NuGetVersion.Parse(x.VersionString).ToNormalizedString()));

                        results = Lib.NugetAuditor.AuditPackages(packages);
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
                    // TODO: dispose managed state (managed objects).
                    //if (_packageReferenceMarkerClients != null)
                    //{
                    //    foreach (var client in _packageReferenceMarkerClients.Values)
                    //    {
                    //        client.Dispose();
                    //    }
                    //    _packageReferenceMarkerClients.Clear();
                    //}

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
