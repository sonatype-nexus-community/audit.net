using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using NuGet.VisualStudio;
using NugetAuditor.Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NugetAuditor.VSIX
{
    public class TaskProvider : ITaskProvider, IDisposable
    {
        private IServiceProvider _provider;
        private TaskCollection _tasks;

        private uint _taskListCookie;
        private IVsTaskList _taskList;

        private int _suspended = 0;
        private bool _dirty = false;

        public Guid ProviderGuid
        {
            get;
            set;
        }

        public string ProviderName
        {
            get;
            set;
        }

        public TaskCollection Tasks
        {
            get
            {
                if (this._tasks == null)
                {
                    this._tasks = new TaskCollection(this);
                    _tasks.CollectionChanged += Tasks_CollectionChanged;
                }
                return this._tasks;
            }
        }

        public TaskProvider(IServiceProvider provider)
        {
            this._provider = provider;
            ProviderGuid = typeof(TaskProvider).GUID;
            ProviderName = "Nuget Auditor";

            InitializeTaskProvider();
        }

        private void InitializeTaskProvider()
        {
            _taskList = this.GetService(typeof(SVsErrorList)) as IVsTaskList;

            if (_taskList != null)
            {
                ErrorHandler.ThrowOnFailure(_taskList.RegisterTaskProvider(this, out _taskListCookie));
            }
        }

        private void Tasks_CollectionChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }



        protected internal object GetService(Type serviceType)
        {
            if (this._provider != null)
            {
                return this._provider.GetService(serviceType);
            }
            return null;
        }

        public void ClearTasks()
        {
            foreach (var task in Tasks.OfType<IVsTaskItem>())
            {
                if (task != null)
                {
                    task.OnDeleteTask();
                }
            }
            Tasks.Clear();
        }

        public void SuspendRefresh()
        {
            if (this._suspended < int.MaxValue)
            {
                this._suspended++;
            }
        }

        public void ResumeRefresh()
        {
            if (this._suspended > 0)
            {
                this._suspended--;

                if ((this._suspended == 0) && this._dirty)
                {
                    this.Refresh();
                }
            }
        }

        public void Refresh()
        {
            if (!VsShellUtilities.ShellIsShuttingDown)
            {
                if (this._suspended == 0)
                {
                    this._dirty = false;
                    ErrorHandler.ThrowOnFailure(this._taskList.RefreshTasks(this._taskListCookie));
                }
                else
                {
                    this._dirty = true;
                }
            }
        }

        public int EnumTaskItems(out IVsEnumTaskItems ppenum)
        {
            ppenum = new TaskEnumerator(Tasks);
            return VSConstants.S_OK;
        }

        public int ImageList(out IntPtr phImageList)
        {
            phImageList = IntPtr.Zero;
            return VSConstants.E_NOTIMPL;
        }

        public int SubcategoryList(uint cbstr, string[] rgbstr, out uint pcActual)
        {
            pcActual = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int ReRegistrationKey(out string pbstrKey)
        {
            pbstrKey = string.Empty;
            return VSConstants.E_NOTIMPL;
        }

        public int OnTaskListFinalRelease(IVsTaskList pTaskList)
        {
            if (pTaskList != null)
            {
                if (_taskListCookie != 0)
                {
                    pTaskList.UnregisterTaskProvider(_taskListCookie);
                    _taskListCookie = VSConstants.VSCOOKIE_NIL;
                    _taskList = null;
                }
            }
            return VSConstants.S_OK;
        }

        public int MaintainInitialTaskOrder(out int bMaintainOrder)
        {
            bMaintainOrder = 0;
            return VSConstants.S_OK;
        }

        public int GetProviderFlags(out uint tpfFlags)
        {
            tpfFlags = (uint)(__VSTASKPROVIDERFLAGS.TPF_ALWAYSVISIBLE | __VSTASKPROVIDERFLAGS.TPF_NOAUTOROUTING);
            return VSConstants.S_OK;
        }

        public int GetProviderName(out string pbstrName)
        {
            if (string.IsNullOrEmpty(ProviderName))
            {
                pbstrName = null;
                return VSConstants.E_NOTIMPL;
            }

            pbstrName = ProviderName;
            return VSConstants.S_OK;
        }

        public int GetProviderGuid(out Guid pguidProvider)
        {
            pguidProvider = ProviderGuid;
            return VSConstants.S_OK;
        }

        public int GetSurrogateProviderGuid(out Guid pguidProvider)
        {
            pguidProvider = Guid.Empty;
            return VSConstants.E_NOTIMPL;
        }

        public int GetProviderToolbar(out Guid pguidGroup, out uint pdwID)
        {
            pguidGroup = Guid.Empty;
            pdwID = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int GetColumnCount(out int pnColumns)
        {
            pnColumns = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int GetColumn(int iColumn, VSTASKCOLUMN[] pColumn)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeginTaskEdit(IVsTaskItem pItem)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnEndTaskEdit(IVsTaskItem pItem, int fCommitChanges, out int pfAllowChanges)
        {
            pfAllowChanges = 0;
            return VSConstants.E_NOTIMPL;
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
                    ClearTasks();
                    Tasks.CollectionChanged -= Tasks_CollectionChanged;
                    ((IVsTaskProvider)this).OnTaskListFinalRelease(_taskList);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _taskList = null;
                _provider = null;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TaskProvider() {
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


    }

    internal class TaskEnumerator : IVsEnumTaskItems
    {
        private TaskCollection tasks;
        private int pos;

        public TaskEnumerator(TaskCollection tasks)
        {
            this.tasks = tasks;
            // pos = 0;
        }

        public virtual int Clone(out IVsEnumTaskItems ppenum)
        {
            ppenum = new TaskEnumerator(tasks);
            return VSConstants.S_OK;
        }

        public virtual int Next(uint celt, IVsTaskItem[] rgelt, uint[] pceltFetched)
        {
            uint fetched = 0;
            while (pos < tasks.Count && fetched < celt)
            {
                Task task = tasks[pos];
                //if (task != null && task.IsVisible())
                if (task != null)
                {
                    if (rgelt != null && rgelt.Length > fetched)
                    {
                        rgelt[fetched] = task;
                    }
                    fetched++;
                }
                pos++;
            }
            if (pceltFetched != null && pceltFetched.Length > 0)
            {
                pceltFetched[0] = fetched;
            }
            return (fetched < celt ? VSConstants.S_FALSE : VSConstants.S_OK);
        }

        public virtual int Reset()
        {
            pos = 0;
            return VSConstants.S_OK;
        }

        public virtual int Skip(uint celt)
        {
            uint skipped = 0;
            while (pos < tasks.Count && skipped < celt)
            {
                Task task = tasks[pos];
                if (task != null)
                {
                    skipped++;
                }
                pos++;
            }
            return VSConstants.S_OK;
        }
    }

    public sealed class TaskCollection : IList, ICollection, IEnumerable
    {
        private ArrayList list;
        private TaskProvider owner;

        public event EventHandler<EventArgs> CollectionChanged;

        public TaskCollection(TaskProvider owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            this.owner = owner;

            this.list = new ArrayList();
        }

        private void OnCollectionChanged()
        {
            var handler = CollectionChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public int Add(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            task.Owner = this.owner;
            OnCollectionChanged();
            return this.list.Add(task);
        }

        public void Clear()
        {
            if (this.list.Count > 0)
            {
                foreach (var item in this.list)
                {
                    ((Task)item).Owner = null;
                }

                this.list.Clear();
                OnCollectionChanged();
            }
        }

        public bool Contains(Task task)
        {
            return this.list.Contains(task);
        }

        private void EnsureTask(object obj)
        {
            if (!(obj is Task))
            {
                object[] args = new object[] { typeof(Task).FullName };
                throw new ArgumentException("obj");
            }
        }

        public IEnumerator GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        public int IndexOf(Task task)
        {
            return this.list.IndexOf(task);
        }

        public void Insert(int index, Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            this.list.Insert(index, task);
            task.Owner = this.owner;
            this.OnCollectionChanged();
        }

        public void Remove(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            this.list.Remove(task);
            task.Owner = null;
            this.OnCollectionChanged();
        }

        public void RemoveAt(int index)
        {
            this[index].Owner = null;
            this.list.RemoveAt(index);
            this.OnCollectionChanged();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.list.CopyTo(array, index);
        }

        int IList.Add(object obj)
        {
            this.EnsureTask(obj);
            return this.Add((Task)obj);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object obj)
        {
            this.EnsureTask(obj);
            return this.Contains((Task)obj);
        }

        int IList.IndexOf(object obj)
        {
            this.EnsureTask(obj);
            return this.IndexOf((Task)obj);
        }

        void IList.Insert(int index, object obj)
        {
            this.EnsureTask(obj);
            this.Insert(index, (Task)obj);
        }

        void IList.Remove(object obj)
        {
            this.EnsureTask(obj);
            this.Remove((Task)obj);
        }

        void IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        public Task this[int index]
        {
            get
            {
                return (Task)this.list[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                Task task = this[index];
                if (task != null)
                {
                    task.Owner = null;
                }
                this.list[index] = value;
                value.Owner = this.owner;
                this.OnCollectionChanged();
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this.EnsureTask(value);
                this[index] = (Task)value;
            }
        }
    }
}
