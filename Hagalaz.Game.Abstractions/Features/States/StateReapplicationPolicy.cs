namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the supported duplicate application semantics for a state type.
    /// </summary>
    public enum StateReapplicationPolicy
    {
        /// <summary>Keep the active instance and reject the new application.</summary>
        KeepExisting,

        /// <summary>Remove the active instance and add the new application.</summary>
        Replace,

        /// <summary>Keep the instance with the larger remaining timed duration.</summary>
        KeepLongestDuration
    }
}
