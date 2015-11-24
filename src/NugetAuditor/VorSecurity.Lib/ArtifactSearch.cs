using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class NugetArtifactSearch : ArtifactSearch
    {
        public NugetArtifactSearch() : base("nuget")
        {
        }
    }

    public abstract class ArtifactSearch
    {
        public string pm { get; private set; }
        public string name { get; set; }
        public string version { get; set; }

        public ArtifactSearch(string packageManager)
        {
            this.pm = packageManager;
        }
    }
}
