using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class TargetReachBase
    {
        /// <summary>
        /// Follows the specified creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public virtual bool Follow(ICreature creature, ICreature target)
        {
            if (!creature.Viewport.VisibleCreatures.Contains(target))
                return false;
            //var interaction = GetInteraction(creature, target);
            //if (interaction == Interaction.Still)
            //{
            //    creature.Movement.ClearQueue();
            //    return true;
            //}

            return true;
        }

        /// <summary>
        /// Gets the interaction.
        /// </summary>
        /// <returns></returns>
        //public abstract Interaction GetInteraction(ICreature creature, ICreature target);
    }
}