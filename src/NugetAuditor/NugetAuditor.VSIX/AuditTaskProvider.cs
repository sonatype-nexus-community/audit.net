using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using NuGet.VisualStudio;
using NugetAuditor.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseTask = Microsoft.VisualStudio.Shell.Task;

namespace NugetAuditor.VSIX
{
    public class AuditTaskProvider : TaskProvider
    {
        public AuditTaskProvider(IServiceProvider serviceProvider)
            :base(serviceProvider)
        {
            ProviderGuid = GuidList.guidAuditTaskProvider;
            ProviderName = "Audit.Net";
        }

        public void AddResults(ProjectInfo projectInfo, IEnumerable<Lib.AuditResult> auditResults)
        {
            foreach (var packageReference in projectInfo.PackageReferencesFile.GetPackageReferences())
            {
                var auditResult = auditResults.Where(x => x.PackageId.Equals(packageReference)).FirstOrDefault();

                if (auditResult == null )
                {
                    continue;
                }

                switch (auditResult.Status)
                {
                    case Lib.AuditStatus.NoKnownVulnerabilities:
                    case Lib.AuditStatus.UnknownPackage:
                    case Lib.AuditStatus.UnknownSource:
                        {
                            continue;
                        }
                    case Lib.AuditStatus.KnownVulnerabilities:
                        {
                            foreach (var vulnerability in auditResult.Vulnerabilities)
                            {
                                var auditTask = new AuditTask(auditResult, packageReference)
                                {
                                    ErrorCategory = TaskErrorCategory.Message,
                                    Text = vulnerability.Summary,
                                    HierarchyItem = projectInfo.ProjectHierarchy
                                };

                                Tasks.Add(auditTask);
                            }
                            break;
                        }
                    case Lib.AuditStatus.Vulnerable:
                        {
                            foreach (var vulnerability in auditResult.AffectingVulnerabilities)
                            {
                                var auditTask = new AuditTask(auditResult, packageReference)
                                {
                                    ErrorCategory = TaskErrorCategory.Error,
                                    Text = vulnerability.Summary,
                                    HierarchyItem = projectInfo.ProjectHierarchy
                                };

                                Tasks.Add(auditTask);
                            }
                            break;
                        }
                }
            }
        }

        public IEnumerable<T> GetTasks<T>() where T : VSIX.Task
        {
            return Tasks.OfType<T>();
        }      
    }
}
