using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;
using System;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <summary>
    ///     The Armadyl aviansie rule: they fly too high to be hit by melee.
    /// </summary>
    internal static class AviansieFlight
    {
        /// <summary>
        ///     Denies a melee attacker: tells characters why and, when allowed, makes the aviansie retaliate next tick.
        /// </summary>
        /// <returns><c>true</c> if the attacker used melee and was denied; <c>false</c> if normal attack rules apply.</returns>
        public static bool DeniesMelee(INpc owner, ICreature attacker, Func<ICreature, bool> canRetaliateTo)
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

            if (canRetaliateTo(attacker))
            {
                var attackerHandle = attacker.Handle;
                owner.QueueTask(new RsTask(() => owner.Combat.SetTarget(attackerHandle), 1));
            }

            return true;
        }
    }
}
