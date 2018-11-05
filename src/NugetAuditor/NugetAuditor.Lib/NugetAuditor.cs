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

using NugetAuditor.Lib.OSSIndex;
using PackageUrl;
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

        private static IEnumerable<AuditResult> AuditPackagesImpl(IEnumerable<PackageId> packageIds, int cacheSync)
        {
            var cachePolicy = CachePolicy(cacheSync);
            var client = new OSSIndex.ApiClient(cachePolicy) as Lib.OSSIndex.IApiClient;

            // NugetPackageSearch(x.Id, version = x.VersionString)
            var packageSearches = packageIds.Select(x => (new PackageURL("nuget", null, x.Id, x.VersionString, null, null)));
            var packages = client.SearchPackages(packageSearches);

            foreach (var packageId in packageIds)
            {
                Lib.OSSIndex.Package package = null;
				//find first 
				package = packages.FirstOrDefault(x => x.Name == packageId.Id);
                yield return new AuditResult(packageId, package);
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
