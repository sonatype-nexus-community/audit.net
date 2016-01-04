using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class NugetAuditor //: INugetAuditor
    {
        private static HttpRequestCachePolicy CachePolicy(int cacheSync)
        {
            switch (cacheSync)
            {
                case -1:
                    {
                        return new HttpRequestCachePolicy(HttpRequestCacheLevel.BypassCache);
                    }
                case 0:
                    {
                        return new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
                    }
                default:
                    {
                        return new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAge, TimeSpan.FromMinutes(cacheSync));
                    }
            }
        }

        private static IEnumerable<AuditResult> AuditPackagesImpl(IEnumerable<PackageId> packages, int cacheSync)
        {
            var cachePolicy = CachePolicy(cacheSync);

            var client = new OSSIndex.ApiClient(cachePolicy);

            var artifactSearches = packages.Select(x => new NugetArtifactSearch() { name = x.Id, version = x.Version });

            var artifacts = client.SearchArtifacts(artifactSearches);
            var scms = client.GetSCMs(artifacts.Where(x => x.ScmId > 0).Select(x => x.ScmId).Distinct());

            foreach (var package in packages)
            {
                var artifact = artifacts.Where(x => x.Name == package.Id && x.Version == package.Version).FirstOrDefault();
                var scm = artifact == null ? null : scms.Where(x => x.Id == artifact.ScmId).FirstOrDefault();
                var vulnerabilities = scm == null ? null : client.GetScmVulnerabilities(scm.Id);

                var result = new AuditResult(package, artifact, scm, vulnerabilities);
                                
                yield return result;
            }
        }

        public static IList<AuditResult> AuditPackages(string path)
        {
            return AuditPackages(path, 0);
        }

        public static IList<AuditResult> AuditPackages(string path, int cacheSync)
        {
            var packagesFile = new PackageReferencesFile(path);

            var packages = packagesFile.GetPackageReferences();

            return AuditPackagesImpl(packages, cacheSync).ToList();
        }

        public static IList<AuditResult> AuditPackages(IEnumerable<PackageId> packages)
        {
            return AuditPackages(packages, 0);
        }

        public static IList<AuditResult> AuditPackages(IEnumerable<PackageId> packages, int cacheSync )
        {
            return AuditPackagesImpl(packages, cacheSync).ToList();
        }

        public static Task<List<AuditResult>> AuditPackagesAsync(IEnumerable<PackageId> packages)
        {
            return AuditPackagesAsync(packages, 0);
        }

        public static Task<List<AuditResult>> AuditPackagesAsync(IEnumerable<PackageId> packages, int cacheSync)
        {
            return new Task<List<AuditResult>>(() => AuditPackagesImpl(packages, cacheSync).ToList());
        }
    }
}
