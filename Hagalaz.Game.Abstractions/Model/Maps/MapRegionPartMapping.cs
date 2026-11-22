namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Maps a local part of a dynamic region to a source part with a rotation.
    /// </summary>
    /// <param name="LocalPartX">The destination local part X index (0-7).</param>
    /// <param name="LocalPartY">The destination local part Y index (0-7).</param>
    /// <param name="Z">The destination plane.</param>
    /// <param name="SourcePartX">The absolute source part X-coordinate.</param>
    /// <param name="SourcePartY">The absolute source part Y-coordinate.</param>
    /// <param name="SourceZ">The source plane.</param>
    /// <param name="Rotation">The part rotation (0-3).</param>
    public readonly record struct MapRegionPartMapping(int LocalPartX, int LocalPartY, int Z, int SourcePartX, int SourcePartY, int SourceZ, int Rotation);
}
