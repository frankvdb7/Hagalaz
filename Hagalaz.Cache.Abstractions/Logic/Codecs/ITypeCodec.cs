using System.IO;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    /// <summary>
    /// Defines a generic contract for codecs that handle the serialization and deserialization
    /// of cacheable types that implement the <see cref="IType"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the object to be encoded or decoded, which must implement <see cref="IType"/>.</typeparam>
    public interface ITypeCodec<T> where T : IType
    {
        /// <summary>
        /// Decodes a memory stream into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="id">The unique identifier of the object being decoded.</param>
        /// <param name="stream">The <see cref="MemoryStream"/> containing the raw byte data to decode.</param>
        /// <returns>An instance of <typeparamref name="T"/> populated with the decoded data.</returns>
        T Decode(int id, MemoryStream stream);

        /// <summary>
        /// Encodes an object of type <typeparamref name="T"/> into a memory stream.
        /// </summary>
        /// <param name="instance">The instance of <typeparamref name="T"/> to encode.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the serialized byte data of the object.</returns>
        MemoryStream Encode(T instance);
    }
}