using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol
{
    public interface IClientProtocol : IRaidoProtocol
    {
        public void SetEncryptionSeed(uint[] seed);
    }
}
