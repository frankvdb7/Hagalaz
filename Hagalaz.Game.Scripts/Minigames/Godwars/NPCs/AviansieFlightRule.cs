using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <inheritdoc />
    public sealed class AviansieFlightRule : IAviansieFlightRule
    {
        /// <inheritdoc />
        public bool TryDenyMelee(INpc owner, ICreature attacker, bool canRetaliate)
        {
            if (attacker.Combat.GetAttackStyle() is not (AttackStyle.MeleeAccurate or AttackStyle.MeleeAggressive
                or AttackStyle.MeleeControlled or AttackStyle.MeleeDefensive))
            {
                return false;
            }

            if (attacker is ICharacter character)
            {
                character.SendChatMessage("The aviansie is flying too high for you to attack using melee.");
            }

            if (canRetaliate)
            {
                var attackerHandle = attacker.Handle;
                owner.QueueTask(new RsTask(() => owner.Combat.SetTarget(attackerHandle), 1));
            }

            return true;
        }
    }
}
