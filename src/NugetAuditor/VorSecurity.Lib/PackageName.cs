using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class PackageName
    {
        public string Id { get; set; }
        public string Version { get; set; }

        public string Key()
        {
            return string.Format("{0}.{1}", this.Id, this.Version);
        }
    }
}
