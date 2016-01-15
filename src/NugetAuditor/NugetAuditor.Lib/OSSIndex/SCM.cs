// Copyright (c) 2015-2016, Vör Security Ltd.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Vör Security, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL VÖR SECURITY BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
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
