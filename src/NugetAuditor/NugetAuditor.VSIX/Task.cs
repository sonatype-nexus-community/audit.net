using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VSHelp;
using Microsoft.VisualStudio.VSHelp80;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace NugetAuditor.VSIX
{
    public class Task : ITask
    {
        public const string contextNameKeyword = "Keyword";

        private TaskProvider owner;
        private IVsUserContext context;
        private IDictionary<int, string> customColumns = new Dictionary<int, string>();

        public event EventHandler<EventArgs> Help;
        public event EventHandler<CancelEventArgs> Navigate;
        public event EventHandler<EventArgs> Deleted;
        public event EventHandler<EventArgs> Removed;

        internal TaskProvider Owner
        {
            get { return this.owner; }
            set
            {
                if ((this.owner != null) && (value == null))
                {
                    this.OnRemoved(EventArgs.Empty);
                }
                this.owner = value;
            }
        }

        public TaskCategory Category
        {
            get;
            set;
        }

        public string Document
        {
            get;
            set;
        }

        public int Line
        {
            get;
            set;
        }

        public int Column
        {
            get;
            set;
        }

        public string HelpKeyword
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public TaskErrorCategory ErrorCategory
        {
            get;
            set;
        }

        public IVsHierarchy HierarchyItem
        {
            get;
            set;
        }

        public TaskPriority Priority
        {
            get;
            set;
        }

        public bool Checked
        {
            get;
            set;
        }

        public Task()
        {
            
        }

        private void UpdateOwner()
        {
            if (this.owner != null)
            {
                this.owner.Refresh();
            }
        }

        private void OnHelp(EventArgs e)
        {
            if ((this.HelpKeyword.Length > 0) && (this.owner != null))
            {
                var service = this.owner.GetService(typeof(SVsHelp)) as Help2;

                if (service != null)
                {
                    service.DisplayTopicFromF1Keyword(this.HelpKeyword);
                }
            }

            var handler = this.Help;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnNavigate(CancelEventArgs e)
        {
            var handler = this.Navigate;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnDeleted(EventArgs e)
        {
            var handler = this.Deleted;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnRemoved(EventArgs e)
        {
            var handler = this.Removed;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public int GetTaskProvider(out IVsTaskProvider3 ppProvider)
        {
            ppProvider = this.owner as IVsTaskProvider3;
            return VSConstants.S_OK;
        }

        public int GetTaskName(out string pbstrName)
        {
            pbstrName = Text;
            return VSConstants.S_OK;
        }

        public int GetColumnValue(int iField, out uint ptvtType, out uint ptvfFlags, out object pvarValue, out string pbstrAccessibilityName)
        {
            ptvtType = 0;
            ptvfFlags = 0;
            pvarValue = null;
            pbstrAccessibilityName = null;

            return VSConstants.E_NOTIMPL;
        }

        public int GetTipText(int iField, out string pbstrTipText)
        {
            pbstrTipText = null;
            return VSConstants.E_NOTIMPL;
        }

        public int SetColumnValue(int iField, ref object pvarValue)
        {
            switch ((VSTASKFIELD)iField)
            {
                case VSTASKFIELD.FLD_CHECKED: Checked = (bool)pvarValue; break;
                case VSTASKFIELD.FLD_PRIORITY: Priority = (TaskPriority)pvarValue; break;
                default: return VSConstants.E_NOTIMPL;
            }
            return VSConstants.S_OK;
        }

        public int IsDirty(out int pfDirty)
        {
            pfDirty = 0;
            return VSConstants.S_OK;
        }

        public int GetEnumCount(int iField, out int pnValues)
        {
            pnValues = 0;
            return VSConstants.S_OK;
        }

        public int GetEnumValue(int iField, int iValue, out object pvarValue, out string pbstrAccessibilityName)
        {
            pvarValue = null;
            pbstrAccessibilityName = null;
            return VSConstants.S_OK;
        }

        public int OnLinkClicked(int iField, int iLinkIndex)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int GetNavigationStatusText(out string pbstrText)
        {
            pbstrText = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetDefaultEditField(out int piField)
        {
            piField = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int GetSurrogateProviderGuid(out Guid pguidProvider)
        {
            pguidProvider = Guid.Empty;
            return VSConstants.E_NOTIMPL;
        }

        public int GetHierarchy(out IVsHierarchy ppProject)
        {
            ppProject =  HierarchyItem;
            return VSConstants.S_OK;
        }

        public int GetCategory(out uint pCategory)
        {
            pCategory = (uint)(__VSERRORCATEGORY)ErrorCategory;
            return VSConstants.S_OK;
        }


        #region IVsTaskItem2

        int IVsTaskItem2.CanDelete(out int pfCanDelete)
        {
            return ((IVsTaskItem)this).CanDelete(out pfCanDelete);
        }

        int IVsTaskItem2.Category(VSTASKCATEGORY[] pCat)
        {
            return ((IVsTaskItem)this).Category(pCat);
        }

        int IVsTaskItem2.Column(out int piCol)
        {
            return ((IVsTaskItem)this).Column(out piCol);
        }

        int IVsTaskItem2.Document(out string pbstrMkDocument)
        {
            return ((IVsTaskItem)this).Document(out pbstrMkDocument);
        }

        int IVsTaskItem2.get_Checked(out int pfChecked)
        {
            return ((IVsTaskItem)this).get_Checked(out pfChecked);
        }

        int IVsTaskItem2.get_Priority(VSTASKPRIORITY[] ptpPriority)
        {
            return ((IVsTaskItem)this).get_Priority(ptpPriority);
        }

        int IVsTaskItem2.get_Text(out string pbstrName)
        {
            return ((IVsTaskItem)this).get_Text(out pbstrName);
        }

        int IVsTaskItem2.HasHelp(out int pfHasHelp)
        {
            return ((IVsTaskItem)this).HasHelp(out pfHasHelp);
        }

        int IVsTaskItem2.ImageListIndex(out int pIndex)
        {
            return ((IVsTaskItem)this).ImageListIndex(out pIndex);
        }

        int IVsTaskItem2.IsReadOnly(VSTASKFIELD field, out int pfReadOnly)
        {
            return ((IVsTaskItem)this).IsReadOnly(field, out pfReadOnly);
        }

        int IVsTaskItem2.Line(out int piLine)
        {
            return ((IVsTaskItem)this).Line(out piLine);
        }

        int IVsTaskItem2.NavigateTo()
        {
            return ((IVsTaskItem)this).NavigateTo();
        }

        int IVsTaskItem2.NavigateToHelp()
        {
            return ((IVsTaskItem)this).NavigateToHelp();
        }

        int IVsTaskItem2.OnDeleteTask()
        {
            return ((IVsTaskItem)this).OnDeleteTask();
        }

        int IVsTaskItem2.OnFilterTask(int fVisible)
        {
            return ((IVsTaskItem)this).OnFilterTask(fVisible);
        }

        int IVsTaskItem2.put_Checked(int fChecked)
        {
            return ((IVsTaskItem)this).put_Checked(fChecked);
        }

        int IVsTaskItem2.put_Priority(VSTASKPRIORITY tpPriority)
        {
            return ((IVsTaskItem)this).put_Priority(tpPriority);
        }

        int IVsTaskItem2.put_Text(string bstrName)
        {
            return ((IVsTaskItem)this).put_Text(bstrName);
        }

        int IVsTaskItem2.SubcategoryIndex(out int pIndex)
        {
            return ((IVsTaskItem)this).SubcategoryIndex(out pIndex);
        }

        int IVsTaskItem2.BrowseObject(out object ppObj)
        {
            ppObj = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem2.IsCustomColumnReadOnly(ref Guid guidView, uint iCustomColumnIndex, out int pfReadOnly)
        {
            pfReadOnly = 1;
            return VSConstants.S_OK;
        }

        int IVsTaskItem2.put_CustomColumnText(ref Guid guidView, uint iCustomColumnIndex, string bstrText)
        {
            customColumns[(int)iCustomColumnIndex] = bstrText;
            return VSConstants.S_OK;
        }

        int IVsTaskItem2.get_CustomColumnText(ref Guid guidView, uint iCustomColumnIndex, out string pbstrText)
        {
            if (customColumns.TryGetValue((int)iCustomColumnIndex, out pbstrText))
            {
                return VSConstants.S_OK;
            }

            return VSConstants.E_INVALIDARG;
        }

        #endregion

        int IVsTaskItem.get_Priority(VSTASKPRIORITY[] ptpPriority)
        {
            if (ptpPriority != null)
            {
                ptpPriority[0] = (VSTASKPRIORITY)Priority;
            }
            return VSConstants.S_OK;
        }

        int IVsTaskItem.put_Priority(VSTASKPRIORITY tpPriority)
        {
            Priority = (TaskPriority)tpPriority;

            return VSConstants.S_OK;
        }

        int IVsTaskItem.Category(VSTASKCATEGORY[] pCat)
        {
            if (pCat != null)
            {
                pCat[0] = (VSTASKCATEGORY)Category;
            }
            return VSConstants.S_OK;
        }

        int IVsTaskItem.SubcategoryIndex(out int pIndex)
        {
            pIndex = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem.ImageListIndex(out int pIndex)
        {
            pIndex = 0;
            return VSConstants.E_NOTIMPL;
        }


        int IVsTaskItem.get_Checked(out int pfChecked)
        {
            pfChecked = (Checked ? 1 : 0);
            return VSConstants.S_OK;
        }

        int IVsTaskItem.put_Checked(int fChecked)
        {
            Checked = (fChecked > 0);
            return VSConstants.S_OK;
        }

        int IVsTaskItem.get_Text(out string pbstrName)
        {
            pbstrName = Text;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.put_Text(string bstrName)
        {
            if (bstrName != null)
            {
                Text = bstrName;
                return VSConstants.S_OK;
            }
            return VSConstants.E_INVALIDARG;
        }

        int IVsTaskItem.Document(out string pbstrMkDocument)
        {
            pbstrMkDocument = Document;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.Line(out int piLine)
        {
            piLine = Line;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.Column(out int piCol)
        {
            piCol = Column;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.CanDelete(out int pfCanDelete)
        {
            pfCanDelete = 0;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.IsReadOnly(VSTASKFIELD field, out int pfReadOnly)
        {
            switch (field)
            {
                case VSTASKFIELD.FLD_CHECKED: pfReadOnly = 0; break;
                case VSTASKFIELD.FLD_PRIORITY: pfReadOnly = 0; break;
                default: pfReadOnly = 1; break;
            }
            return VSConstants.S_OK;
        }

        int IVsTaskItem.HasHelp(out int pfHasHelp)
        {
            pfHasHelp = (string.IsNullOrEmpty(HelpKeyword)) ? 0 : 1;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.NavigateTo()
        {
            var args = new CancelEventArgs(false);

            this.OnNavigate(args);

            if (args.Cancel)
            {
                return VSConstants.S_OK;
            }
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem.NavigateToHelp()
        {
            this.OnHelp(EventArgs.Empty);
            return VSConstants.S_OK;
        }

        int IVsTaskItem.OnFilterTask(int fVisible)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem.OnDeleteTask()
        {
            Release();
            OnDeleted(EventArgs.Empty);
            return VSConstants.S_OK;
        }

        public void Release()
        {
            // Called from "OnDelete"
            // task list makes sure it doesn't show up 
            //  and we remove it later when an enumeration is asked.
            Owner = null;
        }

        public int GetUserContext(out IVsUserContext ppctx)
        {
            var hr = VSConstants.S_OK;

            if (context == null)
            {
                // Create an empty context
                IVsMonitorUserContext monitorContext = owner.GetService(typeof(SVsMonitorUserContext)) as IVsMonitorUserContext;
                ErrorHandler.ThrowOnFailure(monitorContext.CreateEmptyContext(out context));

                // Add the required information to the context
                hr = context.AddAttribute(VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_LookupF1, "Keyword", this.HelpKeyword);
            }
            ppctx = context;

            return hr;
        }
        //#endregion       
    }
}
