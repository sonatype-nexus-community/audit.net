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

