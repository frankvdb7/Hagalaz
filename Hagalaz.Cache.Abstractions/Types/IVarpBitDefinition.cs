namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a VarpBit definition.
    /// </summary>
    public interface IVarpBitDefinition : IType
    {
        /// <summary>
        /// Gets or sets the standard configuration identifier.
        /// </summary>
        short ConfigID { get; set; }

        /// <summary>
        /// Gets or sets the length of the bit.
        /// </summary>
        byte BitLength { get; set; }

        /// <summary>
        /// Gets or sets the bit offset.
        /// </summary>
        byte BitOffset { get; set; }
    }
}
