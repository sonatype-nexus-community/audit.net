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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using NugetAuditor.Lib;
using NugetAuditor.VSIX.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NugetAuditor.VSIX
{
    internal class PackageReferenceMarkerProvider : IDisposable
    {
        private TaskProvider _taskProvider;

        private List<PackageReferenceMarker> _markers = new List<PackageReferenceMarker>();
        
        public PackageReferenceMarkerProvider(TaskProvider taskProvider)
        {
            this._taskProvider = taskProvider;
        }

        public void CreateMarkers(string documentPath)
        {
            RemoveMarkers(documentPath);

            var textLines = VsUtility.GetDocumentTextLines(documentPath);

            // if document is opened
            if (textLines != null)
            {
                var packageReferences = _taskProvider.Tasks.OfType<VulnerabilityTask>()
                                        .Where(x => x.Document.Equals(documentPath, StringComparison.OrdinalIgnoreCase))
                                        .Select(x => x.PackageReference)
                                        .Distinct();

                foreach (var packageReference in packageReferences)
                {
                    CreateMarker(textLines, packageReference);
                }              
            }
        }

        private void CreateMarker(IVsTextLines buffer, PackageReference packageReference)
        {
            var marker = new PackageReferenceMarker(_taskProvider, packageReference);

            marker.MarkerInvalidated += Marker_MarkerInvalidated;
            marker.BeforeBufferClose += Marker_BeforeBufferClose;

            marker.CreateTextLineMarker(buffer);

            _markers.Add(marker);
        }

        private void Marker_BeforeBufferClose(object sender, EventArgs e)
        {
            var marker = sender as PackageReferenceMarker;

            if (marker != null)
            {
                RemoveMarker(marker);
            }
        }

        private void Marker_MarkerInvalidated(object sender, EventArgs e)
        {
            var marker = sender as PackageReferenceMarker;

            if (marker != null)
            {
                RemoveMarker(marker);
            }
        }

        private void RemoveMarker(PackageReferenceMarker marker)
        {
            if (marker != null)
            {
                this._markers.Remove(marker);
                marker.MarkerInvalidated -= Marker_MarkerInvalidated;
                marker.BeforeBufferClose -= Marker_BeforeBufferClose;
                marker.Dispose();
            }
        }

        public void RemoveMarkers(string documentPath)
        {
            var markersToRemove = this._markers.Where(x => x.PackageReference.File.Equals(documentPath, StringComparison.OrdinalIgnoreCase));

            RemoveMarkers(markersToRemove);
        }

        private void RemoveMarkers(IEnumerable<PackageReferenceMarker> markersToRemove)
        {
            foreach (var marker in markersToRemove.ToArray())
            {
                RemoveMarker(marker);
            }
        }

        public void RefreshMarkers()
        {
            foreach (var marker in this._markers.ToArray())
            {
                marker.RefreshTextLineMarker();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_markers != null)
                    {
                        RemoveMarkers(_markers);
                    }
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _markers = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PackageReferenceMarkerProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
      
    internal class PackageReferenceMarker : IVsTextMarkerClient, IDisposable
    {
        private TaskProvider _taskProvider;
        private IVsTextLineMarker _textLineMarker;

        public event EventHandler<EventArgs> MarkerInvalidated;
        public event EventHandler<EventArgs> BeforeBufferClose;

        public PackageReference PackageReference
        {
            get;
            private set;
        }

        private IEnumerable<VulnerabilityTask> Tasks
        {
            get
            {
                return _taskProvider.Tasks.OfType<VulnerabilityTask>()
                        .Where(x => x.PackageReference.Equals(PackageReference));
            }
        }

        public PackageReferenceMarker(TaskProvider taskProvider, PackageReference packageReference)
        {
            this._taskProvider = taskProvider;
            this.PackageReference = packageReference;
        }

        public void CreateTextLineMarker(IVsTextLines buffer)
        {
            RemoveTextLineMarker();

            var tasks = this.Tasks;

            if (!tasks.Any())
            {
                return;
            }

            int markerType = (int)MARKERTYPE.MARKER_INVISIBLE;

            var errorCategory = tasks.Min(x => x.ErrorCategory);

            switch (errorCategory)
            {
                case TaskErrorCategory.Message:
                    markerType = (int)MARKERTYPE.MARKER_COMPILE_ERROR;
                    break;
                case TaskErrorCategory.Warning:
                    markerType = (int)MARKERTYPE2.MARKER_WARNING;
                    break;
                case TaskErrorCategory.Error:
                    markerType = (int)MARKERTYPE.MARKER_CODESENSE_ERROR;
                    break;
            }

            int iStartLine = this.PackageReference.StartLine - 1;
            int iStartIndex = this.PackageReference.StartPos;
            int iEndLine = this.PackageReference.EndLine - 1;
            int iEndIndex = this.PackageReference.EndPos;

            // create marker
            IVsTextLineMarker[] marker = new IVsTextLineMarker[1];

            ErrorHandler.ThrowOnFailure(buffer.CreateLineMarker(markerType, iStartLine, iStartIndex, iEndLine, iEndIndex, this, marker));
                                   
            _textLineMarker = marker[0];
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_textLineMarker != null)
                {
                    RemoveTextLineMarker();
                }
                this.PackageReference = null;
                this._taskProvider = null;
            }
        }

        public void RefreshTextLineMarker()
        {
            if (_textLineMarker != null && !Tasks.Any())
            {
                ErrorHandler.Ignore(_textLineMarker.Invalidate());
            }
        }

        public void RemoveTextLineMarker()
        {
            if (_textLineMarker != null)
            {
                ErrorHandler.Ignore(_textLineMarker.UnadviseClient());
                ErrorHandler.Ignore(_textLineMarker.Invalidate());
            }
            _textLineMarker = null;
        }

        #region IVsTextMarkerClient

        int IVsTextMarkerClient.ExecMarkerCommand(IVsTextMarker pMarker, int iItem)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTextMarkerClient.GetMarkerCommandInfo(IVsTextMarker pMarker, int iItem, string[] pbstrText, uint[] pcmdf)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTextMarkerClient.GetTipText(IVsTextMarker pMarker, string[] pbstrText)
        {
            var tasks = this.Tasks;
            var known = tasks.Count(x => x.ErrorCategory == TaskErrorCategory.Message);
            var affecting = tasks.Count(x => x.ErrorCategory == TaskErrorCategory.Error);

            pbstrText[0] = string.Format(Resources.Culture, Resources.MarkerTipText_VulnarebilitiesFound, this.PackageReference, known, affecting);

            return VSConstants.S_OK;
        }

        void IVsTextMarkerClient.MarkerInvalidated()
        {
            var handler = MarkerInvalidated;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        int IVsTextMarkerClient.OnAfterMarkerChange(IVsTextMarker pMarker)
        {
            return VSConstants.S_OK;
        }

        void IVsTextMarkerClient.OnAfterSpanReload()
        {
            return;
        }

        void IVsTextMarkerClient.OnBeforeBufferClose()
        {
            var handler = BeforeBufferClose;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            return;
        }

        void IVsTextMarkerClient.OnBufferSave(string pszFileName)
        {
            return;
        }

        #endregion

    }
}
