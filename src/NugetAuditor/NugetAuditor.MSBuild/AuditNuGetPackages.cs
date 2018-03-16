using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NugetAuditor.Lib;

namespace NugetAuditor.MSBuild
{
    public class AuditNuGetPackages : Task
    {
        private const string SENDER_NAME="AuditNugetPackages";
        public string ProjectPath { get; set; }

        public override bool Execute()
        {
            var packagePath = Path.Combine(ProjectPath, "packages.config");
            var whiteListPath = Path.Combine(ProjectPath, @"..\packages-whitelist.config");

            if (!File.Exists(packagePath))
            {
                // Why would anyone install this without packages.config, fail for them.
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs("No packages.config found", string.Empty, "SafeNuGet", MessageImportance.High));
                return false;
            }

            WhiteListed[] whiteList = null;
            if (File.Exists(whiteListPath))
            {
                var serializer = new XmlSerializer(typeof (WhiteListed[]));

                using (var sr = new StreamReader(whiteListPath))
                {
                    whiteList = (WhiteListed[]) serializer.Deserialize(sr);
                    sr.Close();
                }
            }

            IEnumerable<AuditResult> auditResults;
            try
            {
                auditResults = Lib.NugetAuditor.AuditPackages(packagePath).ToList();
            }
            catch (Exception ex)
            {
                BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, packagePath, 0, 0, 0, 0, "Unable to check packages for Vulnerabilities: " + ex, string.Empty, SENDER_NAME));
                return true; // fail as OK
            }

            var vulnerablePackages = 0;
            var packageNum = 1;

            foreach (var auditResult in auditResults)
            {
                WhiteListed whiteListed = null;
                if (whiteList != null)
                {
                    whiteListed = whiteList.ToList().FirstOrDefault(x => x.Id == auditResult.PackageId.Id && x.Version == auditResult.PackageId.VersionString);
                    if (whiteListed != null && (whiteListed.DateAccepted > DateTime.Now.AddDays(-90) || whiteListed.Permanent))
                    {
                        var permAccept = whiteListed.Permanent ? "(Permanent)" : string.Empty;
                        BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, packagePath, packageNum, 0, 0, 0, 
                            $"package whitelisted {permAccept}: {auditResult.PackageId.Id} - {auditResult.PackageId.VersionString}", "", SENDER_NAME));
                        continue;
                    }
                }

                switch (auditResult.Status)
                {
                    case AuditStatus.UnknownPackage:
                        BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, packagePath, packageNum, 0, 0, 0, "Unknown package: " +auditResult.PackageId.Id + " - " + auditResult.PackageId.VersionString, "", SENDER_NAME));
                        break;

                    case AuditStatus.UnknownSource:
                        BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, packagePath, packageNum, 0, 0, 0, "Unknown source for package: " + auditResult.PackageId.Id + " - " + auditResult.PackageId.VersionString, "", SENDER_NAME));
                        break;

                    case AuditStatus.NoKnownVulnerabilities:
                        // that is nice :-)
                        break;

                    case AuditStatus.HasVulnerabilities:
                        if (auditResult.Vulnerabilities.Any())
                        {
                            vulnerablePackages++;
                            if (whiteListed == null)
                            {
                                BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, packagePath, packageNum, 0, 0, 0, "Security Vulnerability: " + auditResult.PackageId.Id + " - " + auditResult.PackageId.VersionString, "", SENDER_NAME));
                            }
                            else
                            {
                                BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, packagePath, packageNum, 0, 0, 0, "Security Vulnerability (white listing expired): " + auditResult.PackageId.Id + " - " + auditResult.PackageId.VersionString, "", SENDER_NAME));
                            }
                        }

                        foreach (var item in auditResult.Vulnerabilities)
                        {
                            BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, packagePath, packageNum, 0, 0, 0, item.Title + " " + item.Description, "", SENDER_NAME));
                        }

                        break;
                }
                packageNum++;
            }

            if (vulnerablePackages > 0) return false;
            else return true;
        }
    }
}
