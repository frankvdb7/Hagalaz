using System;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public interface IRaidoCodecFactory<TProtocol> where TProtocol : IRaidoProtocol
    {
        public IRaidoMessageDecoder? GetMessageDecoder(int opcode);
        public IRaidoMessageEncoder? GetMessageEncoder(Type messageType);
    }
}