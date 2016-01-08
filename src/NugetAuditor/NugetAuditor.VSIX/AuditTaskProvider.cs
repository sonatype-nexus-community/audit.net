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
    public class AuditTaskProvider : VSIX.TaskProvider
    {
        public AuditTaskProvider(IServiceProvider serviceProvider)
            :base(serviceProvider)
        {
            ProviderGuid = GuidList.guidAuditTaskProvider;
            ProviderName = "Audit.Net";
        }

        public void AddResults(ProjectInfo projectInfo, IEnumerable<Lib.AuditResult> auditResults)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var packageReference in projectInfo.PackageReferencesFile.GetPackageReferences())
            {
                var auditResult = auditResults.Where(x => x.PackageId.Equals(packageReference)).FirstOrDefault();

                if (auditResult == null 
                    || auditResult.Status ==  AuditStatus.NoKnownVulnerabilities 
                    || auditResult.Status == AuditStatus.UnknownPackage
                    || auditResult.Status == AuditStatus.UnknownSource)
                {
                    continue;
                }

                foreach (var vulnerability in auditResult.Vulnerabilities)
                {
                    var task = new VulnerabilityTask(vulnerability, packageReference)
                    {
                        ErrorCategory = auditResult.AffectingVulnerabilities.Contains(vulnerability) ? TaskErrorCategory.Error : TaskErrorCategory.Message,
                        Text = string.Format("{0}\n{1}", vulnerability.Title, vulnerability.Summary),
                        HierarchyItem = projectInfo.ProjectHierarchy,
                        HelpKeyword = vulnerability.CveId,
                    };

                    Tasks.Add(task);
                }
            }

            watch.Stop();
            System.Diagnostics.Trace.TraceInformation("Elapsed AddResults: {0}", watch.Elapsed);
        }

        public IEnumerable<T> GetTasks<T>() where T : VSIX.Task
        {
            return Tasks.OfType<T>();
        }      
    }
}
