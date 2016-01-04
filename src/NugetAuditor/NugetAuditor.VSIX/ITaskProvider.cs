using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    public interface ITaskProvider : IVsTaskProvider2, IVsTaskProvider3
    {
    }
}
