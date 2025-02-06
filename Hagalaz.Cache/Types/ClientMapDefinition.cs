using System.Collections.Generic;
using System.IO;
using System.Text;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientMapDefinition
    {
        /// <summary>
        /// The client script definitions.
        /// </summary>
        private static ClientMapDefinition?[]? clientMapDefinitions;
        /// <summary>
        /// The client script values.
        /// </summary>
        private Dictionary<int, object>? _valueMap;
        /// <summary>
        /// The values.
        /// </summary>
        private object[]? _values;

        /// <summary>
        /// Contains the identifier.
        /// </summary>
        public int ID { get; }
        /// <summary>
        /// Contains the default string value.
        /// </summary>
        public string DefaultStringValue { get; private set; }
        /// <summary>
        /// Contains the default int value.
        /// </summary>
        public int DefaultIntValue { get; private set; }
        /// <summary>
        /// Gets a char5926.
        /// </summary>
        public char KeyType { get; private set; }
        /// <summary>
        /// Gets a char5921.
        /// </summary>
        public char ValType { get; private set; }
        /// <summary>
        /// Contains the count of the values.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientMapDefinition" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public ClientMapDefinition(int id)
        {
            ID = id;
            DefaultStringValue = "null";
        }

        /// <summary>
        /// Gets the int value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public int GetIntValue(int key)
        {
            var value = GetValue(key);
            if (!(value is int))
                return DefaultIntValue;
            return (int)value;
        }

        /// <summary>
        /// Gets the string value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string GetStringValue(int key)
        {
            var value = GetValue(key);
            if (!(value is string))
                return DefaultStringValue;
            return (string)value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public object? GetValue(int key)
        {
            if (_values == null)
            {
                return _valueMap?[key];
            }

            if (key < 0 || key >= _values.Length)
                return null;
            return _values[key];

        }

        /// <summary>
        /// Parse's definition from given buffer.
        /// </summary>
        /// <param name="buffer">Buffer from which definition should be parsed.</param>
        private void ParseDefinition(MemoryStream buffer)
        {
            for (byte opcode = (byte)buffer.ReadUnsignedByte(); opcode != 0; opcode = (byte)buffer.ReadUnsignedByte())
            {
                ParseOpcode(opcode, buffer);
            }
        }

        /// <summary>
        /// Parses the opcode.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="stream">The stream.</param>
        private void ParseOpcode(byte opcode, MemoryStream stream)
        {
            if (opcode == 1)
                KeyType = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            else if (opcode == 2)
                ValType = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            else if (opcode == 3)
                DefaultStringValue = stream.ReadString();
            else if (opcode == 4)
                DefaultIntValue = stream.ReadInt();
            else if (opcode == 5 || opcode == 6)
            {
                Count = stream.ReadUnsignedShort();
                _valueMap = new Dictionary<int, object>(Count);
                for (int i = 0; i < Count; i++)
                {
                    int key = stream.ReadInt();
                    object value;
                    if (opcode == 5)
                        value = stream.ReadString();
                    else
                        value = stream.ReadInt();
                    _valueMap.Add(key, value);
                }
            }
            else if (opcode == 7 || opcode == 8)
            {
                int valueSize = stream.ReadUnsignedShort();
                Count = stream.ReadUnsignedShort();
                _values = new object[valueSize];
                for (int i = 0; i < Count; i++)
                {
                    int index = stream.ReadUnsignedShort();
                    if (opcode == 7)
                        _values[index] = stream.ReadString();
                    else
                        _values[index] = stream.ReadInt();
                }
            }
        }

        /// <summary>
        /// Gets the scripts count.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public static int GetScriptsCount(ICacheAPI cache)
        {
            var lastID = cache.GetFileCount(17) - 1;
            return (lastID * 256) + cache.GetFileCount(17, lastID);
        }

        /// <summary>
        /// Gets the client script definition.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="mapId">The script identifier.</param>
        /// <returns></returns>
        public static ClientMapDefinition? GetClientMapDefinition(ICacheAPI cache, int mapId)
        {
            if (clientMapDefinitions == null)
                clientMapDefinitions = new ClientMapDefinition[GetScriptsCount(cache)];
            if (clientMapDefinitions[mapId] != null)
                return clientMapDefinitions[mapId];

            var definition = clientMapDefinitions[mapId] = new ClientMapDefinition(mapId);

            using (var data = cache.Read(17, (int) (((uint) mapId) >> 8), mapId & 0xFF))
            {
                definition.ParseDefinition(data);
            }
            return definition;
        }
    }
}
