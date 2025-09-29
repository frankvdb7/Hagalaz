using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class Cs2Definition
    {
        /// <summary>
        /// The CS2 definitions.
        /// </summary>
        private static Cs2Definition[]? cs2Definitions;
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        public short ID { get; }
        /// <summary>
        /// Contains the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cs2Definition"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public Cs2Definition(short id)
        {
            ID = id;
            Name = string.Empty;
        }

        /// <summary>
        /// Parse's definition from given buffer.
        /// </summary>
        /// <param name="buffer">Buffer from which definition should be parsed.</param>
        private void ParseDefinition(MemoryStream buffer)
        {
            buffer.Position = buffer.Length - 2;
            int switchBlocksSize = buffer.ReadUnsignedShort();
            long codeBlockEnd = buffer.Length - switchBlocksSize - 16 - 2;
            buffer.Position = codeBlockEnd;
            int codeSize = buffer.ReadInt();
            int intLocalsCount = buffer.ReadUnsignedShort();
            int stringLocalsCount = buffer.ReadUnsignedShort();
            int longLocalsCount = buffer.ReadUnsignedShort();
            int intArgsCount = buffer.ReadUnsignedShort();
            int stringArgsCount = buffer.ReadUnsignedShort();
            int longArgsCount = buffer.ReadUnsignedShort();

            int switchesCount = buffer.ReadUnsignedByte();
            Dictionary<int, int>[] switches = new Dictionary<int, int>[switchesCount];
            for (int i = 0; i < switchesCount; i++)
            {
                int numCases = buffer.ReadUnsignedShort();
                switches[i] = new Dictionary<int, int>(numCases);
                while (numCases-- > 0)
                {
                    switches[i].Add(buffer.ReadInt(), buffer.ReadInt());
                }
            }
            buffer.Position = 0;
            Name = buffer.ReadCheckedString();

            int[] intPool = new int[codeSize];
            string[] stringPool = new string[codeSize];
            long[] longPool = new long[codeSize];
            int[] opcodes = new int[codeSize];

            int writeOffset = 0;
            while (buffer.Position < codeBlockEnd)
            {
                int opcode = buffer.ReadUnsignedShort();
                if (opcode == 145)
                    stringPool[writeOffset] = buffer.ReadString();
                else if (opcode == 63)
                    longPool[writeOffset] = buffer.ReadLong();
                else if (opcode == 3 || opcode == 122 || opcode == 145 || opcode == 323 || opcode == 362 || 
                        opcode == 527 || opcode == 573 || opcode == 588 || opcode == 606 || opcode == 703 || 
                        opcode == 728 || opcode == 1044)
                {

                }

                opcodes[writeOffset++] = opcode;
            }
        }

        /// <summary>
        /// Gets the cs2 count.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public static int GetCs2Count(CacheApi cache)
        {
            var lastId = cache.GetFileCount(12) - 1;
            return cache.GetFileCount(12, lastId);
        }

        /// <summary>
        /// Gets the cs2 definition.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="scriptId">The script identifier.</param>
        /// <returns></returns>
        public static Cs2Definition GetCs2Definition(CacheApi cache, short scriptId)
        {
            if (cs2Definitions == null)
                cs2Definitions = new Cs2Definition[GetCs2Count(cache)];
            if (cs2Definitions[scriptId] != null)
                return cs2Definitions[scriptId];

            var definition = cs2Definitions[scriptId] = new Cs2Definition(scriptId);

            using (var data = cache.Read(12, scriptId, 0))
            {
                definition.ParseDefinition(data);
            }
            return definition;
        }
    }
}
