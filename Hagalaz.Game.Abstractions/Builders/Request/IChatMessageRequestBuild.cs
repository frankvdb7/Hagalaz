using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a chat message request.
    /// This interface provides the method to construct the final <see cref="IChatMessageRequest"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IChatMessageRequest"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IChatMessageRequest"/> object configured with the specified properties.</returns>
        IChatMessageRequest Build();
    }
}