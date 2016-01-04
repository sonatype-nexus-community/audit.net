using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    internal static class ErrorHandler
    {
        public static bool Failed(int hr)
        {
            return Microsoft.VisualStudio.ErrorHandler.Failed(hr);
        }

        public static bool Succeeded(int hr)
        {
            return Microsoft.VisualStudio.ErrorHandler.Succeeded(hr);
        }

        public static void ThrowOnFailure(int hr)
        {
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "hr")]
        public static void Ignore(int hr)
        {
            // This method is only used to avoid FxCop warning that an
            // HRESULT is not used.
        }
    }
}
