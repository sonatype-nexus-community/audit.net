using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using NugetAuditor.Lib;
using NugetAuditor.VSIX.EventSinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NuGet.VisualStudio;
using System.Diagnostics;
using Microsoft.VisualStudio;

namespace NugetAuditor.VSIX
{
    internal class NugetAuditManager : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private AuditTaskProvider _taskProvider;

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
            _taskProvider = new AuditTaskProvider(this._serviceProvider);

            _selectionEvents = new EventSinks.VsSelectionEvents(ServiceLocator.GetInstance<IVsMonitorSelection>());
            _selectionEvents.SolutionOpened += SelectionEvents_SolutionOpened;

            _packageInstallerEvents = ServiceLocator.GetInstance<IVsPackageInstallerEvents>();
            _packageInstallerEvents.PackageReferenceAdded += InstallerEvents_PackageReferenceAdded;
            _packageInstallerEvents.PackageReferenceRemoved += InstallerEvents_PackageReferenceRemoved;

            var dte = ServiceLocator.GetInstance<DTE>();

            _documentEvents = dte.Events.DocumentEvents;

            _documentEvents.DocumentOpened += OnDocumentOpened;
            _documentEvents.DocumentClosing += OnDocumentClosing;
        }

        private void SelectionEvents_SolutionOpened(object sender, EventArgs e)
        {
            AuditSolutionPackages();
        }

        private void InstallerEvents_PackageReferenceAdded(IVsPackageMetadata metadata)
        {
            var packageId = new Lib.PackageId(metadata.Id, metadata.VersionString);

            this.AuditPackage(packageId);
        }

        private void InstallerEvents_PackageReferenceRemoved(IVsPackageMetadata metadata)
        {
            var packageId = new Lib.PackageId(metadata.Id, metadata.VersionString);

            var tasks = _taskProvider.GetTasks<AuditTask>().Where(x => x.PackageId == packageId);

            RemoveTasks(tasks.ToArray());
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

        public void RemoveMarkers(string documentPath)
        {
            // Invalidate line markers for document task.
            foreach (var task in _taskProvider.GetTasks<AuditTask>().Where(x => x.Document.Equals(documentPath, StringComparison.InvariantCultureIgnoreCase)))
            {
                task.InvalidateTextLineMarker();
            }
        }

        public void AddMarkers(string documentPath)
        {
            var textLines = AuditHelper.GetDocumentTextLines(documentPath);

            // if document is opened
            if (textLines != null)
            {
                // Create line markers for each task.
                foreach (var task in _taskProvider.GetTasks<AuditTask>().Where(x => x.Document.Equals(documentPath, StringComparison.InvariantCultureIgnoreCase)))
                {
                    task.CreateTextLineMarker(textLines);
                }
            }
        }

        private void RemoveTasks(AuditTask[] tasks)
        {
            _taskProvider.SuspendRefresh();

            foreach (var task in tasks)
            {
                _taskProvider.Tasks.Remove(task);
            }

            _taskProvider.ResumeRefresh();
        }

        public void AuditPackage(PackageId packageId)
        {
            ClearOutput();

            WriteLine("Auditing package {0}", packageId);

            var packageIds = new[] { packageId };

            bool started = RunAudit(packageIds, (object sender, AuditCompletedEventArgs e) =>
            {
                if (e.Exception != null)
                {
                    WriteLine("Audit failed for package {0} {1}", packageId, e.Exception);
                }
                else
                {
                    WriteLine("Audit succeeded for package {0}", packageId);

                    _taskProvider.SuspendRefresh();

                    foreach (Project project in AuditHelper.GetAllSupportedProjects())
                    {
                        using (var projectInfo = new ProjectInfo(project))
                        {
                            _taskProvider.AddResults(projectInfo, e.Results);
                            AddMarkers(projectInfo.PackageReferencesFile.Path);
                        }
                    }

                    _taskProvider.ResumeRefresh();
                }
            });

            if (started)
            {
                //remove tasks related to this package
                var tasks = _taskProvider.GetTasks<AuditTask>().Where(x => x.PackageId == packageId);

                RemoveTasks(tasks.ToArray());
            }
        }

        public void AuditProjectPackages(Project project)
        {
            ClearOutput();

            var packageIds = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages(project).Select(x => new PackageId(x.Id, x.VersionString));

            WriteLine("Auditing {0} packages for project {1}.", packageIds.Count(), project.Name);

            bool started = RunAudit(packageIds, (object sender, AuditCompletedEventArgs e) =>
            {

                if (e.Exception != null)
                {
                    WriteLine("Audit failed for project {0}, {1}", project.Name, e.Exception);
                }
                else
                {
                    WriteLine("Audit succeeded for project {0}.", project.Name);

                    _taskProvider.SuspendRefresh();

                    using (var projectInfo = new ProjectInfo(project))
                    {
                        _taskProvider.AddResults(projectInfo, e.Results);

                        AddMarkers(projectInfo.PackageReferencesFile.Path);
                    }

                    _taskProvider.ResumeRefresh();
                }
            });

            if (started)
            {
                //remove tasks related to this project 
                using (var projectInfo = new ProjectInfo(project))
                {
                    var tasks = _taskProvider.GetTasks<AuditTask>().Where(x => x.Document.Equals(projectInfo.PackageReferencesFile.Path, StringComparison.InvariantCultureIgnoreCase));
                    RemoveTasks(tasks.ToArray());
                }
            }
        }

        public void AuditSolutionPackages()
        {
            ClearOutput();

            var packageIds = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages().Select(x => new PackageId(x.Id, x.VersionString));

            WriteLine("Auditing {0} NuGet packages for solution.", packageIds.Count());

            bool started = RunAudit(packageIds, (object sender, AuditCompletedEventArgs e) =>
            {
                if (e.Exception != null)
                {
                    WriteLine("Audit failed for solution. {0}", e.Exception);
                }
                else
                {
                    WriteLine("Audit succeeded for solution.");

                    _taskProvider.SuspendRefresh();

                    foreach (Project project in AuditHelper.GetAllSupportedProjects())
                    {
                        using (var projectInfo = new ProjectInfo(project))
                        {
                            _taskProvider.AddResults(projectInfo, e.Results);
                            AddMarkers(projectInfo.PackageReferencesFile.Path);
                        }
                    }

                    _taskProvider.ResumeRefresh();
                }
            });

            if (started)
            {
                //remove all tasks
                RemoveTasks(_taskProvider.GetTasks<AuditTask>().ToArray());
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
            return VSPackage.Instance.GetOutputPane(VSConstants.SID_SVsGeneralOutputWindowPane, "Nuget Auditor");
        }

        private void ClearOutput()
        {
            var pane = GetOutputPane();

            if (pane != null)
            {
                ErrorHandler.ThrowOnFailure(pane.Clear());
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

        public void Dispose()
        {
            _packageInstallerEvents.PackageReferenceAdded -= InstallerEvents_PackageReferenceAdded;
            _packageInstallerEvents.PackageReferenceRemoved -= InstallerEvents_PackageReferenceRemoved;

            _documentEvents.DocumentOpened -= OnDocumentOpened;
            _documentEvents.DocumentClosing -= OnDocumentClosing;

            _selectionEvents.SolutionOpened -= SelectionEvents_SolutionOpened;
            _selectionEvents.Dispose();

            _taskProvider.Dispose();

            _taskProvider = null;
            _packageInstallerEvents = null;
            _documentEvents = null;
        }
    }
}
