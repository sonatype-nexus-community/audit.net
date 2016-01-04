using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    public interface ITask : IVsTaskItem, IVsTaskItem2, IVsTaskItem3, IVsErrorItem, IVsProvideUserContext
    {
    }
}
