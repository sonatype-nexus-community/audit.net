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

using CommandLine;
using NugetAuditor.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = new Options();

            if (!Parser.Default.ParseArgumentsStrict(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return -1;
            }
            
            var packagePath = Path.GetFullPath(options.Package);

            if (!File.Exists(packagePath))
            {
                Console.WriteLine("Package file \"{0}\" not found.", options.Package);
                Console.WriteLine(options.GetUsage());
                return -2;
            }

            if (options.Verbose)
            {
                Console.WriteLine("Auditing package file \"{0}\".", options.Package);
                Console.WriteLine();
            }

            try
            {
                var auditResults = Lib.NugetAuditor.AuditPackages(packagePath, options.CacheSync);

                var totalPackages = auditResults.Count();
                var vulnerablePackages = 0;
                var packageNum = 1;

                foreach (var auditResult in auditResults)
                {
                    Console.Write("[{0}/{1}] ", packageNum++, totalPackages);
                    Console.Write("{0} {1} ", auditResult.PackageId.Id, auditResult.PackageId.VersionString);

                    switch (auditResult.Status)
                    {
                        case Lib.AuditStatus.UnknownPackage:
                            {
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine("Unknown package.");
                                Console.ResetColor();
                                break;
                            }
                        case Lib.AuditStatus.UnknownSource:
                            {
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine("Unknown source for package.");
                                Console.ResetColor();
                                break;
                            }
                        case Lib.AuditStatus.NoActiveVulnerabilities:
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("No known vulnerabilities");
                                Console.ResetColor();
                                break;
                            }
                        case Lib.AuditStatus.HasVulnerabilities:
                            {
                                if (auditResult.Vulnerabilities.Any())
                                {
                                    vulnerablePackages++;
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[VULNERABLE]");
                                    Console.ResetColor();
                                }

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("{0} vulnerabilities affecting installed version", auditResult.MatchedVulnerabilities);
                                Console.ResetColor();

                                foreach (var item in auditResult.Vulnerabilities)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine(item.Title);
                                    Console.WriteLine(item.Description);
                                }

                                break;
                            }
                        //case Lib.AuditStatus.Vulnerable:
                        //    {
                        //        vulnerablePackages++;
                        //        Console.ForegroundColor = ConsoleColor.Red;
                        //        Console.WriteLine("[VULNERABLE]");
                        //        Console.ResetColor();
                        //        Console.WriteLine("{0} known vulnerabilities, {1} affecting installed version", auditResult.Vulnerabilities.Count(), auditResult.AffectingVulnerabilities.Count());

                        //        foreach (var item in auditResult.AffectingVulnerabilities)
                        //        {
                        //            Console.WriteLine();
                        //            Console.WriteLine(item.Title);
                        //            Console.WriteLine(item.Summary);
                        //        }
                        //        break;
                        //    }
                    }
                }

                Console.WriteLine();

                if (options.Verbose)
                {
                    Console.WriteLine("Done auditing package file \"{0}\".", options.Package);
                }

                return vulnerablePackages;
            }
            catch (Exception exception)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed: \"{0}\".", exception.Message);
                Console.ResetColor();
                Console.WriteLine();

                return -3;
            }
            finally
            {
                if (options.Verbose)
                {
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
