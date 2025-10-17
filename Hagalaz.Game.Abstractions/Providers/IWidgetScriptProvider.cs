using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that maps a widget (UI component) ID to its corresponding script type.
    /// These scripts handle the logic and interactions for UI elements like buttons, dialogues, and shops.
    /// </summary>
    public interface IWidgetScriptProvider
    {
        /// <summary>
        /// Finds the <see cref="Type"/> of the script for a specific widget.
        /// </summary>
        /// <param name="id">The unique identifier of the widget.</param>
        /// <returns>The script <see cref="Type"/> for the specified widget, or a default/null type if not found.</returns>
        Type FindScriptTypeById(int id);

        /// <summary>
        /// Gets the total number of registered widget script interfaces.
        /// </summary>
        /// <returns>The count of widget script interfaces.</returns>
        int GetInterfacesCount();
    }
}