using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NugetAuditor;

namespace NugetAuditor.Tests
{
    [TestClass]
    public class NugetAuditorLibTests
    {
        [TestMethod]
        public void CanAuditFiles()
        {
            var f3 = Path.Combine("TestFiles", "packages.config.example.3");
            Assert.IsTrue(File.Exists(f3));
            var results = Lib.NugetAuditor.AuditPackages(f3, 0, new NuGet.Common.NullLogger());
            Assert.AreNotEqual(0, results.Count(r => r.MatchedVulnerabilities > 0));
        }
    }
}
