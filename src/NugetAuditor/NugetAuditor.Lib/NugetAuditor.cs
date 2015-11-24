using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class NugetAuditor
    {
        private IEnumerable<AuditResult> AuditPackagesImpl(IEnumerable<PackageName> packageNames)
        {
            var client = new OSSIndexApi();

            var artifactSearches = packageNames.Select(x => new NugetArtifactSearch() { name = x.Id, version = x.Version });

            var artifacts = client.SearchArtifacts(artifactSearches);
            var scms = client.GetSCMs(artifacts.Where(x => x.ScmId > 0).Select(x => x.ScmId).Distinct());

            foreach (var packageName in packageNames)
            {
                var artifact = artifacts.Where(x => x.Name == packageName.Id && x.Version == packageName.Version).FirstOrDefault();
                var scm = artifact == null ? null : scms.Where(x => x.Id == artifact.ScmId).FirstOrDefault();
                var vulnerabilities = scm == null ? null : client.GetScmVulnerabilities(scm.Id);

                var result = new AuditResult(packageName, artifact, scm, vulnerabilities);
                                
                yield return result;
            }
        }

        public IList<AuditResult> AuditPackages(string path)
        {
            var packagesFile = new PackageReferenceFile(path);

            var packages = packagesFile.GetPackageReferences().Select(x => new PackageName() { Id = x.Id, Version = x.Version.ToString() });

            return this.AuditPackagesImpl(packages).ToList();
        }

        public IList<AuditResult> AuditPackages(IEnumerable<PackageName> packages)
        {
            return this.AuditPackagesImpl(packages).ToList();
        }
    }
}
