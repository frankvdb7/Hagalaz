using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.NPCs
{
    [NpcScriptMetaData([2606])]
    public class TzHaarXil : RangedNpcScriptBase
    {
        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="delay">The delay.</param>
        public override void RenderProjectile(ICreature target, int delay)
        {
            var projectile = new Projectile(442);
            projectile.SetSenderData(Owner, 50, false);
            projectile.SetReceiverData(target, 35);
            projectile.SetFlyingProperties(25, delay, 10, 0, false);
            projectile.Display();
        }
    }
}