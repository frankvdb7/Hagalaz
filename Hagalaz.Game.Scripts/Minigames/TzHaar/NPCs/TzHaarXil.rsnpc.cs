using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.NPCs
{
    [NpcScriptMetaData([2606])]
    public class TzHaarXil : RangedNpcScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public TzHaarXil(IProjectileBuilder projectileBuilder) => _projectileBuilder = projectileBuilder;

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="delay">The delay.</param>
        public override void RenderProjectile(ICreature target, int delay) =>
            _projectileBuilder.Create()
                .WithGraphicId(442)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithFromHeight(50)
                .WithToHeight(35)
                .WithDelay(25)
                .WithSlope(10)
                .Send();
    }
}