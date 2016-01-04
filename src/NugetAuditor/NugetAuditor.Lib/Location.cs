using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
    public class Location
    {
        public string File { get; set; }
        public int StartLine { get; set; }
        public int StartPos { get; set; }
        public int EndLine { get; set; }
        public int EndPos { get; set; }

        
    }
}
