using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Identity;
using OpenIddict.Server;

namespace Hagalaz.Services.Authorization.Services
{
    public class OpenIddictService : IOpenIddictService
    {
        private readonly IOpenIddictServerFactory _serverFactory;
        private readonly IOpenIddictServerDispatcher _dispatcher;

        public OpenIddictService(IOpenIddictServerFactory serverFactory, IOpenIddictServerDispatcher dispatcher)
        {
            _serverFactory = serverFactory;
            _dispatcher = dispatcher;
        }

        public async ValueTask<OpenIddictServerTransaction> CreateTransactionAsync()
        {
            var transaction = await _serverFactory.CreateTransactionAsync();
            transaction.Properties[OpenIddictServerPocoEvents.RequirePocoRequest.PocoRequestKey] = true;
            return transaction;
        }

        public ValueTask DispatchAsync<TContext>(TContext context) where TContext : OpenIddictServerEvents.BaseContext => _dispatcher.DispatchAsync(context);
    }
}
