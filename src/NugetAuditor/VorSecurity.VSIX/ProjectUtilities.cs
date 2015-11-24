using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NugetAuditor.VSIX
{
    static class ProjectUtilities
    {
        private static IServiceProvider _serviceProvider;

        public static void SetServiceProvider(IServiceProvider provider)
        {
            _serviceProvider = provider;
        }

        static public IEnumerable<IVsProject> GetLoadedProjects()
        {
            var solution = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution == null)
            {
                Debug.Fail("Failed to get SVsSolution service.");
                yield break;
            }

            IEnumHierarchies enumerator = null;
            Guid guid = Guid.Empty;
            solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out enumerator);
            IVsHierarchy[] hierarchy = new IVsHierarchy[1] { null };
            uint fetched = 0;
            for (enumerator.Reset(); enumerator.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1; /*nothing*/)
            {
                yield return (IVsProject)hierarchy[0];
            }
        }
    }
}
