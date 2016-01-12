using System;
using System.Xml;
using System.Xml.Linq;

namespace NugetAuditor.Lib
{
    public class PackageReference : PackageId
    {
        public PackageId PackageId
        {
            get
            {
                return this as PackageId;
            }
        }

        public string File { get; private set; }
        public int StartLine { get; set; }
        public int StartPos { get; set; }
        public int EndLine { get; set; }
        public int EndPos { get; set; }
        public bool Ignore { get; set; }

        public PackageReference(string file, string id, string version) 
            : base(id, version)
        {
            this.File = file;
        }

        public override int GetHashCode()
        {
            int h1 = this.File.GetHashCode();
            int h2 = base.GetHashCode();

            return (((h1 << 5) + h1) ^ h2);
        }

        public override bool Equals(object obj)
        {
            var other = obj as PackageReference;

            if (other == null)
            {
                return false;
            }

            return (this.File.Equals(other.File, StringComparison.OrdinalIgnoreCase) && base.Equals(obj));
        }
    }
}
