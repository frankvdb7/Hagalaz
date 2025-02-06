using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <summary>
    ///     An abstract class that contains standard body guard methods.
    /// </summary>
    public abstract class BodyGuard : NpcScriptBase
    {
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
        ///     Respawns this npc.
        ///     By default, this unregisters the NPC if the CanSpawn method returns false.
        ///     Otherwise, this calls the Respawn method of the NPC.
        /// </summary>
        public override void Respawn()
        {
            if (GodwarsManager.CanBodyGuardRespawn(Owner))
            {
                Owner.Respawn();
            }

            // else nothing... wait for the general to respawn.
        }

        /// <summary>
        ///     Get's called when npc is created.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnCreate() => GodwarsManager.AddBodyGuard(Owner);

        /// <summary>
        ///     Get's called when npc is destroyed permanently.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDestroy() => GodwarsManager.RemoveBodyGuard(Owner);

        /// <summary>
        ///     Determines whether this instance can spawn.
        ///     If false, then the npc will be unregistered from the world.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can spawn; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanSpawn() => GodwarsManager.CanBodyGuardRespawn(Owner);
    }
}