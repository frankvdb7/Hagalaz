using Raido.Common.Protocol;

namespace Hagalaz.Services.GameUpdate.Network.Messages
{
    public class EncryptionFlagMessage : RaidoMessage
    {
        public byte EncryptionFlag { get; init; }
    }
}