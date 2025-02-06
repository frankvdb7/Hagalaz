using System.Threading;

namespace Hagalaz.Workers
{
    public abstract class ThreadWorker
    {
        private readonly Thread _workerThread;

        protected ThreadWorker(string name, ThreadPriority priority) =>
            _workerThread = new Thread(Run)
            {
                Name = name,
                Priority = priority
            };

        public void Start() => _workerThread.Start();

        public void Stop() => _workerThread.Interrupt();

        protected abstract void Run();
    }
}
