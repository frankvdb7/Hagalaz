using Hagalaz.Cache.Abstractions.Logic.Codecs;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a specific codec for decoding and encoding <see cref="IItemType"/> objects.
    /// </summary>
    /// <remarks>
    /// This interface serves as a marker for dependency injection and inherits all its functionality
    /// from the generic <see cref="ITypeCodec{T}"/> interface, specialized for <see cref="IItemType"/>.
    /// </remarks>
    public interface IItemTypeCodec : ITypeCodec<IItemType>
    {
    }
}