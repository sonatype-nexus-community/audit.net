using EnvDTE;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
