using System.IO;
using System.Text;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class Cs2IntDefinition
    {
        /// <summary>
        /// The configuration definitions
        /// </summary>
        private static Cs2IntDefinition[]? cs2IntDefinitions;
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        public short ID { get; }
        /// <summary>
        /// Contains a char327.
        /// </summary>
        public char AChar327 { get; private set; }
        /// <summary>
        /// Contains an int325.
        /// </summary>
        public int AnInt325 { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cs2IntDefinition"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public Cs2IntDefinition(short id)
        {
            ID = id;
            AnInt325 = 1;
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
                AChar327 = Encoding.ASCII.GetChars(new byte[1] { (byte)(stream.ReadSignedByte() & 0xFF) })[0];
            }
            else if (opcode == 2)
            {
                AnInt325 = 0;
            }
        }

        /// <summary>
        /// Gets the scripts count.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public static int GetCs2IntCount(CacheApi cache) => cache.GetFileCount(2, 19);

        /// <summary>
        /// Gets the cs2 int definition.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="configId">The configuration identifier.</param>
        /// <returns></returns>
        public static Cs2IntDefinition GetCs2IntDefinition(CacheApi cache, short configId)
        {
            if (cs2IntDefinitions == null)
                cs2IntDefinitions = new Cs2IntDefinition[GetCs2IntCount(cache)];
            if (cs2IntDefinitions[configId] != null)
                return cs2IntDefinitions[configId];

            var definition = cs2IntDefinitions[configId] = new Cs2IntDefinition(configId);

            using (var data = cache.Read(2, 19, configId))
            {
                definition.ParseDefinition(data);
            }
            return definition;
        }
    }
}
