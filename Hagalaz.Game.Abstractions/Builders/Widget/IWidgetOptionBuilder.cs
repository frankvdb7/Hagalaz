using System;
using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    /// <summary>
    /// Represents the nested builder for configuring the interaction options of a widget component.
    /// The options are combined into a single integer value representing a bitmask.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is used within the fluent API provided by <see cref="IWidgetBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetOptionBuilder
    {
        /// <summary>
        /// Gets the final integer value representing the combined, bitmasked settings for the widget component's options.
        /// </summary>
        int Value { get; }

        /// <summary>
        /// Enables or disables the default left-click action for the component.
        /// </summary>
        /// <param name="allowed">If set to <c>true</c>, the default left-click action is allowed.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IWidgetOptionBuilder SetLeftClickOption(bool allowed);

        /// <summary>
        /// Sets the availability of a range of right-click options.
        /// </summary>
        /// <param name="optionCount">The number of right-click options to configure (typically 1-10).</param>
        /// <param name="allowed">If set to <c>true</c>, the specified right-click options are allowed.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IWidgetOptionBuilder SetRightClickOptions(int optionCount, bool allowed);

        /// <summary>
        /// Enables or disables a specific right-click option by its ID.
        /// </summary>
        /// <param name="optionID">The identifier of the right-click option (0-9).</param>
        /// <param name="allowed">If set to <c>true</c>, the specified right-click option is allowed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the option ID is not between 0 and 9.</exception>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IWidgetOptionBuilder SetRightClickOption(int optionID, bool allowed);

        /// <summary>
        /// Configures which "Use on" interactions are permitted for this component (e.g., using an item on the component).
        /// </summary>
        /// <param name="options">An array of <see cref="UseOnOption"/> flags to be enabled.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IWidgetOptionBuilder SetUseOnOptions(params UseOnOption[] options);

        /// <summary>
        /// Sets the event propagation depth for this component. A higher depth allows events (like clicks)
        /// to pass through to underlying components. For example, a depth of 1 can enable dragging items
        /// from an inventory widget over the main game screen widget.
        /// </summary>
        /// <param name="depth">The event depth level (0-7).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the depth is not between 0 and 7.</exception>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IWidgetOptionBuilder SetEventsDepth(int depth);

        /// <summary>
        /// Globally enables or disables the ability for items to be used on this component.
        /// </summary>
        /// <param name="allow">If set to <c>true</c>, items can be used on this component.</param>
        /// <returns>The same builder instance to allow for further configuration.</returns>
        IWidgetOptionBuilder SetCanUseOn(bool allow);
    }
}