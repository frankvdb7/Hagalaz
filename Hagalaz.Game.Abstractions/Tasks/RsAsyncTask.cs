using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Represents a stateful game task that observes an asynchronous operation without blocking the game loop.
    /// </summary>
    public class RsAsyncTask : ITaskItem, IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private Func<CancellationToken, Task>? _prepare;
        private Task? _pending;
        private int _isCancelled;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask"/> class.
        /// </summary>
        /// <param name="taskExec">The asynchronous operation to start from the game loop.</param>
        public RsAsyncTask(Func<Task> taskExec)
            : this(CreatePreparation(taskExec))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask"/> class.
        /// </summary>
        /// <param name="taskExec">The asynchronous operation to start from the game loop.</param>
        public RsAsyncTask(Func<CancellationToken, Task> taskExec) => _prepare = taskExec ?? throw new ArgumentNullException(nameof(taskExec));

        private static Func<CancellationToken, Task> CreatePreparation(Func<Task> taskExec)
        {
            ArgumentNullException.ThrowIfNull(taskExec);
            return _ => taskExec();
        }

        /// <inheritdoc />
        public bool IsCancelled => Volatile.Read(ref _isCancelled) != 0;

        /// <inheritdoc />
        public bool IsCompleted { get; private set; }

        /// <inheritdoc />
        public bool IsFaulted { get; private set; }

        /// <inheritdoc />
        public void Tick()
        {
            if (IsCancelled || IsCompleted || IsFaulted)
            {
                return;
            }

            try
            {
                _pending ??= _prepare!(_cancellation.Token);
                if (!_pending.IsCompleted)
                {
                    return;
                }

                if (_pending.IsCanceled)
                {
                    Volatile.Write(ref _isCancelled, 1);
                    return;
                }

                Complete(_pending);
                IsCompleted = true;
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
        }

        /// <summary>
        /// Completes the operation after its task has been observed as complete.
        /// </summary>
        /// <param name="pending">The completed asynchronous operation.</param>
        protected virtual void Complete(Task pending) => pending.GetAwaiter().GetResult();

        /// <inheritdoc />
        public void Cancel()
        {
            Volatile.Write(ref _isCancelled, 1);
            _cancellation.Cancel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources owned by the task.
        /// </summary>
        protected virtual void ReleaseResources()
        {
            _prepare = null;
            _cancellation.Dispose();
        }
    }

    /// <summary>
    /// Represents a stateful game task that observes an asynchronous operation and applies its result on a game tick.
    /// </summary>
    /// <typeparam name="TResult">The type of the asynchronous result.</typeparam>
    public sealed class RsAsyncTask<TResult> : RsAsyncTask, ITaskItem<TResult>
    {
        private Action<TResult> _resultHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask{TResult}"/> class.
        /// </summary>
        /// <param name="prepare">The asynchronous operation to start from the game loop.</param>
        /// <param name="resultHandler">The callback to execute synchronously when the result is ready.</param>
        public RsAsyncTask(Func<Task<TResult>> prepare, Action<TResult>? resultHandler = null)
            : this(CreatePreparation(prepare), resultHandler)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask{TResult}"/> class.
        /// </summary>
        /// <param name="prepare">The asynchronous operation to start from the game loop.</param>
        /// <param name="resultHandler">The callback to execute synchronously when the result is ready.</param>
        public RsAsyncTask(Func<CancellationToken, Task<TResult>> prepare, Action<TResult>? resultHandler = null)
            : base(CreatePreparation(prepare))
        {
            _resultHandler = resultHandler ?? NoopResultHandler;
        }

        /// <inheritdoc />
        public void RegisterResultHandler(Action<TResult> resultHandler)
        {
            ArgumentNullException.ThrowIfNull(resultHandler);
            var currentHandler = _resultHandler;
            _resultHandler = result =>
            {
                currentHandler.Invoke(result);
                resultHandler.Invoke(result);
            };
        }

        /// <inheritdoc />
        protected override void Complete(Task pending)
        {
            var result = ((Task<TResult>)pending).GetAwaiter().GetResult();
            _resultHandler.Invoke(result);
        }

        /// <inheritdoc />
        protected override void ReleaseResources()
        {
            base.ReleaseResources();
            _resultHandler = null!;
        }

        private static void NoopResultHandler(TResult _) { }

        private static Func<CancellationToken, Task> CreatePreparation(Func<CancellationToken, Task<TResult>> prepare)
        {
            ArgumentNullException.ThrowIfNull(prepare);
            return token => prepare(token);
        }

        private static Func<CancellationToken, Task<TResult>> CreatePreparation(Func<Task<TResult>> prepare)
        {
            ArgumentNullException.ThrowIfNull(prepare);
            return _ => prepare();
        }
    }
}
