namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Opts a timed state into keeping the instance with the longest remaining duration.
    /// </summary>
    public interface IKeepLongestDurationState : IStateReapplicationPolicy
    {
        /// <inheritdoc />
        StateReapplicationPolicy IStateReapplicationPolicy.ReapplicationPolicy => StateReapplicationPolicy.KeepLongestDuration;
    }
}
