using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    public class Cs2DefinitionCodec : ICs2DefinitionCodec
    {
        public ICs2Definition Decode(int id, MemoryStream stream)
        {
            var definition = new Cs2Definition(id);
            Decode(definition, stream);
            return definition;
        }

        private void Decode(Cs2Definition definition, MemoryStream buffer)
        {
            buffer.Position = buffer.Length - 2;
            int switchBlocksSize = buffer.ReadUnsignedShort();
            long codeBlockEnd = buffer.Length - switchBlocksSize - 16 - 2;
            buffer.Position = codeBlockEnd;
            int codeSize = buffer.ReadInt();
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
                    switches[i].Add(buffer.ReadInt(), buffer.ReadInt());
                }
            }
            definition.Switches = switches;

            buffer.Position = 0;
            definition.Name = buffer.ReadCheckedString();

            var intPool = new int[codeSize];
            var stringPool = Enumerable.Repeat(string.Empty, codeSize).ToArray();
            var longPool = new long[codeSize];
            var opcodes = new int[codeSize];

            int writeOffset = 0;
            while (buffer.Position < codeBlockEnd)
            {
                int opcode = buffer.ReadUnsignedShort();
                if (opcode == 3)
                {
                    intPool[writeOffset] = buffer.ReadInt();
                }
                else if (opcode == 145)
                {
                    stringPool[writeOffset] = buffer.ReadString();
                }
                else if (opcode == 63)
                {
                    longPool[writeOffset] = buffer.ReadLong();
                }

                opcodes[writeOffset++] = opcode;
            }

            definition.Opcodes = opcodes;
            definition.IntPool = intPool;
            definition.StringPool = stringPool;
            definition.LongPool = longPool;
        }

        public MemoryStream Encode(ICs2Definition instance)
        {
            var stream = new MemoryStream();
            stream.WriteCheckedString(instance.Name);

            for(int i = 0; i < instance.Opcodes.Count; i++)
            {
                var opcode = instance.Opcodes[i];
                stream.WriteShort(opcode);

                if (opcode == 3)
                {
                    stream.WriteInt(instance.IntPool[i]);
                }
                else if (opcode == 145)
                {
                    stream.WriteString(instance.StringPool[i]);
                }
                else if (opcode == 63)
                {
                    stream.WriteLong(instance.LongPool[i]);
                }
            }

            var switchBlockStream = new MemoryStream();
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

            stream.WriteInt(instance.Opcodes.Count);
            stream.WriteShort(instance.IntLocalsCount);
            stream.WriteShort(instance.StringLocalsCount);
            stream.WriteShort(instance.LongLocalsCount);
            stream.WriteShort(instance.IntArgsCount);
            stream.WriteShort(instance.StringArgsCount);
            stream.WriteShort(instance.LongArgsCount);

            stream.Write(switchBlockStream.ToArray());
            stream.WriteShort((int)switchBlockStream.Length);

            return stream;
        }
    }
}
