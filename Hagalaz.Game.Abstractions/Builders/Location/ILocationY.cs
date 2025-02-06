namespace Hagalaz.Game.Abstractions.Builders.Location
{
    public interface ILocationY
    {
        /// <summary>
        /// Sets the y coordinate on the location
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        ILocationOptional WithY(int y);
    }
}