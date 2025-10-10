using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a user interface widget.
    /// This interface provides methods to either construct the <see cref="IWidget"/> object
    /// or to construct and immediately open it for the target character.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IWidgetBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetBuild
    {
        /// <summary>
        /// Builds the configured <see cref="IWidget"/> instance without opening it.
        /// This is useful if the widget object needs to be returned or manipulated further before being displayed.
        /// </summary>
        /// <returns>A new <see cref="IWidget"/> object configured with the specified properties.</returns>
        IWidget Build();

        /// <summary>
        /// Builds the configured <see cref="IWidget"/> instance and immediately opens it for the target character.
        /// </summary>
        void Open();
    }
}