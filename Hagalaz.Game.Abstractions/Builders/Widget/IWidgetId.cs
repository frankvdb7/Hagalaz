using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a widget where the widget's ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IWidgetBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetId
    {
        /// <summary>
        /// Sets the unique identifier for the widget being built.
        /// </summary>
        /// <param name="id">The unique identifier for the widget.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        IWidgetOptional WithId(int id);
    }
}