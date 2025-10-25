using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using System.IO;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// A codec for <see cref="IVarpBitDefinition"/>.
    /// </summary>
    public class VarpBitDefinitionCodec : ITypeCodec<IVarpBitDefinition>
    {
        /// <inheritdoc />
        public IVarpBitDefinition Decode(int id, MemoryStream stream)
        {
            var definition = new VarpBitDefinition(id);
            for (byte opcode = (byte)stream.ReadUnsignedByte(); opcode != 0; opcode = (byte)stream.ReadUnsignedByte())
            {
                ParseOpcode(opcode, stream, definition);
            }
            return definition;
        }

        /// <inheritdoc />
        public MemoryStream Encode(IVarpBitDefinition type)
        {
            var stream = new MemoryStream();

            if (type.ConfigID != 0 || type.BitOffset != 0 || type.BitLength != 0)
            {
                stream.WriteByte(1);
                stream.WriteShort(type.ConfigID);
                stream.WriteByte(type.BitOffset);
                stream.WriteByte(type.BitLength);
            }

            stream.WriteByte(0); // Terminator
            return stream;
        }

        private void ParseOpcode(byte opcode, MemoryStream stream, IVarpBitDefinition definition)
        {
            if (opcode == 1)
            {
                definition.ConfigID = (short)stream.ReadUnsignedShort();
                definition.BitOffset = (byte)stream.ReadUnsignedByte();
                definition.BitLength = (byte)stream.ReadUnsignedByte();
            }
        }
    }
}
