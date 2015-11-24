using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* 
{
  "id": 7098693681,
  "name": "0xdeafcafe.Bropack",
  "version": "1.2.1",
  "description": "Shared stuff for vnext projects. hi xox",
  "package_manager": "nuget",
  "package": "https://ossindex.net/v1.0/package/7098693652",
  "package_id": 7098693652,
  "scm": "https://ossindex.net/v1.0/scm/7098693659",
  "scm_id": 7098693659,
  "url": "https://www.nuget.org/api/v2/package/0xdeafcafe.Bropack/1.2.1",
  "details": "https://ossindex.net/v1.0/artifact/7098693681",
  "dependencies": "https://ossindex.net/v1.0/artifact/7098693681/dependencies",
  "search": [
    "nuget",
    "0xdeafcafe.Bropack",
    "https://www.nuget.org/api/v2/package/0xdeafcafe.Bropack/1.2.1",
    "1.2.1"
  ]
}
*/

namespace NugetAuditor.Lib
{
	public class Artifact
    {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }
		public string Description { get; set; }
		public string PackageManager { get; set; }
		public string Package { get; set; }
		public long PackageId { get; set; }
		public string Scm { get; set; }
		public long ScmId { get; set; }
		public string Url { get; set; }
		public string Details { get; set; }
		public string Dependencies { get; set; }
		public List<string> Search { get; set; }
    }
}
