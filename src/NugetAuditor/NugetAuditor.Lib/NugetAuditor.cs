#define BATCH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;

namespace NugetAuditor.Lib
{
    public class NugetAuditor
    {
        const string pm = "nuget";

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

        private static IEnumerable<AuditResult> AuditPackagesImpl(IEnumerable<PackageId> packageIds, int cacheSync)
        {
            var cachePolicy = CachePolicy(cacheSync);

            var client = new OSSIndex.ApiClient(cachePolicy) as Lib.OSSIndex.IApiClient;
#if (BATCH)
            var artifactSearches = packageIds.Select(x => new NugetArtifactSearch() { name = x.Id, version = x.VersionString });
            var artifacts = client.SearchArtifacts(artifactSearches);
            var scms = client.GetSCMs(artifacts.Where(x => x.ScmId.HasValue).Select(x => x.ScmId.Value).Distinct());
#endif
            foreach (var packageId in packageIds)
            {
                Lib.OSSIndex.Artifact artifact = null;
                Lib.OSSIndex.SCM scm = null;
                IList<Lib.OSSIndex.Vulnerability> vulnerabilities = null;
#if (!BATCH)
                var search = new NugetArtifactSearch() { name = packageId.Id, version = packageId.VersionString };
                var artifacts = client.SearchArtifact(search);
#endif
                //find first by exact version
                artifact = artifacts.FirstOrDefault(x => x.Name == packageId.Id && x.Version == packageId.VersionString);

                if (artifact == null)
                {
                    //find first by search version
                    artifact = artifacts.FirstOrDefault(x => x.Search.Contains(packageId.Id) && x.Search.Contains(packageId.VersionString));
                }

                if (artifact != null && artifact.ScmId.HasValue)
                {
#if (BATCH)
                    scm = scms.Where(x => x.Id == artifact.ScmId).FirstOrDefault();
#else
                    scm = client.GetSCM(artifact.ScmId.Value).FirstOrDefault();
#endif
                    if (scm != null && scm.HasVulnerability)
                    {
                        vulnerabilities = client.GetVulnerabilities(scm.Id);
                    }
                }

                yield return new AuditResult(packageId, artifact, scm, vulnerabilities);
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
    }
}
