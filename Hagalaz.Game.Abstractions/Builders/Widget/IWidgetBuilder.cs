namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    /// <summary>
    /// Defines the contract for a widget builder, which serves as the entry point
    /// for constructing an <see cref="Model.Widgets.IWidget"/> object using a fluent interface.
    /// </summary>
    public interface IWidgetBuilder
    {
        /// <summary>
        /// Begins the process of building a new widget.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the character for whom the widget will be displayed.</returns>
        IWidgetCharacter Create();
    }
}