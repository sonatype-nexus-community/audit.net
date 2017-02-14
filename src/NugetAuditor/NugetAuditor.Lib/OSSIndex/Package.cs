using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
	public class Package
	{
		public long Id { get; set; }
		[RestSharp.Deserializers.DeserializeAs(Name = "pm")]
		[RestSharp.Serializers.SerializeAs(Name = "pm")]
		public string PackageManager { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }
		public int VulnerabilityTotal { get; set; }
		public int VulnerabilityMatches { get; set; }
		public List<Vulnerability> Vulnerabilities { get; set; }
	}
}
