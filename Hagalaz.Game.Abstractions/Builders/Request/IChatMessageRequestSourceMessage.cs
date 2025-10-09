using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for a chat message request where the source character's message must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestSourceMessage
    {
        /// <summary>
        /// Sets the message content from the source character.
        /// </summary>
        /// <param name="message">The chat message to be displayed from the source.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the target of the request.</returns>
        IChatMessageRequestTarget WithSourceMessage(string message);
    }
}