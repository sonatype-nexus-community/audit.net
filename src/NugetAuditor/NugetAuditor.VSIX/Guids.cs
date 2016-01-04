using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    static class GuidList
    {
        public const string guidAuditPkgString = "6f208d03-bc05-4a29-b715-0460c9023754";

        public const string guidAuditCmdSetString = "90c8506f-9b1d-40ae-862d-5bfe33e674c0";
        public const string guidAuditTaskProviderString = "61750098-47b9-4629-8bc2-e3478de30381";

        // any project system that wants to load NuGet when its project opens needs to activate a UI context with this GUID
        //public const string guidAutoLoadNuGetString = "65B1D035-27A5-4BBA-BAB9-5F61C1E2BC4A";

        public static readonly Guid guidAuditCmdSet = new Guid(guidAuditCmdSetString);
        public static readonly Guid guidAuditTaskProvider = new Guid(guidAuditTaskProviderString);
    }

}
