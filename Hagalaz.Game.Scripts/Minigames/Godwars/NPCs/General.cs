using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <summary>
    ///     An abstract class that contains standard general methods.
    /// </summary>
    public abstract class General : NpcScriptBase
    {
        /// <summary>
        ///     Get's called when npc is destroyed permanently.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDestroy() => GodwarsManager.SetGeneral(Owner, true);

        /// <summary>
        ///     Get's called when npc is spawned.
        /// </summary>
        public override void OnSpawn()
        {
            GodwarsManager.SetGeneral(Owner);
            foreach (var npc in GodwarsManager.GetBodyGuards(Owner))
            {
                if (npc.Combat.IsDead)
                {
                    npc.Respawn();
                }
            }
        }

        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if character is character.
        ///     This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature)
        {
            if (creature.IsDestroyed)
            {
                return false;
            }

            if (Owner.Area == creature.Area)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get's if this npc can be poisoned.
        ///     By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can poison; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanPoison() => false;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() => Owner.AddState(new State(StateType.VengeanceImmunity, int.MaxValue));
    }
}