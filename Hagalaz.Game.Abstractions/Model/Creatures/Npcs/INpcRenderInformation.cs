namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Hagalaz.GameAbstractions.Model.Creatures.ICreatureRenderInformation" />
    public interface INpcRenderInformation : ICreatureRenderInformation
    {
        /// <summary>
        /// Gets the update flag.
        /// </summary>
        /// <value>
        /// The update flag.
        /// </value>
        UpdateFlags UpdateFlag { get; }
        /// <summary>
        /// Shedule's flag based update to creature.
        /// </summary>
        /// <param name="flag"></param>
        void ScheduleFlagUpdate(UpdateFlags flag);
        /// <summary>
        /// Cancel's sheduled flag creature.
        /// </summary>
        /// <param name="flag"></param>
        void CancelScheduledUpdate(UpdateFlags flag);
    }
}
