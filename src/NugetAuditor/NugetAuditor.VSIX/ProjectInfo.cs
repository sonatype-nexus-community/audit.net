using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    public class ProjectInfo : IDisposable
    {
        private Project _project;
        private Lib.PackageReferencesFile _packageReferencesFile;
        private IVsHierarchy _projectHierarchy;

        public Lib.PackageReferencesFile PackageReferencesFile
        {
            get
            {
                if (_packageReferencesFile == null)
                {
                    var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_project.FullName), "packages.config");

                    _packageReferencesFile = new Lib.PackageReferencesFile(path);
                }

                return _packageReferencesFile;
            }
        }

      
        public IVsHierarchy ProjectHierarchy
        {
            get
            {
                if (_projectHierarchy == null)
                {
                    _projectHierarchy = AuditHelper.GetProjectHierarchy(_project);
                }

                return _projectHierarchy;
            }
        }

        public ProjectInfo(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            _project = project;
        }

        public void Dispose()
        {
            _project = null;
            _packageReferencesFile = null;
            _projectHierarchy = null;
        }
    }
}
