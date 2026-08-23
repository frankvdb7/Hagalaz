using Hagalaz.Cache.Abstractions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Decoders
{
    public sealed class WorldReconnectHandshakeRequestDecoder : WorldHandshakeRequestDecoder
    {
        public WorldReconnectHandshakeRequestDecoder(IOptions<RsaClientConfig> rsaOptions, ICacheAPI cacheApi)
            : base(rsaOptions, cacheApi)
        {
        }

        protected override ClientSignInRequest CreateRequest(
            int clientRevision,
            int clientRevisionPatch,
            string login,
            string password,
            uint[] isaacSeed,
            int[] cacheCrcs,
            string clientId,
            DisplayMode displayMode,
            short clientSizeX,
            short clientSizeY) => new WorldReconnectRequest
            {
                ClientRevision = clientRevision,
                ClientRevisionPatch = clientRevisionPatch,
                Login = login,
                Password = password,
                IsaacSeed = isaacSeed,
                CacheCRCs = cacheCrcs,
                ClientId = clientId,
                DisplayMode = displayMode,
                ClientSizeX = clientSizeX,
                ClientSizeY = clientSizeY
            };
    }
}
