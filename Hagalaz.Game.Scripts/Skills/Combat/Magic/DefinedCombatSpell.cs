using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     A combat spell whose level, runes, experience and autocast config come from its persisted <see cref="CombatSpellDto" />.
    ///     Subclasses supply the attack behavior.
    /// </summary>
    public abstract class DefinedCombatSpell : ICombatSpell
    {
        protected DefinedCombatSpell(CombatSpellDto dto) => Dto = dto;

        /// <summary>
        ///     The persisted definition of this spell.
        /// </summary>
        public CombatSpellDto Dto { get; }

        public abstract void PerformAttack(ICharacter caster, ICreature victim);

        public virtual bool CanAttack(ICharacter caster, ICreature victim) => true;

        public virtual bool CheckRequirements(ICharacter caster) =>
            caster.Magic.CheckMagicLevel(Dto.RequiredLevel) && caster.Magic.CheckRunes(Dto.RequiredRunes, Dto.RequiredRunesCounts);

        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(Dto.RequiredRunes, Dto.RequiredRunesCounts);

        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, Dto.AutoCastConfig);

        public void OnAutoCastingDeactivation(ICharacter deactivatedOn) => deactivatedOn.Configurations.SendStandardConfiguration(108, -1);

        public virtual int GetCastingSpeed(ICharacter caster) => 5;

        public int GetCombatDistance(ICharacter caster) => 8;

        public double GetMagicExperience() => Dto.BaseExperience;
    }
}
