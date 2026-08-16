namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Marks a state as requiring character persistence across hydration boundaries.
    /// </summary>
    public interface IPersistentState : IState
    {
    }
}
