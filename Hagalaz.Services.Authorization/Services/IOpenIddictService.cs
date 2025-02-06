using System.Threading.Tasks;
using OpenIddict.Server;

namespace Hagalaz.Services.Authorization.Services
{
    public interface IOpenIddictService
    {
        public ValueTask<OpenIddictServerTransaction> CreateTransactionAsync();
        public ValueTask DispatchAsync<TContext>(TContext context) where TContext : OpenIddictServerEvents.BaseContext;
    }
}
