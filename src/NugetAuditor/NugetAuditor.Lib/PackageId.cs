using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class PackageId 
    {
        public string Id
        {
            get;
            private set;
        }

        public string Version
        {
            get;
            private set;
        }

        public PackageId(string id, string version)
        {
            this.Id = id;
            this.Version = version;
        }

        //public static PackageId FromString(string id, string version)
        //{
        //    return new PackageId(id, version);
        //}

        public override bool Equals(object obj)
        {
            PackageId other = obj as PackageId;

            if (other == null)
            {
                return false;
            }

            return (this.Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && this.Version.Equals(other.Version));
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
