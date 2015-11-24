using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
/*
[
  {
    "id": 296375846,
    "uri": "https://github.com/jquery/jquery.git",
    "name": "jquery",
    "description": "jQuery JavaScript Library",
    "size": 21758586,
    "scm_type": "GIT",
    "cpes": [
      {
        "cpecode": "cpe:/a:jquery:jquery",
        "cpe": "https://ossindex.net/v1.0/cpe/a/jquery/jquery"
      }
    ],
    "requires": "https://ossindex.net/v1.0/scm/296375846/requires",
    "hasVulnerability": true,
    "vulnerabilities": "https://ossindex.net/v1.0/scm/296375846/vulnerabilities",
    "references": "https://ossindex.net/v1.0/scm/296375846/references",
    "artifacts": "https://ossindex.net/v1.0/scm/296375846/artifacts",
    "releases": "https://ossindex.net/v1.0/scm/296375846/releases",
    "files": "https://ossindex.net/v1.0/scm/296375846/files",
    "authors": "https://ossindex.net/v1.0/scm/296375846/authors",
    "languages": "https://ossindex.net/v1.0/scm/296375846/languages"
  }
]
*/
    public class SCM
    {
        public long Id { get; set; }
        public string Uri { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long Size { get; set; }
        public string ScmType { get; set; }
        public List<CPE> Cpes { get; set; }
        public string Requires { get; set; }
        public bool HasVulnerability { get; set; }
        public string Vulnerabilities { get; set; }
        public string References { get; set; }
        public string Artifacts { get; set; }
        public string Releases { get; set; }
        public string Files { get; set; }
        public string Authors { get; set; }
        public string Languages { get; set; }
    }

    public class CPE
    {
        public string Cpecode { get; set; }
        public string Cpe { get; set; }
    }
}
