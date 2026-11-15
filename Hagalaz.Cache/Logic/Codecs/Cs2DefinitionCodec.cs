using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs
{
    public class Cs2DefinitionCodec : ICs2DefinitionCodec
    {
        public ICs2Definition Decode(int id, MemoryStream stream)
        {
            var definition = new Cs2Definition(id);
            try
            {
                Decode(definition, stream);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException($"CS2 script {id} is truncated.", exception);
            }
            return definition;
        }

        private void Decode(Cs2Definition definition, MemoryStream buffer)
        {
            if (buffer.Length < 20)
                throw new InvalidDataException("CS2 script is too short.");

            buffer.Position = buffer.Length - 2;
            int switchBlocksSize = buffer.ReadUnsignedShort();
            long codeBlockEnd = buffer.Length - switchBlocksSize - 16 - 2;
            if (switchBlocksSize < 1 || codeBlockEnd < 1)
                throw new InvalidDataException("Invalid CS2 footer size.");

            buffer.Position = codeBlockEnd;
            int codeSize = buffer.ReadInt();
            if (codeSize < 0 || codeSize > (codeBlockEnd - 1) / 3)
                throw new InvalidDataException("Invalid CS2 instruction count.");
            definition.IntLocalsCount = buffer.ReadUnsignedShort();
            definition.StringLocalsCount = buffer.ReadUnsignedShort();
            definition.LongLocalsCount = buffer.ReadUnsignedShort();
            definition.IntArgsCount = buffer.ReadUnsignedShort();
            definition.StringArgsCount = buffer.ReadUnsignedShort();
            definition.LongArgsCount = buffer.ReadUnsignedShort();

            int switchesCount = buffer.ReadUnsignedByte();
            var switches = new Dictionary<int, int>[switchesCount];
            for (int i = 0; i < switchesCount; i++)
            {
                int numCases = buffer.ReadUnsignedShort();
                switches[i] = new Dictionary<int, int>(numCases);
                while (numCases-- > 0)
                {
                    if (!switches[i].TryAdd(buffer.ReadInt(), buffer.ReadInt()))
                        throw new InvalidDataException("Duplicate CS2 switch key.");
                }
            }
            if (buffer.Position != buffer.Length - 2)
                throw new InvalidDataException("CS2 switch tables do not match the footer size.");

            definition.Switches = switches;

            buffer.Position = 0;
            definition.Name = ReadScriptString(buffer);

            var intPool = new int[codeSize];
            var stringPool = Enumerable.Repeat(string.Empty, codeSize).ToArray();
            var longPool = new long[codeSize];
            var opcodes = new int[codeSize];

            for (int i = 0; i < codeSize; i++)
            {
                if (codeBlockEnd - buffer.Position < 3)
                    throw new InvalidDataException("CS2 instruction count exceeds the instruction stream.");

                int opcode = buffer.ReadUnsignedShort();
                opcodes[i] = opcode;
                switch (Cs2Operands.GetKind(opcode))
                {
                    case Cs2OperandKind.String:
                        stringPool[i] = ReadScriptString(buffer);
                        break;
                    case Cs2OperandKind.Long:
                        longPool[i] = buffer.ReadLong();
                        break;
                    case Cs2OperandKind.Int:
                        intPool[i] = buffer.ReadInt();
                        break;
                    case Cs2OperandKind.Byte:
                        intPool[i] = buffer.ReadUnsignedByte();
                        break;
                }
            }

            if (buffer.Position != codeBlockEnd)
                throw new InvalidDataException("CS2 instructions do not match the footer boundary.");

            definition.Opcodes = opcodes;
            definition.IntPool = intPool;
            definition.StringPool = stringPool;
            definition.LongPool = longPool;
        }

        public MemoryStream Encode(ICs2Definition instance)
        {
            var stream = new MemoryStream();
            WriteScriptString(stream, instance.Name);

            for(int i = 0; i < instance.Opcodes.Count; i++)
            {
                var opcode = instance.Opcodes[i];
                stream.WriteShort(opcode);

                switch (Cs2Operands.GetKind(opcode))
                {
                    case Cs2OperandKind.String:
                        WriteScriptString(stream, instance.StringPool[i]);
                        break;
                    case Cs2OperandKind.Long:
                        stream.WriteLong(instance.LongPool[i]);
                        break;
                    case Cs2OperandKind.Int:
                        stream.WriteInt(instance.IntPool[i]);
                        break;
                    case Cs2OperandKind.Byte:
                        stream.WriteByte(checked((byte)instance.IntPool[i]));
                        break;
                }
            }

            long switchBlockLength;
            using (var switchBlockStream = new MemoryStream())
            {
                switchBlockStream.WriteByte((byte)instance.Switches.Length);
                foreach (var switchh in instance.Switches)
                {
                    switchBlockStream.WriteShort(switchh.Count);
                    foreach (var (key, value) in switchh)
                    {
                        switchBlockStream.WriteInt(key);
                        switchBlockStream.WriteInt(value);
                    }
                }

                switchBlockLength = switchBlockStream.Length;
                switchBlockStream.Position = 0;

                stream.WriteInt(instance.Opcodes.Count);
                stream.WriteShort(instance.IntLocalsCount);
                stream.WriteShort(instance.StringLocalsCount);
                stream.WriteShort(instance.LongLocalsCount);
                stream.WriteShort(instance.IntArgsCount);
                stream.WriteShort(instance.StringArgsCount);
                stream.WriteShort(instance.LongArgsCount);

                switchBlockStream.WriteTo(stream);
            }

            stream.WriteShort((int)switchBlockLength);

            return stream;
        }

        // Class125 uses the Windows-1252 extensions and renders undefined bytes as '?'.
        private const string ExtendedCharacters = "€?‚ƒ„…†‡ˆ‰Š‹Œ?Ž??‘’“”•–—˜™š›œ?žŸ";

        private static string ReadScriptString(MemoryStream stream)
        {
            var value = stream.ReadString();
            return string.Create(value.Length, value, static (characters, source) =>
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    var character = source[i];
                    characters[i] = character is >= '\u0080' and <= '\u009f' ? ExtendedCharacters[character - 128] : character;
                }
            });
        }

        private static void WriteScriptString(MemoryStream stream, string value)
        {
            foreach (var character in value)
            {
                var extended = ExtendedCharacters.IndexOf(character);
                stream.WriteByte(character is > '\0' and < '\u0080' or >= '\u00a0' and <= '\u00ff'
                    ? (byte)character
                    : extended >= 0 ? (byte)(128 + extended) : (byte)'?');
            }
            stream.WriteByte(0);
        }
    }
}
