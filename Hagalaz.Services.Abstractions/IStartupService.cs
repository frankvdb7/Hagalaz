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
        /// <returns></returns>
        Task LoadAsync();
    }
}
