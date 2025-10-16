namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's magic-related state and actions, such as autocasting and rune management.
    /// </summary>
    public interface IMagic
    {
        /// <summary>
        /// Gets the combat spell that is currently set to be autocast.
        /// </summary>
        ICombatSpell? AutoCastingSpell { get; }
        /// <summary>
        /// Gets or sets the spell that has been selected for a single "use on" action.
        /// </summary>
        ICombatSpell? SelectedSpell { get; set; }
        /// <summary>
        /// Clears the currently set autocasting spell.
        /// </summary>
        /// <param name="inform">If set to <c>true</c>, informs the client of the change.</param>
        void ClearAutoCastingSpell(bool inform = true);
        /// <summary>
        /// Checks if the character's Magic level meets a specified requirement.
        /// </summary>
        /// <param name="required">The required Magic level.</param>
        /// <returns><c>true</c> if the level requirement is met; otherwise, <c>false</c>.</returns>
        bool CheckMagicLevel(int required);
        /// <summary>
        /// Checks if the character has the required runes in their inventory to cast a spell.
        /// </summary>
        /// <param name="types">An array of the required rune types.</param>
        /// <param name="runeAmounts">An array of the required amounts for each corresponding rune type.</param>
        /// <returns><c>true</c> if all rune requirements are met; otherwise, <c>false</c>.</returns>
        bool CheckRunes(RuneType[] types, int[] runeAmounts);
        /// <summary>
        /// Sets a combat spell to be autocast by the character.
        /// </summary>
        /// <param name="spell">The combat spell to set for autocasting.</param>
        /// <param name="inform">If set to <c>true</c>, informs the client of the change.</param>
        void SetAutoCastingSpell(ICombatSpell spell, bool inform = true);
        /// <summary>
        /// Removes a specified set of runes from the character's inventory. This method should be called after a successful <see cref="CheckRunes"/> call.
        /// </summary>
        /// <param name="types">An array of the rune types to remove.</param>
        /// <param name="runeAmounts">An array of the amounts to remove for each corresponding rune type.</param>
        void RemoveRunes(RuneType[] types, int[] runeAmounts);
    }
}
