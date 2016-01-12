using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    public static class ExceptionHelper
    {
        // Fields
        private const string LogEntrySource = "Audit.Net";

        // Methods
        public static void WriteToActivityLog(Exception exception)
        {
            if (exception != null)
            {
                exception = Unwrap(exception);
                ActivityLog.LogError(LogEntrySource, exception.Message + exception.StackTrace);
            }
        }

        public static Exception Unwrap(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if ((exception.InnerException != null) && ((exception is AggregateException) || (exception is TargetInvocationException)))
            {
                return exception.GetBaseException();
            }
            return exception;
        }
    }
}
