using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class ModelType
    {
        /// <summary>
        /// Gets the vertex count.
        /// </summary>
        /// <value>
        /// The vertex count.
        /// </value>
        public int VertexCount { get; private set; } = 0;

        /// <summary>
        /// Gets the face count.
        /// </summary>
        /// <value>
        /// The face count.
        /// </value>
        public int FaceCount { get; private set; } = 0;

        /// <summary>
        /// Parses the definition.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        private void ParseDefinition(MemoryStream buffer)
        {
            buffer.Position = buffer.Capacity - 2;
            if (buffer.ReadShort() == -1)
            {
                ParseNewDefinition(buffer);
            }
            else
            {
                ParseOldDefinition(buffer);
            }
        }

        /// <summary>
        /// Parses the old definition.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        private void ParseOldDefinition(MemoryStream buffer)
        {

        }

        /// <summary>
        /// Parses the new definition.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        private void ParseNewDefinition(MemoryStream buffer)
        {

        }
    }
}
