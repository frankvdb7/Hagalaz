using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    /// </summary>
    public class TeleBlock : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TeleBlock" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public TeleBlock(CombatSpellDto dto)
            : base(dto)
        {
        }

        /// <summary>
        ///     Determines whether this instance can attack the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <returns></returns>
        public override bool CanAttack(ICharacter caster, ICreature victim)
        {
            if (victim is INpc)
            {
                caster.SendChatMessage("This spell can only be casted on other players.");
                return false;
            }

            if (!victim.HasState(StateType.TeleBlocked))
            {
                return base.CanAttack(caster, victim);
            }

            caster.SendChatMessage("This player is already effected by this spell.");
            return false;

        }

        /// <summary>
        ///     Called when [succesfull attack].
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        public override void OnSuccessfulHit(ICharacter caster, ICreature victim)
        {
            if (!(victim is ICharacter target))
            {
                return;
            }

            var ticks = 500; // 5 minutes
            if (target.Prayers.IsPraying(NormalPrayer.ProtectFromMagic) || target.Prayers.IsPraying(AncientCurses.DeflectMagic))
            {
                ticks = 250; // 2:30 minutes
            }

            target.AddState(new State(StateType.TeleBlocked, ticks));
        }
    }
}