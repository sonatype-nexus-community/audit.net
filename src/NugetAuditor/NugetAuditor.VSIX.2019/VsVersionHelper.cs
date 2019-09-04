#region License
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
#endregion

using EnvDTE;
using System;

namespace NugetAuditor.VSIX
{
    public static class VsVersionHelper
    {
        // Fields
        private static readonly Lazy<string> _fullVsEdition = new Lazy<string>(new Func<string>(VsVersionHelper.GetFullVsVersionString));
        private static readonly Lazy<int> _vsMajorVersion = new Lazy<int>(new Func<int>(VsVersionHelper.GetMajorVsVersion));
        private const int MaxVsVersion = 13;

        // Methods
        private static string GetFullVsVersionString()
        {
            DTE instance = ServiceLocator.GetInstance<DTE>();
            string edition = instance.Edition;
            if (!edition.StartsWith("VS", StringComparison.OrdinalIgnoreCase))
            {
                edition = "VS " + edition;
            }
            return (edition + "/" + instance.Version);
        }

        private static int GetMajorVsVersion()
        {
            Version version;
            if (Version.TryParse(ServiceLocator.GetInstance<DTE>().Version, out version))
            {
                return version.Major;
            }
            return MaxVsVersion;
        }

        public static string GetSKU()
        {
            return ServiceLocator.GetInstance<DTE>().Edition;
        }

        // Properties
        public static string FullVsEdition
        {
            get
            {
                return _fullVsEdition.Value;
            }
        }

        public static bool IsVisualStudio2010
        {
            get
            {
                return (VsMajorVersion == 10);
            }
        }

        public static bool IsVisualStudio2012
        {
            get
            {
                return (VsMajorVersion == 11);
            }
        }

        public static bool IsVisualStudio2013
        {
            get
            {
                return (VsMajorVersion == 12);
            }
        }

        public static bool IsVisualStudio2014
        {
            get
            {
                return (VsMajorVersion == 14);
            }
        }

        public static bool IsVisualStudio2015
        {
            get
            {
                return (VsMajorVersion == 15);
            }
        }

        public static int VsMajorVersion
        {
            get
            {
                return _vsMajorVersion.Value;
            }
        }
    }
}
