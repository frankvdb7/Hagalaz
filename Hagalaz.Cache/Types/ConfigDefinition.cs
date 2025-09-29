using System.IO;
using System.Text;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigDefinition
    {
        /// <summary>
        /// The configuration definitions
        /// </summary>
        private static ConfigDefinition[]? configDefinitions;
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        public short ID { get; }
        /// <summary>
        /// Contains the default value.
        /// </summary>
        public int DefaultValue { get; private set; }
        /// <summary>
        /// Gets a char6673.
        /// </summary>
        public char AChar6673 { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigDefinition"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public ConfigDefinition(short id)
        {
            ID = id;
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
            {
                AChar6673 = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            }
            else if (opcode == 5)
            {
                DefaultValue = (short)stream.ReadUnsignedShort();
            }
        }

        /// <summary>
        /// Gets the scripts count.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public static int GetConfigsCount(CacheApi cache) => cache.GetFileCount(2, 16);

        /// <summary>
        /// Gets the configuration definition.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="configId">The configuration identifier.</param>
        /// <returns></returns>
        public static ConfigDefinition GetConfigDefinition(CacheApi cache, short configId)
        {
            if (configDefinitions == null)
                configDefinitions = new ConfigDefinition[GetConfigsCount(cache)];
            if (configDefinitions[configId] != null)
                return configDefinitions[configId];

            var definition = configDefinitions[configId] = new ConfigDefinition(configId);

            using (var data = cache.Read(2, 16, configId))
            {
                definition.ParseDefinition(data);
            }
            return definition;
        }
    }
}
