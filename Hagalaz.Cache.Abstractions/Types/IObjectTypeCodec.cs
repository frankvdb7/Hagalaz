using System.IO;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a codec for encoding and decoding <see cref="IObjectType"/> objects.
    /// </summary>
    public interface IObjectTypeCodec
    {
        /// <summary>
        /// Decodes an <see cref="IObjectType"/> from the given <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="id">The ID of the object type to decode.</param>
        /// <param name="stream">The stream to read the data from.</param>
        /// <returns>The decoded <see cref="IObjectType"/>.</returns>
        IObjectType Decode(int id, MemoryStream stream);

        /// <summary>
        /// Encodes an <see cref="IObjectType"/> into a <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="type">The <see cref="IObjectType"/> to encode.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the encoded data.</returns>
        MemoryStream Encode(IObjectType type);
    }
}