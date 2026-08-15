namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides a base implementation for a state that remains active until explicitly removed.
    /// </summary>
    public abstract class State : IState, IStateReapplicationPolicy
    {
        /// <inheritdoc />
        public virtual StateReapplicationPolicy ReapplicationPolicy => StateReapplicationPolicy.KeepExisting;
    }
}
