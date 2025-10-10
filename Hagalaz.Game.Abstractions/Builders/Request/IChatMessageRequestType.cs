using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for a chat message request where the type of chat must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestType
    {
        /// <summary>
        /// Sets the type of the chat message.
        /// </summary>
        /// <param name="type">The <see cref="ChatMessageType"/> that defines the format and context of the chat (e.g., plain message, NPC dialogue).</param>
        /// <returns>The next step in the fluent builder chain, which allows for adding selectable options to the request.</returns>
        IChatMessageRequestOption WithType(ChatMessageType type);
    }
}