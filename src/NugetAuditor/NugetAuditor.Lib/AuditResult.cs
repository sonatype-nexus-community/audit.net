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

using NugetAuditor.Lib.OSSIndex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class AuditResult
    {
        private PackageId _packageId;
        private Artifact _artifact;
        private SCM _scm;
        private IEnumerable<Vulnerability> _vulnerabilities;
        
        public PackageId PackageId
        {
            get
            {
                return this._packageId;
            }
        }

        public IEnumerable<Vulnerability> Vulnerabilities
        {
            get
            {
                if (this._vulnerabilities == null)
                {
                    this._vulnerabilities = Enumerable.Empty<Vulnerability>();
                }
                return this._vulnerabilities;
            }
        }

        public IEnumerable<Vulnerability> AffectingVulnerabilities
        {
            get
            {
                return this.Vulnerabilities.Where(x => x.AffectsVersion(this.PackageId.VersionString));
            }
        }

        public AuditStatus Status
        {
            get
            {
                if (this._artifact == null)
                {
                    return AuditStatus.UnknownPackage;
                }
                else if (this._scm == null)
                {
                    return AuditStatus.UnknownSource;
                }
                else if (this._scm.HasVulnerability == false)
                {
                    return AuditStatus.NoKnownVulnerabilities;
                }
                else
                {
                    return AuditStatus.HasVulnerabilities;
                }
            }
        }

        private AuditResult(PackageId packageId)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            this._packageId = packageId;
        }

        public AuditResult(PackageId packageId, Artifact artifact, SCM scm, IList<Vulnerability> vulnerabilities)
            : this(packageId)
        {
            this._artifact = artifact;
            this._scm = scm;
            this._vulnerabilities = vulnerabilities;
        }
    }    
}
