using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for a chat message request where the source character must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestSource
    {
        /// <summary>
        /// Sets the source of the chat message request.
        /// </summary>
        /// <param name="character">The <see cref="ICharacter"/> who is initiating the request (e.g., the NPC speaking).</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the message content.</returns>
        IChatMessageRequestSourceMessage WithSource(ICharacter character);
    }
}