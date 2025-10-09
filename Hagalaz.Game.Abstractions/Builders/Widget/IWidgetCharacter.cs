using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a widget where the target character must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IWidgetBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetCharacter
    {
        /// <summary>
        /// Specifies the character for whom the widget will be built and displayed.
        /// </summary>
        /// <param name="character">The <see cref="ICharacter"/> who will see and interact with the widget.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the widget's ID.</returns>
        IWidgetId ForCharacter(ICharacter character);
    }
}