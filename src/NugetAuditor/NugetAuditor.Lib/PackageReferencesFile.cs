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
