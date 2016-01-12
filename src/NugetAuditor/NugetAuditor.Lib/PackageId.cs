using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class PackageId 
    {
        private string _originalVersion;

        public string Id
        {
            get;
            private set;
        }

        public NuGetVersion Version
        {
            get;
            private set;
        }

        public string VersionString
        {
            get
            {
                return this.Version.ToNormalizedString();
            }
        }

        public PackageId(string id, string version)
            :this(id, NuGetVersion.Parse(version))
        {
            this._originalVersion = version;
        }

        private PackageId(string id, NuGetVersion version)
        {
            this.Id = id;
            this.Version = version;
        }

        public override bool Equals(object obj)
        {
            PackageId other = obj as PackageId;

            if (other == null)
            {
                return false;
            }

            return (this.Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && this.VersionString.Equals(other.VersionString, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode()
        {
            int h1 = this.Id.GetHashCode();
            int h2 = this.Version.GetHashCode();

            return (((h1 << 5) + h1) ^ h2);
        }

        public override string ToString()
        {
            return (this.Id + " " + this.Version);
        }
    }
}
