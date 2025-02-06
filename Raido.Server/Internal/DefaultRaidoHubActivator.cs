using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoHubActivator<THub> : IRaidoHubActivator<THub> where THub : RaidoHub
    {
        private static readonly Lazy<ObjectFactory> ObjectFactory =
            new(() => ActivatorUtilities.CreateFactory(typeof(THub), Type.EmptyTypes));

        private bool? _created;
        private readonly IServiceProvider _serviceProvider;

        public DefaultRaidoHubActivator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public THub Create()
        {
            Debug.Assert(!_created.HasValue, "activators must not be reused.");

            _created = false;
            var hub = _serviceProvider.GetService<THub>();
            if (hub != null)
            {
                return hub;
            }

            hub = (THub)ObjectFactory.Value(_serviceProvider, []);
            _created = true;
            return hub;
        }

        public void Release(THub hub)
        {
            Debug.Assert(_created.HasValue, "hubs must be released with the activator they were created");

            if (_created.Value)
            {
                hub.Dispose();
            }
        }
    }
}