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

        private bool IsOSSIndexIgnored(XNode node)
        {
            if (node == null || node.PreviousNode == null)
            {
                return false;
            }

            switch (node.PreviousNode.NodeType)
            {
                case XmlNodeType.Whitespace:
                case XmlNodeType.Text:
                case XmlNodeType.None:
                    {
                        return IsOSSIndexIgnored(node.PreviousNode);
                    }
                case XmlNodeType.Comment:
                    {
                        var commentValue = (node.PreviousNode as XComment).Value;

                        if (commentValue.Replace(" ", string.Empty).Contains("@OSSIndexIgnore"))
                        {
                            return true;
                        }

                        return IsOSSIndexIgnored(node.PreviousNode);
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        public void ToggleIgnorePackageReference(PackageReference packageReference)
        {
            foreach (var node in GetElements())
            {
                var package = new PackageId(
                    node.GetAttributeValue("id", string.Empty),
                    node.GetAttributeValue("version", string.Empty)
                    );
                
                if (package.Equals(packageReference))
                {
                    if (packageReference.Ignore == false)
                    {
                        node.AddBeforeSelf(new XComment("@OSS Index Ignore"), new XText(Environment.NewLine));
                        packageReference.Ignore = true;
                    }
                    else
                    {
                        node.PreviousNode.Remove();
                        packageReference.Ignore = false;
                    }
                    node.Document.Save(this.Path);
                }
            }
        }

        public IEnumerable<PackageReference> GetPackageReferences()
        {
            return GetElements().Select(x =>
            {
                var start = x as IXmlLineInfo;
                var end = x.NextNode as IXmlLineInfo;

                var id = x.GetAttributeValue("id", string.Empty);
                var version = x.GetAttributeValue("version", string.Empty);

                return new PackageReference(this.Path, id, version)
                {
                    StartLine = start.LineNumber,
                    StartPos = start.LinePosition - 1,
                    EndLine = end.LineNumber,
                    EndPos = end.LinePosition - 2,
                    Ignore = IsOSSIndexIgnored(x),
                };
            });
        }
    }
}
