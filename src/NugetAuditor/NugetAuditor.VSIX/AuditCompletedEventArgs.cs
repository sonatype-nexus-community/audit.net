#region License
// Copyright (c) 2015-2016, Vör Security Ltd.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Vör Security, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL VÖR SECURITY BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

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
