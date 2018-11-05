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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    internal class BackgroundQueue   : IDisposable
    {
       
        private Task _previousTask = Task.FromResult(true);

        private object _lock = new object();

        public Task QueueTask(Action action)
        {
            return QueueTask(action, TaskScheduler.Default);
        }

        public Task QueueTask(Action action, TaskScheduler taskScheduler)
        {
            lock (_lock)
            {
                var task = _previousTask.ContinueWith(t => action()
                    , CancellationToken.None
                    , TaskContinuationOptions.None
                    , taskScheduler);

                _previousTask = task;

                return task;
            }
        }

        public Task<T> QueueTask<T>(Func<T> work)
        {
            return QueueTask(work, TaskScheduler.Default);
        }

        public Task<T> QueueTask<T>(Func<T> work, TaskScheduler taskScheduler)
        {
            lock (_lock)
            {
                var task = _previousTask.ContinueWith(t => work()
                    , CancellationToken.None
                    , TaskContinuationOptions.None
                    , taskScheduler);

                _previousTask = task;

                return task;
            }
        }

        public void Dispose()
        {
            _previousTask.Dispose();
        }
    }
}
