namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TAttacker">The type of the attacker.</typeparam>
    public interface IAttackerInfo<TAttacker>
    {
        /// <summary>
        ///     Gets or sets the damage.
        /// </summary>
        /// <value>The damage.</value>
        int TotalDamage { get; set; }

        /// <summary>
        ///     Contains last tick
        /// </summary>
        /// <value>The last attack tick.</value>
        int LastAttackTick { get; set; }

        /// <summary>
        ///     Contains attacker.
        /// </summary>
        /// <value>The attacker creature.</value>
        TAttacker Attacker { get; }
    }
}
