using System;

namespace NugetAuditor.Lib
{
    [Serializable]
    public class WhiteListed
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public DateTime DateAccepted { get; set; }
        public string Reason { get; set; }
        public bool Permanent { get; set; }
    }
}
