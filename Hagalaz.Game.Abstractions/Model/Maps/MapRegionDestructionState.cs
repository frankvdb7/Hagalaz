namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>Describes the cleanup lifecycle of a map region instance.</summary>
    public enum MapRegionDestructionState
    {
        Active,
        Destroying,
        Destroyed
    }
}
