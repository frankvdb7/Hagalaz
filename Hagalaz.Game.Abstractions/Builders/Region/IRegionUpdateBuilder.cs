namespace Hagalaz.Game.Abstractions.Builders.Region
{
    /// <summary>
    /// Defines the contract for a region update builder, which serves as the entry point
    /// for constructing an <see cref="Model.Maps.Updates.IRegionPartUpdate"/> object using a fluent interface.
    /// </summary>
    public interface IRegionUpdateBuilder
    {
        /// <summary>
        /// Begins the process of building a new region update.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the location of the update.</returns>
        IRegionUpdateLocation Create();
    }
}