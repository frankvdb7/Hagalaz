using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Wilderness.GameObjects
{
    [GameObjectScriptMetaData([65616, 65617, 65618, 65619, 65620, 65621])]
    public class Obelisk : GameObjectScript
    {
        private readonly IMapRegionService _regionService;
        private readonly IRsTaskService _taskService;
        private readonly IRegionUpdateBuilder _regionUpdateBuilder;
        private readonly IGameObjectService _gameObjectService;

        /// <summary>
        ///     The actived obelisks.
        /// </summary>
        private static readonly bool[] _activated = new bool[6];

        /// <summary>
        ///     The obelisks centers
        /// </summary>
        private static readonly ILocation[] _obelisksCenters =
        [
            Location.Create(2978, 3864, 0), Location.Create(3033, 3730, 0), Location.Create(3104, 3792, 0), Location.Create(3154, 3618, 0), Location.Create(3217, 3654, 0), Location.Create(3305, 3914, 0)
        ];

        public Obelisk(IMapRegionService regionService, IRsTaskService taskService, IRegionUpdateBuilder regionUpdateBuilder, IGameObjectService gameObjectService)
        {
            _regionService = regionService;
            _taskService = taskService;
            _regionUpdateBuilder = regionUpdateBuilder;
            _gameObjectService = gameObjectService;
        }

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var index = Owner.Id - 65616;
                if (_activated[index])
                {
                    clicker.SendChatMessage("The obelisk is already active.");
                    return;
                }

                Activate(index);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Activates this instance.
        /// </summary>
        private void Activate(int index)
        {
            _activated[index] = true;
            var center = _obelisksCenters[index];
            var region = _regionService.GetOrCreateMapRegion(center.RegionId, center.Dimension, true);
            var centerObj = region.FindGameObjects(center.RegionLocalX, center.RegionLocalY, center.Z).FirstOrDefault(g => g.Id == Owner.Id);
            var pillar1 = region.FindGameObjects(center.RegionLocalX + 4, center.RegionLocalY, center.Z).FirstOrDefault(g => g.Id == Owner.Id);
            var pillar2 = region.FindGameObjects(center.RegionLocalX, center.RegionLocalY + 4, center.Z).FirstOrDefault(g => g.Id == Owner.Id);
            var pillar3 = region.FindGameObjects(center.RegionLocalX + 4, center.RegionLocalY + 4, center.Z).FirstOrDefault(g => g.Id == Owner.Id);
            if (centerObj != null)
            {
                _gameObjectService.AnimateGameObject(centerObj, Animation.Create(2226));
            }

            if (pillar1 != null)
            {
                _gameObjectService.AnimateGameObject(pillar1, Animation.Create(2226));
            }

            if (pillar2 != null)
            {
                _gameObjectService.AnimateGameObject(pillar2, Animation.Create(2226));
            }

            if (pillar3 != null)
            {
                _gameObjectService.AnimateGameObject(pillar3, Animation.Create(2226));
            }

            _taskService.Schedule(new RsTask(() =>
            {
                for (var x = 1; x < 4; x++)
                {
                    for (var y = 1; y < 4; y++)
                    {
                        var loc = center.Translate(x, y, 0);
                        var locRegion = _regionService.GetOrCreateMapRegion(loc.RegionId, loc.Dimension, true);
                        var update = _regionUpdateBuilder.Create().WithLocation(loc).WithGraphic(Graphic.Create(661)).Build();
                        locRegion.QueueUpdate(update);
                    }
                }

                var newIndex = RandomStatic.Generator.Next(0, _obelisksCenters.Length);
                if (index == newIndex)
                {
                    newIndex++;
                }

                if (newIndex > _obelisksCenters.Length)
                {
                    newIndex = 0;
                }

                var newCenter = _obelisksCenters[newIndex];
                foreach (var character in region.FindAllCharacters().Where(c => c.Location.WithinDistance(center, 3)))
                {
                    var src = character.Location;
                    var dest = newCenter.Translate(src.X - center.X, src.Y - center.Y, 0);
                    new ObeliskTeleport
                    {
                        Destination = dest
                    }.PerformTeleport(character);
                }

                _activated[index] = false;
            }, 8));
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}