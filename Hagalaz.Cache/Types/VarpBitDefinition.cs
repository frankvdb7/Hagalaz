using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class VarpBitDefinition
    {
        /// <summary>
        /// The varp bit definitions.
        /// </summary>
        private static VarpBitDefinition?[]? varpBitDefinitions;
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        public short ID { get; }
        /// <summary>
        /// Contains the standard configuration identifier.
        /// </summary>
        public short ConfigID { get; private set; }
        /// <summary>
        /// Contains the length of the bit.
        /// </summary>
        public byte BitLength { get; private set; }
        /// <summary>
        /// Contains the bit offset.
        /// </summary>
        public byte BitOffset { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VarpBitDefinition"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public VarpBitDefinition(short id)
        {
            ID = id;
        }

        /// <summary>
        /// Parse's definition from given buffer.
        /// </summary>
        /// <param name="buffer">Buffer from which definition should be parsed.</param>
        private void ParseDefinition(MemoryStream buffer)
        {
            for (var opcode = (byte)buffer.ReadUnsignedByte(); opcode != 0; opcode = (byte)buffer.ReadUnsignedByte())
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
                ConfigID = (short)stream.ReadUnsignedShort();
                BitOffset = (byte)stream.ReadUnsignedByte();
                BitLength = (byte)stream.ReadUnsignedByte();
            }
        }
        /// <summary>
        /// Gets the scripts count.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public static int GetVarBitsCount(CacheApi cache) => (cache.GetFileCount(22) - 1) * 0x3ff;

        /// <summary>
        /// Gets the varp bit definition.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="varpBitID">The varp bit identifier.</param>
        /// <returns></returns>
        public static VarpBitDefinition? GetVarpBitDefinition(CacheApi cache, short varpBitID)
        {
            if (varpBitDefinitions == null)
                varpBitDefinitions = new VarpBitDefinition[GetVarBitsCount(cache)];
            if (varpBitDefinitions[varpBitID] != null)
                return varpBitDefinitions[varpBitID];

            var definition = varpBitDefinitions[varpBitID] = new VarpBitDefinition(varpBitID);

            using (var data = cache.Read(22, (int) (((uint) varpBitID) >> 10), varpBitID & 0x3FF))
            {
                definition.ParseDefinition(data);
            }
            return definition;
        }
    }
}
