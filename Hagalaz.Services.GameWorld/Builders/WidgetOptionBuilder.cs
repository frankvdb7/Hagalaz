using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Services.GameWorld.Builders
{
    /// <summary>
    /// A class that contains the interface options. (A.K.A Access Masks)
    /// </summary>
    public class WidgetOptionBuilder : IWidgetOptionBuilder
    {
        /// <summary>
        /// Contains the settings value.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Sets the left click option settings.
        /// </summary>
        /// <param name="allowed">if set to <c>true</c> [allowed].</param>
        public IWidgetOptionBuilder SetLeftClickOption(bool allowed)
        {
            Value &= ~0x1;
            if (allowed) Value |= 0x1;
            return this;
        }

        /// <summary>
        /// Sets the right click options.
        /// </summary>
        /// <param name="optionCount">The options.</param>
        /// <param name="allowed">if set to <c>true</c> [allowed].</param>
        public IWidgetOptionBuilder SetRightClickOptions(int optionCount, bool allowed)
        {
            for (var optionID = 0; optionID <= optionCount; optionID++) SetRightClickOption(optionID, allowed);
            return this;
        }

        /// <summary>
        /// Sets the right click option.
        /// </summary>
        /// <param name="optionID">The option identifier.</param>
        /// <param name="allowed">if set to <c>true</c> [allowed].</param>
        /// <exception cref="ArgumentOutOfRangeException">Option Id must be 0-9</exception>
        public IWidgetOptionBuilder SetRightClickOption(int optionID, bool allowed)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(optionID);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(optionID, 9);
            Value &= ~(0x1 << optionID + 1);
            if (allowed) Value |= 0x1 << optionID + 1;
            return this;
        }

        /// <summary>
        /// Sets the use on options.
        /// </summary>
        /// <param name="options">The settings.</param>
        public IWidgetOptionBuilder SetUseOnOptions(params UseOnOption[] options)
        {
            var flags = options.Aggregate(0, (current, t) => current | 1 << (byte)t);

            Value &= ~(0x7f << 11); // 7
            Value |= flags << 11; // 7
            return this;
        }

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
        public IWidgetOptionBuilder SetEventsDepth(int depth)
        {
            if (depth < 0 || depth > 7) throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be 0-7");
            Value &= ~(0x7 << 18);
            Value |= depth << 18;
            return this;
        }

        /// <summary>
        /// Sets the can use on.
        /// </summary>
        /// <param name="allow">if set to <c>true</c> [allow].</param>
        public IWidgetOptionBuilder SetCanUseOn(bool allow)
        {
            Value &= ~(1 << 22);
            if (allow) Value |= 1 << 22;
            return this;
        }
    }
}