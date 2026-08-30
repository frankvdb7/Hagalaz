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
                WritePlayerEntryBits(output, message.CharacterIndex, message.CharacterLocation, _characterLocationMap);
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

        internal static void WritePlayerEntryBits(
            IRaidoMessageBinaryWriter output,
            int characterIndex,
            Hagalaz.Game.Abstractions.Model.ILocation characterLocation,
            ICharacterLocationService characterLocationMap)
        {
            var bitWriter = output.BeginBitAccess();
            bitWriter.WriteBits(30, (characterLocation.Y & 0x3fff) |
                ((characterLocation.X & 0x3fff) << 14) |
                ((characterLocation.Z & 0x3) << 28));
            for (var index = 1; index < 2048; index++)
            {
                if (index == characterIndex)
                {
                    continue;
                }

                var location = characterLocationMap.FindLocationByIndex(index);
                bitWriter.WriteBits(18, ((location.Z & 0x3) << 16) |
                    ((location.X & 0xff) << 8) |
                    (location.Y & 0xff));
            }

            bitWriter.EndBitAccess();
        }
    }
}
