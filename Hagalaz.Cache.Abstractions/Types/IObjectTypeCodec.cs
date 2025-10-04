using Hagalaz.Cache.Abstractions.Logic.Codecs;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a codec for encoding and decoding <see cref="IObjectType"/> objects.
    /// </summary>
    public interface IObjectTypeCodec : ITypeCodec<IObjectType>
    {
    }
}