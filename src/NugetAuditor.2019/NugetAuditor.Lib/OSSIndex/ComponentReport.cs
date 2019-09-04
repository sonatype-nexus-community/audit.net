using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
    public class ComponentReport
    {
        public ComponentReport() { }

        public IEnumerable<string> coordinates { get; set; }

        public string description { get; set; }

        public string reference { get; set; }

        public List<Vulnerability> Vulnerabilities { get; set; }
    }
}
