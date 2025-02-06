namespace Hagalaz.Game.Abstractions.Builders.Location
{
    public interface ILocationBuilder
    {
        /// <summary>
        /// Begins the sequence to create a location
        /// </summary>
        /// <returns></returns>
        ILocationX Create();
    }
}