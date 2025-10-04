using Hagalaz.Cache.Abstractions.Logic.Codecs;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a codec for encoding and decoding <see cref="IItemType"/> objects.
    /// </summary>
    public interface IItemTypeCodec : ITypeCodec<IItemType>
    {
    }
}