using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace NugetAuditor.VSIX
{
    public static class ServiceLocator
    {
        private static TService GetComponentModelService<TService>() where TService : class
        {
            return GetGlobalService<SComponentModel, IComponentModel>().GetService<TService>();
        }

        private static TService GetDTEService<TService>() where TService : class
        {
            return (TService)QueryService(GetGlobalService<SDTE, DTE>(), typeof(TService));
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class
        {
            if (PackageServiceProvider != null)
            {
                TInterface service = PackageServiceProvider.GetService(typeof(TService)) as TInterface;
                if (service != null)
                {
                    return service;
                }
            }
            return (TInterface)Package.GetGlobalService(typeof(TService));
        }

        public static TService GetInstance<TService>() where TService : class
        {
            if (typeof(TService) == typeof(IServiceProvider))
            {
                return (TService)GetServiceProvider();
            }
            TService dTEService = GetDTEService<TService>();
            if (dTEService != null)
            {
                return dTEService;
            }
            TService componentModelService = GetComponentModelService<TService>();
            if (componentModelService != null)
            {
                return componentModelService;
            }
            return GetGlobalService<TService, TService>();
        }

        private static IServiceProvider GetServiceProvider()
        {
            return GetServiceProvider(GetGlobalService<SDTE, DTE>());
        }

        private static IServiceProvider GetServiceProvider(_DTE dte)
        {
            return new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
        }

        public static void InitializePackageServiceProvider(IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            PackageServiceProvider = provider;
        }

        private static object QueryService(_DTE dte, Type serviceType)
        {
            IntPtr ptr;
            Guid gUID = serviceType.GUID;
            Guid riid = gUID;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider provider = dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
            if (provider.QueryService(ref gUID, ref riid, out ptr) != 0)
            {
                return null;
            }
            object objectForIUnknown = null;
            if (ptr != IntPtr.Zero)
            {
                objectForIUnknown = Marshal.GetObjectForIUnknown(ptr);
                Marshal.Release(ptr);
            }
            return objectForIUnknown;
        }

        public static IServiceProvider PackageServiceProvider
        {
            get;
            private set;
        }
    }
}

