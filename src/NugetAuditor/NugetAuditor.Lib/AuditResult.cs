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
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var res = this.Vulnerabilities.Where(x => x.AffectsVersion(this.PackageId.VersionString));

                sw.Stop();

                System.Diagnostics.Trace.TraceInformation("Affecting elapsed for package {0}: {1}", this._packageId, sw.Elapsed);

                return res;

                //if (_affectingVulnerabilities == null)
                //{
                //    var sw = System.Diagnostics.Stopwatch.StartNew();

                //    _affectingVulnerabilities = this.Vulnerabilities.Where(x => x.AffectsVersion(this.PackageId.VersionString));
                    
                //    sw.Stop();

                //    System.Diagnostics.Trace.TraceInformation("Affecting elapsed for package {0}: {1}", this._packageId, sw.Elapsed);

                //}

                //return _affectingVulnerabilities;
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

                    //if (!this.AffectingVulnerabilities.Any())
                    //{
                    //    return AuditStatus.HasVulnerabilities;
                    //}
                    //else
                    //{
                    //    return AuditStatus.Vulnerable;
                    //}
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
