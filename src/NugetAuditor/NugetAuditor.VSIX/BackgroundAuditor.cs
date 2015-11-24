using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    internal interface IBackgroundAuditor
    {
        event EventHandler Started;
        event EventHandler Stopped;

        bool IsRunning { get; }
        void Start(IEnumerable<IVsProject> projects);
        void StopIfRunning(bool blockUntilDone);
    }

    internal class BackgroundAuditor : IBackgroundAuditor
    {
        private static IServiceProvider _serviceProvider;

        public bool IsRunning
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler Started;
        public event EventHandler Stopped;

        public void Start(IEnumerable<IVsProject> projects)
        {
            throw new NotImplementedException();
        }

        public void StopIfRunning(bool blockUntilDone)
        {
            throw new NotImplementedException();
        }

        private IVsPackageInstallerEvents _packageInstallerEvents;

        private IVsPackageInstallerEvents GetPackageInstallerEvents()
        {
            if (_packageInstallerEvents == null)
            {
                var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));

                _packageInstallerEvents = componentModel.GetService<IVsPackageInstallerEvents>();
            }
            return _packageInstallerEvents;
        }

    }
}
