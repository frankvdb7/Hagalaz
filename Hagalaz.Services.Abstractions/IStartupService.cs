using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Services.Abstractions
{
    /// <summary>
    /// Represents a type that performs async loading.
    /// </summary>
    public interface IStartupService
    {
        /// <summary>
        /// Starts asynchronous.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
