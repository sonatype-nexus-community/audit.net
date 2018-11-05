#region License
// Copyright (c) 2015-2018, Sonatype Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Sonatype, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL SONATYPE BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX.EventSinks
{
    public class VsSelectionEvents : IVsSelectionEvents, IDisposable
    {
        private IVsMonitorSelection _vsMonitorSelection;

        private uint _selectionEventsCookie;
        private uint _solutionExistsAndFullyLoadedContextCookie;

        public event EventHandler<EventArgs> SolutionOpened;

        public VsSelectionEvents(IVsMonitorSelection vsMonitorSelection)
        {
            _vsMonitorSelection = vsMonitorSelection;

            // get the solution exists and fully loaded cookie
            Guid rguidCmdUI = VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid;

            _vsMonitorSelection.GetCmdUIContextCookie(ref rguidCmdUI, out this._solutionExistsAndFullyLoadedContextCookie);

            ErrorHandler.ThrowOnFailure(_vsMonitorSelection.AdviseSelectionEvents(this, out _selectionEventsCookie));
        }

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            return VSConstants.S_OK;
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            if ((dwCmdUICookie == this._solutionExistsAndFullyLoadedContextCookie) && (fActive == 1))
            {
                var handler = SolutionOpened;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }

            return VSConstants.S_OK;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    if (_vsMonitorSelection != null && _selectionEventsCookie != VSConstants.VSCOOKIE_NIL)
                    {
                        ErrorHandler.Ignore(_vsMonitorSelection.UnadviseSelectionEvents(_selectionEventsCookie));
                    }
                    _vsMonitorSelection = null;
                    _selectionEventsCookie = VSConstants.VSCOOKIE_NIL;
                    _solutionExistsAndFullyLoadedContextCookie = VSConstants.VSCOOKIE_NIL;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~VsSelectionEvents()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion       
    }
}
