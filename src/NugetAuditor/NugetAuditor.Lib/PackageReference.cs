// Copyright (c) 2015-2018, Sonatype Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Sonatype, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL SONATYPE BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Xml;
using System.Xml.Linq;

namespace NugetAuditor.Lib
{
    public class PackageReference
    {
        public PackageId PackageId
        {
            get;
            private set;
        }

        public string File
        {
            get;
            private set;
        }

        public int StartLine
        {
            get;
            set;
        }

        public int StartPos
        {
            get;
            set;
        }

        public int EndLine
        {
            get;
            set;
        }

        public int EndPos
        {
            get;
            set;
        }

        public bool Ignore
        {
            get;
            set;
        }

        public PackageReference(string file, string id, string version)
            :this(file, new PackageId(id, version))
        {
        }

        public PackageReference(string file, PackageId packageId) 
        {
            this.File = file;
            this.PackageId = packageId;
        }

        public override int GetHashCode()
        {
            int h1 = this.File.GetHashCode();
            int h2 = this.PackageId.GetHashCode();

            return (((h1 << 5) + h1) ^ h2);
        }

        public override bool Equals(object obj)
        {
            var other = obj as PackageReference;

            if (other == null)
            {
                return false;
            }

            return Equals(other);// (this.File.Equals(other.File, StringComparison.OrdinalIgnoreCase) && this.PackageId.Equals(other.PackageId));
        }

        public bool Equals(PackageReference other)
        {
            return (this.File.Equals(other.File, StringComparison.OrdinalIgnoreCase) && this.PackageId.Equals(other.PackageId));
        }
    }
}
