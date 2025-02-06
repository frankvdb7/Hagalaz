using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Model;

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
            : base(dto)
        {
        }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="victim"></param>
        /// <param name="delay">The delay.</param>
        public override void RenderProjectile(ICharacter caster, ICreature victim, byte delay)
        {
            var projectile = new Projectile(2735);
            projectile.SetSenderData(caster, 43, false);
            projectile.SetReceiverData(victim, 50);
            projectile.SetFlyingProperties(51, (byte)(delay - 51), 3, 64, false);
            projectile.Display();

            var projectile2 = new Projectile(2736);
            projectile2.SetSenderData(caster, 43, false);
            projectile2.SetReceiverData(victim, 50);
            projectile2.SetFlyingProperties(51, (byte)(delay - 51), 20, 64, false);
            projectile2.Display();

            var projectile3 = new Projectile(2736);
            projectile3.SetSenderData(caster, 43, false);
            projectile3.SetReceiverData(victim, 50);
            projectile3.SetFlyingProperties(51, (byte)(delay - 51), 110, 64, false);
            projectile3.Display();
        }
    }
}