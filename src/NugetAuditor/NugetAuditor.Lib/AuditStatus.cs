using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public enum AuditStatus
    {
        NoKnownVulnerabilities = 0,
        KnownVulnerabilities,
        Vulnerable,
        UnknownSource,
        UnknownPackage
    }
}
                    