using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Items.Jewelry
{
    /// <summary>
    /// </summary>
    public class JewelryTeleport : TeleportSpellScript
    {
        /// <summary>
        /// </summary>
        public delegate bool RemoveRequirementsCallback();

        /// <summary>
        ///     Initializes a new instance of the <see cref="JewelryTeleport" /> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="teleportDistance">The teleport distance.</param>
        /// <param name="callback">The callback.</param>
        public JewelryTeleport(ILocation location, int teleportDistance, RemoveRequirementsCallback callback)
        {
            Destination = location;
            _teleportDistance = teleportDistance;
            _callback = callback;
        }

        /// <summary>
        ///     The callback.
        /// </summary>
        private readonly RemoveRequirementsCallback _callback;

        /// <summary>
        ///     The teleport distance.
        /// </summary>
        private readonly int _teleportDistance;

        /// <summary>
        ///     Perform's teleport of the caster.
        /// </summary>
        /// <param name="caster"></param>
        public override void PerformTeleport(ICharacter caster)
        {
            if (caster.HasState<TeleportingState>())
            {
                return;
            }

            // remove or replace jewelry with less charges.
            if (!_callback.Invoke())
            {
                return;
            }

            var regionService = caster.ServiceProvider.GetRequiredService<IMapRegionService>();
            caster.AddState(new State(StateType.Teleporting, TeleportDelay + 1));
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
                    {
                        curX += -xOffset;
                    }
                    else
                    {
                        curX += xOffset;
                    }

                    if (yType == 1)
                    {
                        curY += -yOffset;
                    }
                    else
                    {
                        curY += yOffset;
                    }

                    var current = Location.Create(curX, curY, Destination.Z, Destination.Dimension);
                    found = regionService.IsAccessible(current);
                    if (found)
                    {
                        teleport = current;
                    }
                }
            }

            caster.QueueAnimation(Animation.Create(9603));
            caster.QueueGraphic(Graphic.Create(1684));
            caster.QueueTask(new RsTask(() =>
                {
                    caster.Movement.Teleport(Teleport.Create(teleport));
                }, TeleportDelay));

            caster.QueueTask(new RsTask(() =>
                {
                    caster.Movement.Unlock(false);
                }, TeleportDelay + 1));
        }

        /// <summary>
        ///     Gets the delay.
        /// </summary>
        /// <value></value>
        public override int TeleportDelay => 5;

        /// <summary>
        ///     Gets the teleport distance.
        /// </summary>
        /// <value></value>
        public override int TeleportDistance => _teleportDistance;
    }
}