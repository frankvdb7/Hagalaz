﻿using Hagalaz.Cache.Abstractions.Logic.Codecs;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a specific codec for decoding and encoding <see cref="IObjectType"/> objects.
    /// </summary>
    /// <remarks>
    /// This interface serves as a marker for dependency injection and inherits all its functionality
    /// from the generic <see cref="ITypeCodec{T}"/> interface, specialized for <see cref="IObjectType"/>.
    /// </remarks>
    public interface IObjectTypeCodec : ITypeCodec<IObjectType>
    {
    }
}