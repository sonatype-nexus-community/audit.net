using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    internal static class OSSAuditor
    {
        private static IServiceProvider _serviceProvider;
        private static IVsPackageInstallerEvents _installerEvents;
        private static IVsPackageInstallerServices _installerServices;
        
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));

            _installerServices = componentModel.GetService<IVsPackageInstallerServices>();
            _installerEvents = componentModel.GetService<IVsPackageInstallerEvents>();

            _installerEvents.PackageReferenceAdded += _installerEvents_PackageReferenceAdded;
            _installerEvents.PackageReferenceRemoved += _installerEvents_PackageReferenceRemoved;
            
            TaskManager.AddMessage("OSSAuditor Initialized");
        }

        private static void _installerEvents_PackageReferenceRemoved(IVsPackageMetadata metadata)
        {
            TaskManager.AddMessage("PackageReferenceRemoved" + metadata.Id);
        }

        private static void _installerEvents_PackageReferenceAdded(IVsPackageMetadata metadata)
        {
            TaskManager.AddMessage("PackageReferenceAdded" + metadata.Id);
            //throw new NotImplementedException();
        }

        public static void AuditInstalledPackages()
        {
            //var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            //IVsPackageInstallerServices installerServices = componentModel.GetService<IVsPackageInstallerServices>();
            //var installedPackages = installerServices.GetInstalledPackages();

            //TaskManager.AddError("OSSAuditor Error");
        }
    }
}
