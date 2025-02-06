using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawStandardMapMessageEncoder : IRaidoMessageEncoder<DrawStandardMapMessage>
    {
        private readonly ICharacterLocationService _characterLocationMap;

        public DrawStandardMapMessageEncoder(ICharacterLocationService characterLocationMap)
        {
            _characterLocationMap = characterLocationMap;
        }

        public void EncodeMessage(DrawStandardMapMessage message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(89)
                .SetSize(RaidoMessageSize.VariableShort);

            // enter world packet bits
            if (message.RenderViewport)
            {
                var clocation = message.CharacterLocation;
                var bitWriter = output.BeginBitAccess();
                bitWriter.WriteBits(30, (clocation.Y & 0x3fff) | ((clocation.X & 0x3fff) << 14) | ((clocation.Z & 0x3) << 28));
                for (var index = 1; index < 2048; index++)
                {
                    if (index == message.CharacterIndex)
                    {
                        continue;
                    }

                    var location = _characterLocationMap.FindLocationByIndex(index);
                    bitWriter.WriteBits(18, ((location.Z & 0x3) << 16) | ((location.X & 0xff) << 8) | (location.Y & 0xff));
                }
                bitWriter.EndBitAccess();
            }

            output
                .WriteByte((byte)message.MapSizeIndex)
                .WriteInt16BigEndian((short)message.RegionPartY)
                .WriteByteS((byte)(message.ForceUpdate ? 1 : 0))
                .WriteInt16LittleEndianA((short)message.RegionPartX);

            foreach (var regionXtea in message.VisibleRegionXteaKeys)
            {
                foreach (var xtea in regionXtea)
                {
                    output.WriteInt32BigEndian(xtea);
                }
            }
        }
    }
}
