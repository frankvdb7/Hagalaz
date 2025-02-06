namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface IForceMovement
    {
        /// <summary>
        /// Location where movement starts.
        /// </summary>
        ILocation StartLocation { get; }
        /// <summary>
        /// Location where movement ends.
        /// </summary>
        ILocation EndLocation { get; }
        /// <summary>
        /// Speed per which the creature will move to start location 
        /// from current location. 1 = 30ms, 2 = 60ms and so on.
        /// </summary>
        int EndSpeed { get; }
        /// <summary>
        /// Speed per which the creature will move to end location
        /// from current location. 1 = 30ms, 2 = 60ms and so on.
        /// </summary>
        int StartSpeed { get; }
        /// <summary>
        /// Contains creature facing direction while moving.
        /// </summary>
         FaceDirection FaceDirection { get; }
    }
}
