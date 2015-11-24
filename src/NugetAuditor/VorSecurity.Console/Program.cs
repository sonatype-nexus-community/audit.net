using CommandLine;
using Newtonsoft.Json;
using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            var errNumber = 0;

            var options = new Options();

            if (!Parser.Default.ParseArgumentsStrict(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return -1;
            }
            
            var packagePath = System.IO.Path.GetFullPath(options.Package);

            if (!System.IO.File.Exists(packagePath))
            {
                Console.WriteLine("File not found.");
                return -2;
            }

            Console.WriteLine("Auditing package file \"{0}\".", options.Package);
            Console.WriteLine();

            var auditor = new Lib.NugetAuditor();

            var auditResults = auditor.AuditPackages(packagePath);

            var totalPackages = auditResults.Count();
            var packageNum = 1;

            foreach (var res in auditResults)
            {
                Console.Write("[{0}/{1}] ", packageNum++, totalPackages, res.Id, res.Version);

                Action outPackageName = () => { Console.Write("{0} {1} ", res.Id, res.Version); };

                switch (res.Status)
                {
                    case Lib.AuditStatus.UnknownPackage:
                        {
                            outPackageName();
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("Unknown package.");
                            Console.ResetColor();
                            break;
                        }
                    case Lib.AuditStatus.UnknownSource:
                        {
                            outPackageName();
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Unknown source for package.");
                            Console.ResetColor();
                            break;
                        }
                    case Lib.AuditStatus.NoKnownVulnerabilities:
                        {
                            outPackageName();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("No known vulnerabilities");
                            Console.ResetColor();
                            break;
                        }
                    case Lib.AuditStatus.KnownVulnerabilities:
                        {
                            outPackageName();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("{0} known vulnerabilities, {1} affecting installed version", res.Vulnerabilities.Count(), res.AffectingVulnerabilities.Count());
                            Console.ResetColor();
                            break;
                        }
                    case Lib.AuditStatus.Vulnerable:
                        {
                            outPackageName();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[VULNERABLE]");
                            Console.ResetColor();
                            Console.WriteLine("{0} known vulnerabilities, {1} affecting installed version", res.Vulnerabilities.Count(), res.AffectingVulnerabilities.Count());
                            
                            foreach (var item in res.AffectingVulnerabilities)
                            {
                                Console.WriteLine();
                                Console.WriteLine(item.Title);
                                Console.WriteLine(item.Summary);
                            }

                            break;
                        }
                }
            }
            Console.WriteLine();
            Console.WriteLine("Done auditing package file \"{0}\".", options.Package);
            Console.ReadLine();
            return errNumber;
        }
    }
}
