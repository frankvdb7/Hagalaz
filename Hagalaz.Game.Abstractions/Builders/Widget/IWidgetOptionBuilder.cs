using System;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    public interface IWidgetOptionBuilder
    {
        /// <summary>
        /// Contains the settings value.
        /// </summary>
        int Value { get; }

        /// <summary>
        /// Sets the left click option settings.
        /// </summary>
        /// <param name="allowed">if set to <c>true</c> [allowed].</param>
        IWidgetOptionBuilder SetLeftClickOption(bool allowed);

        /// <summary>
        /// Sets the right click options.
        /// </summary>
        /// <param name="optionCount">The options.</param>
        /// <param name="allowed">if set to <c>true</c> [allowed].</param>
        IWidgetOptionBuilder SetRightClickOptions(int optionCount, bool allowed);

        /// <summary>
        /// Sets the right click option.
        /// </summary>
        /// <param name="optionID">The option identifier.</param>
        /// <param name="allowed">if set to <c>true</c> [allowed].</param>
        /// <exception cref="ArgumentOutOfRangeException">Option Id must be 0-9</exception>
        IWidgetOptionBuilder SetRightClickOption(int optionID, bool allowed);

        /// <summary>
        /// Sets the use on options.
        /// </summary>
        /// <param name="options">The settings.</param>
        IWidgetOptionBuilder SetUseOnOptions(params UseOnOption[] options);

        /// <summary>
        /// Sets the events depth.
        /// For example, we have the inventory interface opened on the
        /// game frame interface (548 or 746).
        /// If depth is 1, then the clicks in the inventory will also invoke
        /// click handler scripts on the game frame interface.
        /// EventDepth 1 for example, allows dragging.
        /// </summary>
        /// <param name="depth">The depth.</param>
        /// <exception cref="ArgumentOutOfRangeException">Depth must be 0-7</exception>
        IWidgetOptionBuilder SetEventsDepth(int depth);

        /// <summary>
        /// Sets the can use on.
        /// </summary>
        /// <param name="allow">if set to <c>true</c> [allow].</param>
        IWidgetOptionBuilder SetCanUseOn(bool allow);
    }
}