using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Model;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    /// </summary>
    public class LeverTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LeverTeleport" /> class.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="teleportDistance">The teleport distance.</param>
        public LeverTeleport(IGameObject obj, int x, int y, int z, short teleportDistance)
        {
            _obj = obj;
            Destination = Location.Create(x, y, z, 0);
            _teleportDistance = teleportDistance;
        }

        /// <summary>
        ///     The obj.
        /// </summary>
        private readonly IGameObject _obj;

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
            if (caster.HasState(StateType.Teleporting))
            {
                return;
            }

            if (!caster.Area.Script.CanDoGameObjectTeleport(caster, _obj))
            {
                return;
            }

            if (!caster.EventManager.SendEvent(new TeleportAllowEvent(caster, Destination)))
            {
                return;
            }

            var regionService = caster.ServiceProvider.GetRequiredService<IMapRegionService>();

            caster.QueueAnimation(Animation.Create(2140));
            caster.QueueTask(new RsTask(() =>
                {
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

                    caster.QueueAnimation(Animation.Create(8939));
                    caster.QueueGraphic(Graphic.Create(1576));
                    caster.QueueTask(new RsTask(() => caster.Movement.Teleport(Teleport.Create(teleport)), TeleportDelay));

                    caster.QueueTask(new RsTask(() =>
                        {
                            caster.QueueAnimation(Animation.Create(8941));
                            caster.QueueGraphic(Graphic.Create(1577));
                            caster.Statistics.AddExperience(StatisticsConstants.Magic, MagicExperience);
                            caster.Movement.Unlock(false);
                        }, TeleportDelay + 1));
                }, 1));
        }

        /// <summary>
        ///     Gets the teleport distance.
        /// </summary>
        /// <value></value>
        public override int TeleportDistance => _teleportDistance;
    }
}