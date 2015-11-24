using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class AuditResult
    {
        private PackageName _packageName;
        private Artifact _artifact;
        private SCM _scm;
        private IList<Vulnerability> _vulnerabilities;

        public string Id
        {
            get
            {
                return _packageName.Id;
            }
        }

        public string Version
        {
            get
            {
                return _packageName.Version;
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
                    if (!this.AffectingVulnerabilities.Any())
                    {
                        return AuditStatus.KnownVulnerabilities;
                    }
                    else
                    {
                        return AuditStatus.Vulnerable;
                    }

                }
            }
        }

        public IEnumerable<Vulnerability> AffectingVulnerabilities
        {
            get
            {
                return this.Vulnerabilities.Where(x => x.Versions.Any(r => SemVer.Range.IsSatisfied(r, this._packageName.Version)));
            }
        }

        public IEnumerable<Vulnerability> Vulnerabilities
        {
            get
            {
                return this._vulnerabilities;
            }
        }

        public AuditResult(PackageName packageName, Artifact artifact, SCM scm, IList<Vulnerability> vulnerabilities)
        {
            if (packageName == null)
            {
                throw new ArgumentNullException("packageName");
            }

            this._packageName = packageName;
            this._artifact = artifact;
            this._scm = scm;

            if (vulnerabilities == null)
            {
                this._vulnerabilities = new List<Vulnerability>();
            }
            else
            {
                this._vulnerabilities = new List<Vulnerability>(vulnerabilities);
            }
        }
    }    
}
