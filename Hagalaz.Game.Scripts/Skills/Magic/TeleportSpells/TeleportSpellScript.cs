using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    /// Contains teleport spell.
    /// </summary>
    public class TeleportSpellScript : ITeleportSpellScript
    {
        /// <summary>
        /// Gets or sets the magic book.
        /// </summary>
        public MagicBook Book { get; protected init; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public ILocation Destination { get; init; }

        /// <summary>
        /// Perform's teleport of the caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        public virtual void PerformTeleport(ICharacter caster)
        {
            if (!CanPerformTeleport(caster)) return;
            var regionService = caster.ServiceProvider.GetRequiredService<IMapRegionService>();
            caster.AddState(new TeleportingState { TicksLeft = TeleportDelay + 1 });
            caster.Interrupt(this);
            caster.Movement.Lock(true);
            var teleport = Destination.Clone();
            if (TeleportDistance > 0)
            {
                var curX = teleport.X;
                var curY = teleport.Y;
                var found = false;
                for (var i = 0; !found && i < 10; i++)
                {
                    var xType = RandomStatic.Generator.Next(0, 2);
                    var yType = RandomStatic.Generator.Next(0, 2);
                    var xOffset = RandomStatic.Generator.Next(0, TeleportDistance);
                    var yOffset = RandomStatic.Generator.Next(0, TeleportDistance);
                    if (xType == 1)
                        curX += -xOffset;
                    else
                        curX += xOffset;
                    if (yType == 1)
                        curY += -yOffset;
                    else
                        curY += yOffset;
                    var current = Location.Create(curX, curY, Destination.Z, Destination.Dimension);
                    found = regionService.IsAccessible(current);
                    if (found) teleport = current;
                }
            }

            var anims = Animations;
            caster.QueueAnimation(Animation.Create(anims[0]));
            var graphics = Graphics;
            caster.QueueGraphic(Graphic.Create(graphics[0]));
            caster.QueueTask(new RsTask(() => caster.Movement.Teleport(Teleport.Create(teleport)), TeleportDelay));

            caster.QueueTask(new RsTask(() =>
                {
                    caster.QueueAnimation(Animation.Create(anims[1]));
                    caster.QueueGraphic(Graphic.Create(graphics[1]));
                    caster.Statistics.AddExperience(StatisticsConstants.Magic, MagicExperience);
                    caster.Movement.Unlock(false);
                    OnTeleportFinished();
                },
                TeleportDelay + 1));

            OnTeleportStarted(caster);
        }

        /// <summary>
        /// Determines whether this instance [can perform teleport] the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public virtual bool CanPerformTeleport(ICharacter caster)
        {
            if (caster.HasState<TeleportingState>()) return false;
            if (!caster.Area.Script.CanDoStandardTeleport(caster)) return false;
            if (!caster.EventManager.SendEvent(new TeleportAllowEvent(caster, Destination))) return false;
            return true;
        }

        /// <summary>
        /// Gets the teleport delay.
        /// </summary>
        /// <value></value>
        public virtual int TeleportDelay => 4;

        /// <summary>
        /// Gets the animations.
        /// </summary>
        /// <value></value>
        public virtual int[] Animations =>
            Book switch
            {
                MagicBook.StandardBook => [8939, 8941],
                MagicBook.AncientBook => [9599, -1],
                MagicBook.LunarBook => [9606, -1],
                _ => [-1, -1]
            };

        /// <summary>
        /// Gets the graphics.
        /// </summary>
        /// <value></value>
        public virtual int[] Graphics =>
            Book switch
            {
                MagicBook.StandardBook => [1576, 1577],
                MagicBook.AncientBook => [1681, -1],
                MagicBook.LunarBook => [1685, 1685],
                _ => [-1, -1]
            };

        /// <summary>
        /// Gets the magic experience.
        /// </summary>
        /// <value></value>
        public virtual double MagicExperience => 0.0;

        /// <summary>
        /// Gets the teleport distance.
        /// </summary>
        /// <value></value>
        public virtual int TeleportDistance => 0;

        /// <summary>
        /// Determines whether this instance can teleport the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public virtual bool CanTeleport(ICharacter caster) => true;

        /// <summary>
        /// Called when [teleport started].
        /// </summary>
        /// <param name="caster">The caster.</param>
        public virtual void OnTeleportStarted(ICharacter caster) { }

        /// <summary>
        /// Called when [teleport finished].
        /// </summary>
        public virtual void OnTeleportFinished() { }
    }
}