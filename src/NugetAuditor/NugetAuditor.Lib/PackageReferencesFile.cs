using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NugetAuditor.Lib
{
    public class PackageReferencesFile
    {
        public string Path { get; private set; }

        public bool Exists
        {
            get
            {
                return System.IO.File.Exists(this.Path);
            }
        }

        public PackageReferencesFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            this.Path = path;
        }

        private IEnumerable<XElement> GetElements()
        {
            var loadOptions = LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo;

            return XDocument.Load(this.Path, loadOptions).Root.Elements("package");
        }

        public IEnumerable<PackageReference> GetPackageReferences()
        {
            return GetElements().Select(x =>
            {
                var start = x as IXmlLineInfo;
                var end = x.NextNode as IXmlLineInfo;

                return new PackageReference
                (
                    x.GetAttributeValue("id", string.Empty),
                    x.GetAttributeValue("version", string.Empty)
                )
                {
                    File = this.Path,
                    StartLine = start.LineNumber,
                    StartPos = start.LinePosition - 1,
                    EndLine = end.LineNumber,
                    EndPos = end.LinePosition - 2,
                };
            });
        }
    }
}
