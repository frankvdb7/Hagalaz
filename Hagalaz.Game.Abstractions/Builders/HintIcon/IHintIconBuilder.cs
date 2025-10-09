namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    /// <summary>
    /// Defines the contract for a hint icon builder, which serves as the entry point
    /// for constructing an <see cref="Model.IHintIcon"/> object using a fluent interface.
    /// </summary>
    public interface IHintIconBuilder
    {
        /// <summary>
        /// Begins the process of building a new hint icon.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the type of hint icon to create.</returns>
        IHintIconType Create();
    }
}