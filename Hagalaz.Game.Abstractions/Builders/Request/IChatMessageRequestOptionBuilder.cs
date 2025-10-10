using System;
using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Represents the nested builder for configuring a single option within a chat message request.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is used within the fluent API provided by <see cref="IChatMessageRequestBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IChatMessageRequestOptionBuilder
    {
        /// <summary>
        /// Sets the type of click interaction associated with this option.
        /// </summary>
        /// <param name="clickType">The <see cref="CharacterClickType"/> that defines the interaction.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IChatMessageRequestOptionBuilder WithType(CharacterClickType clickType);

        /// <summary>
        /// Sets the action to be executed when this option is selected by the player.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> delegate to execute.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IChatMessageRequestOptionBuilder WithAction(Action action);

        /// <summary>
        /// Sets the display text for this option.
        /// </summary>
        /// <param name="name">The text to be displayed to the player for this option.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IChatMessageRequestOptionBuilder WithName(string name);
    }
}