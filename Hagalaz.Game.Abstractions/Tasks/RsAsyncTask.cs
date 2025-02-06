using System;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Tasks
{
    public class RsAsyncTask : ITaskItem, IDisposable
    {
        private Func<Task>? _taskExec;

        public RsAsyncTask(Func<Task> taskExec) => _taskExec = taskExec;

        public bool IsCancelled { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsFaulted { get; private set; }

        public void Tick()
        {
            if (IsCancelled || IsCompleted || IsFaulted)
            {
                return;
            }

            try
            {
                var task = _taskExec?.Invoke();
                task?.ConfigureAwait(false).GetAwaiter().GetResult();
                IsCompleted = task?.IsCompleted ?? true;
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
        }

        private void ReleaseUnmanagedResources() => _taskExec = null;

        public void Cancel() => IsCancelled = true;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RsAsyncTask() => ReleaseUnmanagedResources();
    }
}
