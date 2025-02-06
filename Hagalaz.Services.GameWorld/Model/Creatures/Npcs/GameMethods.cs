using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Contains game methods for npc.
    /// </summary>
    public partial class Npc : Creature
    {
        /// <summary>
        /// Get's called when npc is spawned.
        /// </summary>
        public override void OnSpawn()
        {
            new CreatureSpawnedEvent(this).Send();
            Script.OnSpawn();
            Combat.OnSpawn();
        }

        /// <summary>
        /// Get's called when npc dies.
        /// </summary>
        public override void OnDeath()
        {
            if (!Combat.IsDead)
            {
                new CreatureDiedEvent(this).Send();
                Movement.Lock(true); // reset movement and prevent further actions
                Script.OnDeath();
                Combat.OnDeath();
            }
        }

        /// <summary>
        /// Happens when npc location changes.
        /// </summary>
        /// <param name="oldLocation">Old location.</param>
        protected override void OnLocationChange(ILocation? oldLocation) => new CreatureLocationChangedEvent(this, oldLocation).Send();

        /// <summary>
        /// This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnTargetKilled(ICreature target)
        {
            new CreatureKillEvent(this, target).Send();
            Script.OnTargetKilled(target);
            Combat.OnTargetKilled(target);
        }

        /// <summary>
        /// Get's called when killed by a creature.
        /// </summary>
        /// <param name="killer">The creature.</param>
        public override void OnKilledBy(ICreature killer)
        {
            new CreatureKillEvent(killer, this).Send();
            Script.OnKilledBy(killer);
            Combat.OnKilledBy(killer);
        }

        /// <summary>
        /// Respawn's this npc.
        /// </summary>
        public override void Respawn()
        {
            Statistics.Normalise();
            Movement.Teleport(Teleport.Create(Bounds.DefaultLocation.Clone()));
            Movement.Unlock(true);
            Appearance.Visible = true;
            OnSpawn();
        }

        /// <summary>
        /// Interrupts current jobs.
        /// </summary>
        /// <param name="source">Object which performed the interruption,
        /// this parameter can be null , but it is not recomended to do so.
        /// Best use would be to set the invoker class instance ('this') or any other related object
        /// if invoker is static as source.</param>
        /// <returns>If something was interupted.</returns>
        public override void Interrupt(object source)
        {
            new CreatureInterruptedEvent(this, source).Send();
            if (!(source is CreatureCombat))
                Combat.CancelTarget();
            Script.OnInterrupt(source);
        }

        /// <summary>
        /// Poison's this npc by given amount.
        /// If amount is lower than 10 the character is then unpoisoned.
        /// This method does have no effect if this character is not poisonable or does have
        /// ResistPoison state.
        /// </summary>
        /// <param name="amount">Amount of poison strength.</param>
        public override bool Poison(short amount)
        {
            if (HasState(StateType.ResistPoison) && !Script.CanPoison())
                return false;
            Statistics.SetPoisonAmount(amount);
            return true;
        }
    }
}