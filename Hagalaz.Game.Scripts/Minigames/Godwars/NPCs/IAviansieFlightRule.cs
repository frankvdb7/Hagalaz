using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <summary>
    ///     The Armadyl aviansie rule: they fly too high to be hit by melee.
    /// </summary>
    public interface IAviansieFlightRule
    {
        /// <summary>
        ///     Denies a melee attacker: tells characters why and, when allowed, makes the aviansie retaliate next tick.
        /// </summary>
        /// <param name="owner">The aviansie.</param>
        /// <param name="attacker">The creature trying to attack it.</param>
        /// <param name="canRetaliate">Whether the aviansie may retaliate against this attacker.</param>
        /// <returns><c>true</c> if the attacker used melee and was denied; <c>false</c> if normal attack rules apply.</returns>
        bool TryDenyMelee(INpc owner, ICreature attacker, bool canRetaliate);
    }
}
