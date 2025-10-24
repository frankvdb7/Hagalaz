using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hagalaz.Cache.Types
{
    public class ClientMapDefinitionCodec : ITypeCodec<IClientMapDefinition>
    {
        public IClientMapDefinition Decode(int id, MemoryStream stream)
        {
            var definition = new ClientMapDefinition(id);
            for (byte opcode = (byte)stream.ReadUnsignedByte(); opcode != 0; opcode = (byte)stream.ReadUnsignedByte())
            {
                ParseOpcode(opcode, stream, definition);
            }
            return definition;
        }

        public MemoryStream Encode(IClientMapDefinition type)
        {
            var stream = new MemoryStream();

            if (type.KeyType != default(char))
            {
                stream.WriteByte(1);
                stream.WriteSignedByte((sbyte)type.KeyType);
            }

            if (type.ValType != default(char))
            {
                stream.WriteByte(2);
                stream.WriteSignedByte((sbyte)type.ValType);
            }

            if (type.DefaultStringValue != "null")
            {
                stream.WriteByte(3);
                stream.WriteString(type.DefaultStringValue);
            }

            if (type.DefaultIntValue != 0)
            {
                stream.WriteByte(4);
                stream.WriteInt(type.DefaultIntValue);
            }

            if (type.ValueMap != null && type.ValueMap.Count > 0)
            {
                var firstValue = type.ValueMap.Values.FirstOrDefault();
                if (firstValue is string)
                {
                    stream.WriteByte(5);
                    stream.WriteShort(type.ValueMap.Count);
                    foreach (var kvp in type.ValueMap)
                    {
                        stream.WriteInt(kvp.Key);
                        stream.WriteString((string)kvp.Value);
                    }
                }
                else if (firstValue is int)
                {
                    stream.WriteByte(6);
                    stream.WriteShort(type.ValueMap.Count);
                    foreach (var kvp in type.ValueMap)
                    {
                        stream.WriteInt(kvp.Key);
                        stream.WriteInt((int)kvp.Value);
                    }
                }
            }
            else if (type.Values != null && type.Values.Length > 0)
            {
                object firstValue = type.Values.FirstOrDefault(v => v != null);

                if (firstValue is string)
                {
                    stream.WriteByte(7);
                    stream.WriteShort(type.Values.Length);
                    stream.WriteShort(type.Count);
                    for (int i = 0; i < type.Values.Length; i++)
                    {
                        if (type.Values[i] is string strValue)
                        {
                            stream.WriteShort(i);
                            stream.WriteString(strValue);
                        }
                    }
                }
                else if (firstValue is int)
                {
                    stream.WriteByte(8);
                    stream.WriteShort(type.Values.Length);
                    stream.WriteShort(type.Count);
                    for (int i = 0; i < type.Values.Length; i++)
                    {
                        if (type.Values[i] is int intValue)
                        {
                            stream.WriteShort(i);
                            stream.WriteInt(intValue);
                        }
                    }
                }
            }

            stream.WriteByte(0); // Terminator
            return stream;
        }

        private void ParseOpcode(byte opcode, MemoryStream stream, IClientMapDefinition definition)
        {
            if (opcode == 1)
                definition.KeyType = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            else if (opcode == 2)
                definition.ValType = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            else if (opcode == 3)
                definition.DefaultStringValue = stream.ReadString();
            else if (opcode == 4)
                definition.DefaultIntValue = stream.ReadInt();
            else if (opcode == 5 || opcode == 6)
            {
                definition.Count = stream.ReadUnsignedShort();
                definition.ValueMap = new Dictionary<int, object>(definition.Count);
                for (int i = 0; i < definition.Count; i++)
                {
                    int key = stream.ReadInt();
                    object value;
                    if (opcode == 5)
                        value = stream.ReadString();
                    else
                        value = stream.ReadInt();
                    definition.ValueMap.Add(key, value);
                }
            }
            else if (opcode == 7 || opcode == 8)
            {
                int valueSize = stream.ReadUnsignedShort();
                definition.Count = stream.ReadUnsignedShort();
                definition.Values = new object[valueSize];
                for (int i = 0; i < definition.Count; i++)
                {
                    int index = stream.ReadUnsignedShort();
                    if (opcode == 7)
                        definition.Values[index] = stream.ReadString();
                    else
                        definition.Values[index] = stream.ReadInt();
                }
            }
        }
    }
}
