namespace Hagalaz.Game.Abstractions.Builders.Graphic
{
    /// <summary>
    /// Defines the contract for a graphic effect builder, which serves as the entry point
    /// for constructing an <see cref="Model.IGraphic"/> object using a fluent interface.
    /// </summary>
    public interface IGraphicBuilder
    {
        /// <summary>
        /// Begins the process of building a new graphic effect.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the graphic's ID.</returns>
        IGraphicId Create();
    }
}