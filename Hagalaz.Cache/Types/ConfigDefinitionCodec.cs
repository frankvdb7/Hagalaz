using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using System.IO;
using System.Text;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// A codec for encoding and decoding <see cref="IConfigDefinition"/> objects.
    /// </summary>
    public class ConfigDefinitionCodec : ITypeCodec<IConfigDefinition>
    {
        /// <inheritdoc />
        public IConfigDefinition Decode(int id, MemoryStream stream)
        {
            var definition = new ConfigDefinition(id);
            while (stream.Position < stream.Length)
            {
                var opcode = (byte)stream.ReadUnsignedByte();
                if (opcode == 0)
                    break;

                ParseOpcode(opcode, stream, definition);
            }
            return definition;
        }

        /// <inheritdoc />
        public MemoryStream Encode(IConfigDefinition definition)
        {
            var stream = new MemoryStream();
            if (definition.ValueType != default)
            {
                stream.WriteByte(1);
                stream.WriteByte((byte)definition.ValueType);
            }

            if (definition.DefaultValue != default)
            {
                stream.WriteByte(5);
                stream.WriteShort((ushort)definition.DefaultValue);
            }

            stream.WriteByte(0); // End of definition
            return stream;
        }

        /// <summary>
        /// Parses an opcode from the stream and applies it to the definition.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="definition">The definition to modify.</param>
        private static void ParseOpcode(byte opcode, MemoryStream stream, IConfigDefinition definition)
        {
            if (opcode == 1)
            {
                definition.ValueType = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            }
            else if (opcode == 5)
            {
                definition.DefaultValue = stream.ReadUnsignedShort();
            }
        }
    }
}
