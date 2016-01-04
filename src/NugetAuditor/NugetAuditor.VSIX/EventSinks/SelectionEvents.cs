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

        public void Dispose()
        {
            if (_vsMonitorSelection != null && _selectionEventsCookie != 0)
            {
                ErrorHandler.Ignore(_vsMonitorSelection.UnadviseSelectionEvents(_selectionEventsCookie));
            }

            _vsMonitorSelection = null;
        }
    }
}
