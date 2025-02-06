namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMagic
    {
        /// <summary>
        /// Contains spell that is being autocasted.
        /// </summary>
        ICombatSpell? AutoCastingSpell { get; }
        /// <summary>
        /// Contains spell that was select by 'use on'.
        /// </summary>
        ICombatSpell? SelectedSpell { get; set; }
        /// <summary>
        /// Clear's autocasted spell.
        /// </summary>
        void ClearAutoCastingSpell(bool inform = true);
        /// <summary>
        /// Checks the magic level.
        /// </summary>
        /// <param name="required">The required.</param>
        /// <returns></returns>
        bool CheckMagicLevel(int required);
        /// <summary>
        /// Check's rune requirements.
        /// Both arrays lengths must math.
        /// Sends message 'You don't have enough x runes to cast this spell.'
        /// </summary>
        /// <param name="types">Types of the runes.</param>
        /// <param name="runeAmounts">The rune amounts.</param>
        /// <returns></returns>
        bool CheckRunes(RuneType[] types, int[] runeAmounts);
        /// <summary>
        /// Set's autocasted spell.
        /// </summary>
        void SetAutoCastingSpell(ICombatSpell spell, bool inform = true);
        /// <summary>
        /// Remove's runes from character's inventory.
        /// This method doesn't throw exceptions if there's not enough runes
        /// so calling CheckRunes must be called.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <param name="runeAmounts">The rune amounts.</param>
        void RemoveRunes(RuneType[] types, int[] runeAmounts);
    }
}
