using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    /// </summary>
    public class HoldSpell : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HoldSpell" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public HoldSpell(CombatSpellDto dto)
            : base(dto)
        {
        }

        /// <summary>
        ///     Called when [succesfull attack].
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        public override void OnSuccessfulHit(ICharacter caster, ICreature victim)
        {
            var ticks = 8; // 5 seconds
            if (Dto.ButtonId == 55)
            {
                ticks = 17; // 10 seconds
            }
            else if (Dto.ButtonId == 81)
            {
                ticks = 25; // 15 seconds
            }

            victim.Freeze(ticks, ticks / 2);
        }
    }
}