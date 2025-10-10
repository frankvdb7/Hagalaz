using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for a chat message request where the target character must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestTarget
    {
        /// <summary>
        /// Sets the target of the chat message request.
        /// </summary>
        /// <param name="character">The <see cref="ICharacter"/> who is receiving the request (e.g., the player).</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the message content for the target.</returns>
        IChatMessageRequestTargetMessage WithTarget(ICharacter character);
    }
}