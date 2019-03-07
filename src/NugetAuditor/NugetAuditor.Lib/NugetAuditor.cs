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

using NuGet.Protocol.Core.Types;
using NugetAuditor.Lib.OSSIndex;
using PackageUrl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Cache;
using NuGet.Configuration;
using System.Threading;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Packaging;
using System.Runtime.Caching;
using System.IO;
using Newtonsoft.Json;

namespace NugetAuditor.Lib
{
    public class NugetAuditor
    {
        private static FileCache depCache = null;

        private static void initDepCache()
        {
            if (depCache == null)
            {
                // Get an appropriate place for the cache and initialize it
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(directory, "OSSIndex", "depCache");
                depCache = new FileCache(path, new ObjectBinder());
            }
        }

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

        private static IEnumerable<AuditResult> AuditPackagesImpl(IEnumerable<PackageId> packagesToAudit, int cacheSync, ILogger logger)
        {
            IEnumerable<PackageId> packageIds = getDependencies(packagesToAudit, logger);

            var cachePolicy = CachePolicy(cacheSync);
            var client = new OSSIndex.ApiClient(cachePolicy, logger) as Lib.OSSIndex.IApiClient;

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

        private static IEnumerable<PackageId> getDependencies(IEnumerable<PackageId> packages, ILogger logger)
        {
            try
            {
                initDepCache();

                HashSet<PackageId> visited = new HashSet<PackageId>();
                Queue<PackageId> todo = new Queue<PackageId>(packages);
                logger.LogDebug("Finding dependencies...");
                while (todo.Count > 0)
                {                    
                    PackageId pkg = todo.Dequeue();
                    string purl = "pkg:nuget/" + pkg.Id + "@" + pkg.Version;
                    try
                    {
                        if (!visited.Contains(pkg))
                        {
                            visited.Add(pkg);
                            HashSet<PackageId> deps = null;
                            if (depCache.Contains(purl))
                            {
                               
                                string json = (string)depCache[purl];
                                deps = JsonConvert.DeserializeObject<HashSet<PackageId>>(json);
                                
                            }
                            else
                            {
                                logger.LogDebug("  Fetch dependencies for " + purl);
                                deps = getDependencies(pkg);
                                if (deps != null)
                                {
                                    depCache[purl] = deps.ToJson();
                                }
                            }
                            foreach (PackageId dep in deps)
                            {
                                todo.Enqueue(dep);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogDebug($"An error ocurred retrieving dependencies for package {purl.ToString()}: {e.Message}.");
                        logger.LogInformation($"Skipping dependencies for package {purl.ToString()}.");  
                    }
                }
                logger.LogDebug("  done.");
                return visited;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static HashSet<PackageId> getDependencies(PackageId pkg)
        {
            HashSet<PackageId> results = new HashSet<PackageId>();

            Logger logger = new Logger();
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
            //providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
            PackageSource packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            SourceRepository sourceRepository = new SourceRepository(packageSource, providers);
            PackageMetadataResource packageMetadataResource = sourceRepository.GetResource<PackageMetadataResource>();
            var task = packageMetadataResource.GetMetadataAsync(pkg.Id, true, true, logger, CancellationToken.None);
            task.Wait();
            IEnumerable<IPackageSearchMetadata> searchMetadata = task.Result;
            foreach (IPackageSearchMetadata metadata in searchMetadata)
            {
                if (pkg.Version == metadata.Identity.Version)
                {
                    foreach (PackageDependencyGroup deps in metadata.DependencySets)
                    {
                        foreach (var dep in deps.Packages) {
                            results.Add(new PackageId(dep.Id, dep.VersionRange.MinVersion.ToNormalizedString()));
                        }
                    }
                    break;
                }
            }
            return results;
        }

        public static IEnumerable<AuditResult> AuditPackages(string path, ILogger logger)
        {
            return AuditPackages(path, 0, logger);
        }

        public static IEnumerable<AuditResult> AuditPackages(string path, int cacheSync, ILogger logger)
        {
            var packagesFile = new PackageReferencesFile(path);

            var packages = packagesFile.GetPackageReferences().Select(x => x.PackageId);

            return AuditPackagesImpl(packages, cacheSync, logger).ToList();
        }

        public static IEnumerable<AuditResult> AuditPackages(IEnumerable<PackageId> packages, ILogger logger)
        {
            return AuditPackages(packages, 0, logger);
        }

        public static IEnumerable<AuditResult> AuditPackages(IEnumerable<PackageId> packages, int cacheSync, ILogger logger)
        {
            return AuditPackagesImpl(packages, cacheSync, logger).ToList();
        }

        public static IEnumerable<AuditResult> auditPackage(string name, string version, ILogger logger)
        {
            var pkg = new PackageId(name, version);
            Collection<PackageId> packages = new Collection<PackageId>();
            packages.Add(new PackageId(name, version));
            return AuditPackagesImpl(packages, 0, logger).ToList();
        }
    }
    public class Logger : ILogger
    {
        void ILogger.LogDebug(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogError(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogErrorSummary(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogInformation(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogInformationSummary(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogMinimal(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogVerbose(string data)
        {
            Console.WriteLine(data);
        }

        void ILogger.LogWarning(string data)
        {
            Console.WriteLine(data);
        }
    }
}
