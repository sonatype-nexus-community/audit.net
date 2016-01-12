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

        //public void Dispose()
        //{
        //    
        //}
    }
}
