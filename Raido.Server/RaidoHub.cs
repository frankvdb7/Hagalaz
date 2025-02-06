using System;
using System.Threading.Tasks;

namespace Raido.Server
{
    public abstract class RaidoHub : IDisposable
    {
        private bool _disposed;
        private IRaidoCallerClients _clients = default!;
        private RaidoCallerContext _context = default!;
        
        public IRaidoCallerClients Clients
        {
            get
            {
                CheckDisposed();
                return _clients;
            }
            internal set
            {
                CheckDisposed();
                _clients = value;
            }
        }
        
        public RaidoCallerContext Context
        {
            get
            {
                CheckDisposed();
                return _context;
            }
            internal set
            {
                CheckDisposed();
                _context = value;
            }
        }

        
        /// <summary>
        /// Called when a new connection is established.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connect.</returns>
        public virtual Task OnConnectedAsync() => Task.CompletedTask;

        /// <summary>
        /// Called when a connection is terminated.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous disconnect.</returns>
        public virtual Task OnDisconnectedAsync(Exception? exception) => Task.CompletedTask;

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Dispose(true);

            _disposed = true;
        }
        
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}