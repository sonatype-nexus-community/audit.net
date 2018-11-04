using PackageUrl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
    [Serializable]
    public class Package
	{
		public string Coordinates { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public List<Vulnerability> Vulnerabilities { get; set; }
        public long CachedAt { get; set; }

        public string Name
        {
            get
            {
                PackageURL purl = new PackageURL(Coordinates);
                return purl.Name;
            }
        }
	}
}
