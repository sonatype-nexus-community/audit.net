using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace NugetAuditor.VSIX
{
    public class AuditTask : VSIX.Task, IVsTextMarkerClient, IDisposable
    {
        private Lib.AuditResult _auditResult;
        private Lib.PackageReference _packageReference;
        private IVsTextLineMarker _textLineMarker;

        public Lib.PackageId PackageId
        {
            get
            {
                return _auditResult.PackageId;
            }
        }

        public AuditTask(Lib.AuditResult auditResult, Lib.PackageReference packageReference)
        {
            this._auditResult = auditResult;
            this._packageReference = packageReference;

            this.Category = TaskCategory.CodeSense;
            this.Document = _packageReference.File;
            this.Line = _packageReference.StartLine - 1;
            this.Column = _packageReference.StartPos;
            this.HelpKeyword = _packageReference.Id;
            this.Priority = TaskPriority.Normal;
        }

        public void CreateTextLineMarker(IVsTextLines buffer)
        {
            InvalidateTextLineMarker();

            var span = new TextSpan()
            {
                iStartLine = _packageReference.StartLine - 1,
                iStartIndex = _packageReference.StartPos,
                iEndLine = _packageReference.EndLine - 1,
                iEndIndex = _packageReference.EndPos
            };

            int markerType;

            switch (_auditResult.Status)
            {
                case Lib.AuditStatus.Vulnerable:
                    markerType = (int)MARKERTYPE.MARKER_CODESENSE_ERROR;
                    break;
                case Lib.AuditStatus.UnknownPackage:
                    markerType = (int)MARKERTYPE.MARKER_OTHER_ERROR;
                    break;
                case Lib.AuditStatus.UnknownSource:
                    markerType = (int)MARKERTYPE.MARKER_OTHER_ERROR;
                    break;
                case Lib.AuditStatus.KnownVulnerabilities:
                    markerType = (int)MARKERTYPE2.MARKER_WARNING;
                    break;
                default:
                    markerType = (int)MARKERTYPE2.MARKER_WARNING;
                    break;
            }

            // create marker
            IVsTextLineMarker[] marker = new IVsTextLineMarker[1];
            ErrorHandler.ThrowOnFailure(buffer.CreateLineMarker(markerType, span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex, this, marker));
            _textLineMarker = marker[0];
        }

        public void InvalidateTextLineMarker()
        {
            if (_textLineMarker != null)
            {
                ErrorHandler.ThrowOnFailure(_textLineMarker.Invalidate());
            }
        }

        #region IVsTextMarkerClient

        public int ExecMarkerCommand(IVsTextMarker pMarker, int iItem)
        {
            return VSConstants.S_OK;
        }

        public int GetMarkerCommandInfo(IVsTextMarker pMarker, int iItem, string[] pbstrText, uint[] pcmdf)
        {
            return VSConstants.S_OK;
        }

        public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
        {
            pbstrText[0] = Text;

            return VSConstants.S_OK;
        }

        public void MarkerInvalidated()
        {
            ErrorHandler.Ignore(_textLineMarker.UnadviseClient());

            _textLineMarker = null;
        }

        public int OnAfterMarkerChange(IVsTextMarker pMarker)
        {
            return VSConstants.S_OK;
        }

        public void OnAfterSpanReload() { }

        public void OnBeforeBufferClose() { }

        public void OnBufferSave(string pszFileName) { }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    InvalidateTextLineMarker();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                HierarchyItem = null;
                _auditResult = null;
                _packageReference = null;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AuditTask() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion



        #endregion
    }
}
