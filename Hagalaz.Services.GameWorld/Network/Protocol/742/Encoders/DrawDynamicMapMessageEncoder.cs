using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawDynamicMapMessageEncoder : IRaidoMessageEncoder<DrawDynamicMapMessage>
    {
        /// <summary>
        /// The client's second dynamic map-region type (wire value 2). Unlike types 1 and 3 it does not clear the
        /// client NPC list on every draw, matching the standard map; unlike type 4 it discards out-of-view objects.
        /// </summary>
        internal const byte DynamicMapRegionType = 2;

        private readonly ICharacterLocationService _characterLocationMap;

        public DrawDynamicMapMessageEncoder(ICharacterLocationService characterLocationMap)
        {
            _characterLocationMap = characterLocationMap;
        }

        public void EncodeMessage(DrawDynamicMapMessage message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(153)
                .SetSize(RaidoMessageSize.VariableShort);

            if (message.RenderViewport)
            {
                DrawStandardMapMessageEncoder.WritePlayerEntryBits(output, message.CharacterIndex, message.CharacterLocation, _characterLocationMap);
            }

            output
                .WriteByteC(DynamicMapRegionType)
                .WriteInt16LittleEndianA((short)message.RegionPartY)
                .WriteByteS((byte)(message.ForceUpdate ? 1 : 0))
                .WriteByte((byte)message.MapSizeIndex)
                .WriteInt16BigEndianA((short)message.RegionPartX);

            var bitWriter = output.BeginBitAccess();
            foreach (var chunk in message.Chunks)
            {
                bitWriter.WriteBits(1, chunk.IsPresent ? 1 : 0);
                if (!chunk.IsPresent)
                {
                    continue;
                }

                bitWriter.WriteBits(26, ((chunk.Rotation & 0x3) << 1)
                    | ((chunk.SourcePlane & 0x3) << 24)
                    | ((chunk.SourcePartX & 0x3ff) << 14)
                    | ((chunk.SourcePartY & 0x7ff) << 3));
            }
            bitWriter.EndBitAccess();

            foreach (var regionXtea in message.SourceRegionXteaKeys)
            {
                foreach (var xtea in regionXtea)
                {
                    output.WriteInt32BigEndian(xtea);
                }
            }
        }
    }
}
