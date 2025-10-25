using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents a VarpBit definition.
    /// </summary>
    public class VarpBitDefinition : IVarpBitDefinition
    {
        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public short ConfigID { get; set; }

        /// <inheritdoc />
        public byte BitLength { get; set; }

        /// <inheritdoc />
        public byte BitOffset { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VarpBitDefinition"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public VarpBitDefinition(int id)
        {
            Id = id;
        }
    }
}
