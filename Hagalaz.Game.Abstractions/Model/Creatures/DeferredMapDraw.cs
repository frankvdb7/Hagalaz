namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// The flags of a map draw that is waiting for a dynamic region to finish population.
    /// </summary>
    public readonly record struct DeferredMapDraw(bool ForceUpdate, bool RenderViewport);
}
