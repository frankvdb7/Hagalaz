namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Hagalaz.GameAbstractions.Model.Creatures.ICreatureAppearance" />
    public interface INpcAppearance : ICreatureAppearance
    {
        /// <summary>
        /// Contains npc composite Id.
        /// </summary>
        int CompositeID { get; }
        /// <summary>
        /// Transforms this npc to other npc.
        /// </summary>
        /// <param name="compositeID">The composite identifier.</param>
        void Transform(int compositeID);
    }
}
