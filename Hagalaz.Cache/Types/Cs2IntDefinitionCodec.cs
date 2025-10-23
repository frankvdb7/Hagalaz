using System.IO;
using System.Text;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    public class Cs2IntDefinitionCodec : ICs2IntDefinitionCodec
    {
        public ICs2IntDefinition Decode(int id, MemoryStream stream)
        {
            var definition = new Cs2IntDefinition(id);
            for (byte opcode = (byte)stream.ReadUnsignedByte(); opcode != 0; opcode = (byte)stream.ReadUnsignedByte())
            {
                ParseOpcode(opcode, stream, definition);
            }
            return definition;
        }

        private void ParseOpcode(byte opcode, MemoryStream stream, Cs2IntDefinition definition)
        {
            if (opcode == 1)
            {
                definition.AChar327 = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            }
            else if (opcode == 2)
            {
                definition.AnInt325 = 0;
            }
        }

        public MemoryStream Encode(ICs2IntDefinition instance)
        {
            var stream = new MemoryStream();
            // Note: The original implementation did not have encoding logic.
            // This is a basic implementation that would need to be expanded if encoding is required.
            if (instance.AChar327 != default)
            {
                stream.WriteByte(1);
                stream.WriteByte((byte)instance.AChar327);
            }

            if (instance.AnInt325 == 0)
            {
                stream.WriteByte(2);
            }
            stream.WriteByte(0); // Terminator
            stream.Position = 0;
            return stream;
        }
    }
}
