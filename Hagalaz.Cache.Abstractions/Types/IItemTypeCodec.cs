using System.IO;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a codec for encoding and decoding <see cref="IItemType"/> objects.
    /// </summary>
    public interface IItemTypeCodec
    {
        /// <summary>
        /// Decodes an <see cref="IItemType"/> from the given <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="id">The ID of the item type to decode.</param>
        /// <param name="stream">The stream to read the data from.</param>
        /// <returns>The decoded <see cref="IItemType"/>.</returns>
        IItemType Decode(int id, MemoryStream stream);

        /// <summary>
        /// Encodes an <see cref="IItemType"/> into a <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="type">The <see cref="IItemType"/> to encode.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the encoded data.</returns>
        MemoryStream Encode(IItemType type);
    }
}