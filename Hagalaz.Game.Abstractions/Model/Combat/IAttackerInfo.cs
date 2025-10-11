namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines a contract for an object that tracks information about damage received from a specific attacker.
    /// This is used to manage threat levels and determine who gets credit for a kill.
    /// </summary>
    /// <typeparam name="TAttacker">The type of the attacker (e.g., a specific creature implementation).</typeparam>
    public interface IAttackerInfo<out TAttacker>
    {
        /// <summary>
        /// Gets or sets the total damage dealt by this attacker.
        /// </summary>
        int TotalDamage { get; set; }

        /// <summary>
        /// Gets or sets the game tick of the last attack from this attacker.
        /// </summary>
        int LastAttackTick { get; set; }

        /// <summary>
        /// Gets the attacker instance.
        /// </summary>
        TAttacker Attacker { get; }
    }
}
