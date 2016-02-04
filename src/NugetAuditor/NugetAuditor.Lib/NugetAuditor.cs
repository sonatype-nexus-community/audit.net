// Copyright (c) 2015-2016, Vör Security Ltd.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Vör Security, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL VÖR SECURITY BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#define BATCH

using NugetAuditor.Lib.OSSIndex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;

namespace NugetAuditor.Lib
{
    public class NugetAuditor
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
            var client = new OSSIndex.ApiClient(cachePolicy) as Lib.OSSIndex.IApiClient;

            var packageIds = packages.ToList();
#if (BATCH)
            var artifactSearches = packageIds.Select(x => new NugetArtifactSearch() { name = x.Id, version = x.VersionString });
            var artifacts = client.SearchArtifacts(artifactSearches);
            var projects = client.GetProjects(artifacts.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId.Value).Distinct());
#endif
            foreach (var packageId in packageIds)
            {
                Lib.OSSIndex.Artifact artifact = null;
                Lib.OSSIndex.Project project = null;
                IList<Lib.OSSIndex.Vulnerability> vulnerabilities = null;
#if (!BATCH)
                try
                {
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

                    if (artifact != null && artifact.ProjectId.HasValue)
                    {
#if (BATCH)
                        project = projects.Where(x => x.Id == artifact.ProjectId).FirstOrDefault();
#else
                        scm = client.GetSCM(artifact.ScmId.Value).FirstOrDefault();
#endif
                        if (project != null && project.HasVulnerability)
                        {
                            vulnerabilities = client.GetVulnerabilities(project.Id);
                        }
                    }

#if (!BATCH)
                }
                catch (ApiClientException e)
                {
                    System.Diagnostics.Trace.TraceError("ApiClient call failed. \n{0}", e.ToString());
                }
#endif
                yield return new AuditResult(packageId, artifact, project, vulnerabilities);
            }
        }

        public static IEnumerable<AuditResult> AuditPackages(string path)
        {
            return AuditPackages(path, 0);
        }

        public static IEnumerable<AuditResult> AuditPackages(string path, int cacheSync)
        {
            var packagesFile = new PackageReferencesFile(path);

            var packages = packagesFile.GetPackageReferences().Select(x => x.PackageId);

            return AuditPackagesImpl(packages, cacheSync).ToList();
        }

        public static IEnumerable<AuditResult> AuditPackages(IEnumerable<PackageId> packages)
        {
            return AuditPackages(packages, 0);
        }

        public static IEnumerable<AuditResult> AuditPackages(IEnumerable<PackageId> packages, int cacheSync)
        {
            return AuditPackagesImpl(packages, cacheSync).ToList();
        }
    }
}
