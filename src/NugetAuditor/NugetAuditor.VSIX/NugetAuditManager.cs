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
using TTasks = System.Threading.Tasks;

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
		//private IVsPackageInstallerProjectEvents _packageInstallerProjectEvents;
		private VsSelectionEvents _selectionEvents;

        private BackgroundQueue _backgroundQueue = new BackgroundQueue();
        private TTasks.TaskScheduler _uiTaskScheduler;

        private Dictionary<PackageId, AuditResult> _auditResults = new Dictionary<PackageId, AuditResult>(EqualityComparer<PackageId>.Default);

		private Timer _refreshTimer;
		private const int _refreshTimeout = 2000;

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
            _uiTaskScheduler = TTasks.TaskScheduler.FromCurrentSynchronizationContext();
            _taskProvider = new VulnerabilityTaskProvider(this._serviceProvider);
            _markerProvider = new PackageReferenceMarkerProvider(_taskProvider);
			_refreshTimer = new Timer(new TimerCallback(RefreshTimer), null, Timeout.Infinite, Timeout.Infinite);

            _selectionEvents = new EventSinks.VsSelectionEvents(VSPackage.Instance.MonitorSelection);
            _selectionEvents.SolutionOpened += SelectionEvents_SolutionOpened;

			_packageInstallerEvents = ServiceLocator.GetInstance<IVsPackageInstallerEvents>();

			if (_packageInstallerEvents == null)
            {
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.General_MissingService, typeof(IVsPackageInstallerEvents).FullName));
            }

			_packageInstallerEvents.PackageInstalled += InstallerEvents_PackageInstalled;
			_packageInstallerEvents.PackageUninstalled += InstallerEvents_PackageUninstalled;
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
			_backgroundQueue.QueueTask(async () => 
            {
                //start the task on UI thread.
                var asyncTask = TTasks.Task.Factory.StartNew(() =>
                {
					return AuditSolutionPackagesInternal();
                }, CancellationToken.None, TTasks.TaskCreationOptions.None, _uiTaskScheduler);

                while (!await asyncTask)
                {
                    //wait on background thread.
                    TTasks.Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                }
            });
        }

		private void InstallerEvents_PackageInstalled(IVsPackageMetadata metadata)
		{
			_backgroundQueue.QueueTask(async () => {
				var asyncTask = TTasks.Task.Factory.StartNew(() => {
					return AuditPackageInternal(metadata);
				}, CancellationToken.None, TTasks.TaskCreationOptions.None, _uiTaskScheduler);

				while (!await asyncTask)
				{
					TTasks.Task.Delay(TimeSpan.FromSeconds(5)).Wait();
				}
			});
		}

		private void InstallerEvents_PackageUninstalled(IVsPackageMetadata metadata)
		{
			_refreshTimer.Change(_refreshTimeout, Timeout.Infinite);
		}

		private void InstallerEvents_PackageReferenceAdded(IVsPackageMetadata metadata)
        {
			_refreshTimer.Change(_refreshTimeout, Timeout.Infinite);
        }

        private void InstallerEvents_PackageReferenceRemoved(IVsPackageMetadata metadata)
        {
			_refreshTimer.Change(_refreshTimeout, Timeout.Infinite);
        }

		private void RefreshTimer(object state) {
			_backgroundQueue.QueueTask(() => {
				RefreshTasks();
			}, _uiTaskScheduler);
		}

        private void RefreshTasks()
        {
            VSPackage.AssertOnMainThread();

            var supportedProjects = _dte.Solution.GetSupportedProjects().ToList();

            _taskProvider.SuspendRefresh();

            _taskProvider.Tasks.Clear();

            foreach (var task in GetVulnerabilityTasks(supportedProjects))
            {
                _taskProvider.Tasks.Add(task);
            }

            _taskProvider.Refresh();
            _taskProvider.ResumeRefresh();

            foreach (var project in supportedProjects)
            {
                CreateMarkers(project.GetPackageReferenceFilePath());
            }
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

        private IEnumerable<VulnerabilityTask> GetVulnerabilityTasks(IEnumerable<Project> supportedProjects)
        {
            foreach (var project in supportedProjects)
            {
                var projectHierarchy = project.GetHierarchy();

                var packageReferencesFile = new PackageReferencesFile(project.GetPackageReferenceFilePath());

                foreach (var packageReference in packageReferencesFile.GetPackageReferences())
                {
                    if (packageReference.Ignore)
                    {
                        continue;
                    }

                    AuditResult auditResult;

                    if (!_auditResults.TryGetValue(packageReference.PackageId, out auditResult))
                    {
                        continue;
                    }

                    if (auditResult == null
                        || auditResult.Status == AuditStatus.NoActiveVulnerabilities
                        || auditResult.Status == AuditStatus.UnknownPackage
                        || auditResult.Status == AuditStatus.UnknownSource)
                    {
                        continue;
                    }

                    foreach (var vulnerability in auditResult.Vulnerabilities)
                    {
						var affecting = true; // vulnerability.AffectsVersion(packageReference.PackageId.VersionString);

                        if (affecting)
                        {
                            var task = new VulnerabilityTask(packageReference, vulnerability)
                            {
                                Priority = affecting ? TaskPriority.Normal : TaskPriority.Low,
                                ErrorCategory = affecting ? TaskErrorCategory.Error : TaskErrorCategory.Message,
                                Text = string.Format("{0}: {1}\nReference: https://ossindex.sonatype.org/resource/vulnerability/{2}\n{3}", packageReference.PackageId, vulnerability.Title, vulnerability.Id, vulnerability.Description),
                                HierarchyItem = projectHierarchy,
                                Category = TaskCategory.Misc,
                                Document = packageReference.File,
                                Line = packageReference.StartLine,
                                Column = packageReference.StartPos,
                                //HelpKeyword = vulnerability.CveId
                            };


                            task.Navigate += Task_Navigate;
                            task.Removed += Task_Removed;
                            task.Help += Task_Help;

                            yield return task;
                        }
                    }
                }
            }
        }

        private void Task_Help(object sender, EventArgs e)
        {
            var task = sender as VulnerabilityTask;

            if (task != null)
            {
                string url = string.Format("https://ossindex.sonatype.org/vuln/{0}", task.Vulnerability.Id);
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
            else if (e.Results.Count() == 0)
            {
                WriteLine(Resources.NoPackagesToAudit);
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

                //update audit results dictionary
                foreach (var auditResult in e.Results)
                {
                    _auditResults[auditResult.PackageId] = auditResult;
                }

				//refresh tasks
				RefreshTasks();

                if (vulnerableCount > 0)
                {
					_taskProvider.BringToFront();
                }
            }
        }
        
        public void AuditProjectPackages(Project project)
        {
            ClearOutput(true);

            AuditProjectPackagesInternal(project);
        }

        public void AuditSolutionPackages()
        {
            ClearOutput(true);

            AuditSolutionPackagesInternal();
        }

        private bool AuditSolutionPackagesInternal()
        {
            VSPackage.AssertOnMainThread();

            var packages = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages();

            WriteLine(Resources.AuditingPackagesInSolution, packages.Count(), _dte.Solution.GetName());

            return AuditPackagesInternal(packages);
        }

        private bool AuditProjectPackagesInternal(Project project)
        {
            VSPackage.AssertOnMainThread();

            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            var packages = ServiceLocator.GetInstance<IVsPackageInstallerServices>().GetInstalledPackages(project);

            WriteLine(Resources.AuditingPackagesInProject, packages.Count(), project.Name);

            return AuditPackagesInternal(packages);
        }

		private bool AuditPackageInternal(IVsPackageMetadata package)
		{
			var packageId = new PackageId(package.Id, package.VersionString);

			WriteLine(Resources.AuditingPackage, packageId);

			return AuditPackagesInternal(new[] { packageId });
		}

		private bool AuditPackagesInternal(IEnumerable<IVsPackageMetadata> packages)
        {
            var packageIds = packages.Select(x => new PackageId(x.Id, x.VersionString));

            return AuditPackagesInternal(packageIds);
        }

        private bool AuditPackagesInternal(IEnumerable<PackageId> packageIds)
        {
            bool started = RunAudit(packageIds, OnAuditCompleted);

            if (started)
            {
                //remove tasks related to this packages
                var tasks = _taskProvider.Tasks.OfType<VulnerabilityTask>().Where(x => packageIds.Contains(x.PackageId));

                RemoveTasks(tasks);
            }

            return started;
        }

		private bool RunAudit(IEnumerable<PackageId> packageIds, EventHandler<AuditCompletedEventArgs> completedHandler)
		{
			if (!packageIds.Any())
			{
				completedHandler?.Invoke(null, new AuditCompletedEventArgs(Enumerable.Empty<AuditResult>(), null));
				return true;
			}

			if (IsAuditRunning)
			{
				return false;
			}

			_auditRunning = true;

			// Now we will queue a delegate that will be run on a worker thread.
			ThreadPool.QueueUserWorkItem(state => {
				// !! WORKER THREAD CONTEXT !!
				Exception exception = null;
				IEnumerable<AuditResult> results = null;

				try
				{
					results = Lib.NugetAuditor.AuditPackages(packageIds, VSPackage.Instance.Option_CacheSync);
				}
				catch (Exception ex)
				{
					// Just record the exception, we will handle it later.
					exception = ex;
				}

				// Here we are still in the worker thread context. The completion event must be executed in
				// the same thread context as caller of RunAsync(). To change the thread context we use the
				// stored synchronization context.
				VSPackage.Instance.UICtx.Send((x) => {
					// !! MAIN THREAD CONTEXT !!
					// Back to main thread. From here we can safely update our internal state and invoke the
					// completion event.

					// Reset process and running flag.
					_auditRunning = false;

					// notify event subscribers (if any).
					completedHandler?.Invoke(null, new AuditCompletedEventArgs(results, exception));
				}, null);
			});

			return true;
		}

        #region Output Window

        private IVsOutputWindowPane GetOutputPane()
        {
            VSPackage.AssertOnMainThread();

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
					_refreshTimer?.Dispose();
                    _markerProvider?.Dispose();

                    if (_packageInstallerEvents != null)
                    {
                        _packageInstallerEvents.PackageReferenceAdded -= InstallerEvents_PackageReferenceAdded;
                        _packageInstallerEvents.PackageReferenceRemoved -= InstallerEvents_PackageReferenceRemoved;
						_packageInstallerEvents.PackageInstalled -= InstallerEvents_PackageInstalled;
						_packageInstallerEvents.PackageUninstalled -= InstallerEvents_PackageUninstalled;
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

                    _taskProvider?.Dispose();
                    _backgroundQueue?.Dispose();

                    _auditResults.Clear();
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
                _backgroundQueue = null;
                _auditResults = null;

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
