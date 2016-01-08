using System;
using System.Xml;
using System.Xml.Linq;

namespace NugetAuditor.Lib
{
    public class PackageReference : PackageId
    {
        //public string AllowedVersions { get; private set; }
       // public Location Location { get; private set; }
        
        public PackageReference(string id, string version) 
            : base(id, version)
        { }

        //public PackageReference(string id, string version, Location location) 
        //    : this(id, version, string.Empty, location)
        //{ }

        //public PackageReference(string id, string version, string versionConstraint) 
        //    : this(id, version, versionConstraint, null)
        //{ }

        //public PackageReference(string id, string version, string allowedVersions, Location location) 
        //    : base(id, version)
        //{
        //    this.AllowedVersions = allowedVersions;
        //}

        public string File { get; set; }
        public int StartLine { get; set; }
        public int StartPos { get; set; }
        public int EndLine { get; set; }
        public int EndPos { get; set; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
