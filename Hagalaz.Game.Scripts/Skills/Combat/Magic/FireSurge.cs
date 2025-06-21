using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    /// </summary>
    public class FireSurge : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FireSurge" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public FireSurge(CombatSpellDto dto)
            : base(dto) { }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="victim"></param>
        /// <param name="delay">The delay.</param>
        public override void RenderProjectile(ICharacter caster, ICreature victim, int delay)
        {
            var projectileBuilder = caster.ServiceProvider.GetRequiredService<IProjectileBuilder>();

            projectileBuilder.Create()
                .WithGraphicId(2735)
                .FromCreature(caster)
                .ToCreature(victim)
                .WithDuration(delay - 51)
                .WithFromHeight(43)
                .WithToHeight(50)
                .WithDelay(51)
                .WithSlope(3)
                .WithAngle(64)
                .Send();

            projectileBuilder.Create()
                .WithGraphicId(2736)
                .FromCreature(caster)
                .ToCreature(victim)
                .WithDuration(delay - 51)
                .WithFromHeight(43)
                .WithToHeight(50)
                .WithDelay(51)
                .WithSlope(20)
                .WithAngle(64)
                .Send();

            projectileBuilder.Create()
                .WithGraphicId(2736)
                .FromCreature(caster)
                .ToCreature(victim)
                .WithDuration(delay - 51)
                .WithFromHeight(43)
                .WithToHeight(50)
                .WithDelay(51)
                .WithSlope(110)
                .WithAngle(64)
                .Send();
        }
    }
}