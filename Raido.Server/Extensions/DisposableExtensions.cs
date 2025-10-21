using System;
using System.Threading.Tasks;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDisposable"/>.
    /// </summary>
    public static class DisposableExtensions
    {
        /// <summary>
        /// Disposes the object asynchronously.
        /// </summary>
        /// <param name="disposable">The object to dispose.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        public static ValueTask DisposeAsync(this IDisposable disposable)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }
            disposable.Dispose();
            return default;
        }
    }
}