using System;
using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents a step in the fluent builder pattern for a chat message request where
    /// a selectable option can be added.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// After adding an option, the builder transitions to the final build step.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestOption
    {
        /// <summary>
        /// Adds a selectable option to the chat message request.
        /// </summary>
        /// <param name="optionBuilder">An action that configures the option, such as setting its text and the action to perform when selected, using the <see cref="IChatMessageRequestOptionBuilder"/>.</param>
        /// <returns>The final build step of the fluent builder chain, allowing for the request to be constructed.</returns>
        IChatMessageRequestBuild WithOption(Action<IChatMessageRequestOptionBuilder> optionBuilder);
    }
}