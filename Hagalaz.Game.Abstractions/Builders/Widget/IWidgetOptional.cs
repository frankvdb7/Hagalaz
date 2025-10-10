using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Scripts;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a widget where optional
    /// parameters like transparency, parentage, and scripts can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IWidgetBuilder"/>.
    /// It also inherits from <see cref="IWidgetBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetOptional : IWidgetBuild
    {
        /// <summary>
        /// Sets the transparency level of the widget.
        /// </summary>
        /// <param name="transparency">The transparency level (0-255), where 0 is fully opaque and 255 is fully transparent.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IWidgetOptional WithTransparency(int transparency);

        /// <summary>
        /// Sets the ID of the parent widget that this widget will be a child of.
        /// </summary>
        /// <param name="parentId">The unique identifier of the parent widget.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IWidgetOptional WithParentId(int parentId);

        /// <summary>
        /// Sets the slot within the parent widget where this widget will be placed.
        /// </summary>
        /// <param name="parentSlot">The index of the slot within the parent widget.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IWidgetOptional WithParentSlot(int parentSlot);

        /// <summary>
        /// Attaches a specific script instance to the widget to define its behavior.
        /// </summary>
        /// <param name="script">An instance of a class that implements <see cref="IWidgetScript"/>.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IWidgetOptional WithScript(IWidgetScript script);

        /// <summary>
        /// Attaches a script to the widget by its type. The script will be resolved from the dependency injection container.
        /// </summary>
        /// <typeparam name="TScript">The type of the script, which must implement <see cref="IWidgetScript"/>.</typeparam>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IWidgetOptional WithScript<TScript>() where TScript : IWidgetScript;

        /// <summary>
        /// Marks the widget as a frame, which may alter its rendering or behavior (e.g., making it a container).
        /// </summary>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IWidgetOptional AsFrame();
    }
}