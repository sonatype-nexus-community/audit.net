using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    /// <summary>
	/// This class wraps a <see cref="AuditResult"/> in the .NET standard
	/// <see cref="EventArgs"/> fashion needed by event handlers. This event argument
	/// is passed to <see cref="AuditRunner.Completed"/>
	/// </summary>
	public sealed class AuditCompletedEventArgs : EventArgs
    {
        private IEnumerable<Lib.AuditResult> _results;
        private Exception _exception;

        /// <summary>
        /// Creates a new instance of <see cref="AuditCompletedEventArgs"/> by the given
        /// <see cref="Lib.AuditResult"/> and <see cref="Exception"/>.
        /// </summary>
        /// <param name="result">Contains the result returned by audit.</param>
        /// <param name="exception">If an error occured running audit
        /// <paramref name="exception"/> refers to the <see cref="System.Exception"/>.</param>
        public AuditCompletedEventArgs(IEnumerable<Lib.AuditResult> results, Exception exception)
        {
            _results = results;
            _exception = exception;
        }

        /// <summary>
        /// Gets the <see cref="Lib.AuditResult"/>.
        /// </summary>
        public IEnumerable<Lib.AuditResult> Results
        {
            get { return _results; }
        }

        /// <summary>
        /// Gets the exception (if any) that was raised while running the auditor. If
        /// no error occurred the return value is <see langword="null"/>.
        /// </summary>
        public Exception Exception
        {
            get { return _exception; }
        }
    }
}
