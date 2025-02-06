using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.NPCs
{
    [NpcScriptMetaData([2591, 2592, 2594, 2595, 2596, 2597])]
    public class TzHaarMej : MagicNpcScriptBase
    {
        /// <summary>
        ///     Gets the magic damage.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override int GetMagicDamage(ICreature target) => ((INpcCombat)Owner.Combat).GetMagicDamage(target, 146);

        /// <summary>
        ///     Gets the magic maximum hit.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override int GetMagicMaxHit(ICreature target) => ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 146);

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}