using System;
using System.Threading.Tasks;

namespace Raido.Server.Extensions
{
    public static class DisposableExtensions
    {
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