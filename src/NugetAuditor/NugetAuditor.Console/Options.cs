using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.ConsoleApp
{
    class Options
    {
#if DEBUG
        const bool verbose = true;
#else
        const bool verbose = false;
#endif
        [Option('p', "package", DefaultValue = "packages.config", HelpText = "Specific packages.config file to audit.")]
        public string Package { get; set; }

        [Option('c', "cache", DefaultValue = 0, HelpText = "Number of minutes item is cached. [0: Default, -1: Disabled]")]
        public int CacheSync { get; set; }

        [Option('v', "verbose", DefaultValue = verbose, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this);
        }
    }
}
