using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    /// <summary>
    /// Defines a specific codec for decoding and encoding <see cref="IAnimationType"/> objects.
    /// </summary>
    /// <remarks>
    /// This interface serves as a marker for dependency injection and inherits all its functionality
    /// from the generic <see cref="ITypeCodec{T}"/> interface.
    /// </remarks>
    public interface IAnimationTypeCodec : ITypeCodec<IAnimationType>
    {
    }
}