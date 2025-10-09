using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for a chat message request where the target character's message must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestTargetMessage
    {
        /// <summary>
        /// Sets the message content to be displayed as if coming from the target character.
        /// </summary>
        /// <param name="message">The chat message to be displayed.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the type of chat request.</returns>
        IChatMessageRequestType WithTargetMessage(string message);
    }
}