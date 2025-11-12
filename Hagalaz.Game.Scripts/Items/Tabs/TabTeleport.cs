using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Items.Tabs
{
    /// <summary>
    ///     Contains tab teleport script.
    /// </summary>
    public class TabTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TabTeleport" /> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="tabItemId">The tab item id.</param>
        public TabTeleport(int x,int y, int z, int distance, int tabItemId)
        {
            Destination = Location.Create(x, y, z, 0);
            _distance = distance;
            _tabItemId = tabItemId;
        }

        /// <summary>
        ///     Contains the tab.
        /// </summary>
        private readonly int _tabItemId;

        /// <summary>
        ///     Contains the distance.
        /// </summary>
        private readonly int _distance;

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

            if (!caster.Area.Script.CanDoStandardTeleport(caster))
            {
                return;
            }

            var regionService = caster.ServiceProvider.GetRequiredService<IMapRegionService>();
            caster.AddState(new State(StateType.Teleporting, 2));
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
                    var xType = RandomStatic.Generator.Next(0, 1);
                    var yType = RandomStatic.Generator.Next(0, 1);
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

            caster.QueueAnimation(Animation.Create(9597));
            caster.QueueGraphic(Graphic.Create(1680));
            caster.QueueTask(new RsTask(() => caster.QueueAnimation(Animation.Create(4731)), 1));

            caster.QueueTask(new RsTask(() =>
                {
                    caster.Movement.Teleport(Teleport.Create(teleport));
                    caster.QueueAnimation(Animation.Create(-1));
                    caster.Movement.Unlock(false);
                }, 2));

            OnTeleportStarted(caster);
        }

        /// <summary>
        ///     Get's teleport distance of this spell.
        /// </summary>
        /// <value></value>
        public override int TeleportDistance => _distance;

        /// <summary>
        ///     Check's if character meet's requirements.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public override bool CanTeleport(ICharacter caster) => caster.Inventory.Contains(_tabItemId);

        /// <summary>
        ///     Called when [teleport started].
        /// </summary>
        /// <param name="caster">The caster.</param>
        public override void OnTeleportStarted(ICharacter caster)
        {
            var itemBuilder = caster.ServiceProvider.GetRequiredService<IItemBuilder>();
            caster.Inventory.Remove(itemBuilder.Create().WithId(_tabItemId).Build());
        }
    }
}