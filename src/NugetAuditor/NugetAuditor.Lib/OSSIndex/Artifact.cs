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

/* 
{
    "id": 7102540843,
    "name": "jQuery",
    "version": "1.6.1",
    "description": "",
    "package_manager": "nuget",
    "package": "http://localhost:8080/v1.1/package/8396450687",
    "package_id": 8396450687,
    "scm": "http://localhost:8080/v1.1/scm/8322029565",
    "scm_id": 8322029565,
    "project": "http://localhost:8080/v1.1/project/8396559329",
    "project_id": 8396559329,
    "url": "https://www.nuget.org/api/v2/package/jQuery/1.6.1",
    "details": "http://localhost:8080/v1.1/artifact/7102540843",
    "dependencies": "http://localhost:8080/v1.1/artifact/7102540843/dependencies",
    "search": [
      "nuget",
      "jquery",
      "null",
      "1.6.1"
    ]
  }
*/

namespace NugetAuditor.Lib.OSSIndex
{
	public class Artifact
    {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }
		public string Description { get; set; }
		public string PackageManager { get; set; }
		public string Package { get; set; }
		public long? PackageId { get; set; }
        public string Scm { get; set; }
        public long? ScmId { get; set; }
        public string Project { get; set; }
        public long? ProjectId { get; set; }
        public string Url { get; set; }
		public string Details { get; set; }
		public string Dependencies { get; set; }
		public List<string> Search { get; set; }
    }
}
