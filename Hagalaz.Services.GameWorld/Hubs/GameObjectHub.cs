using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs.Filters;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    [CharacterFilter]
    public class GameObjectHub : RaidoHub
    {
        private readonly IGameObjectService _gameObjectService;
        private readonly ILocationBuilder _locationBuilder;

        public GameObjectHub(IGameObjectService gameObjectService, ILocationBuilder locationBuilder)
        {
            _gameObjectService = gameObjectService;
            _locationBuilder = locationBuilder;
        }

        [RaidoMessageHandler(typeof(GameObjectClickMessage))]
        public void OnGameObjectClick(GameObjectClickMessage message)
        {
            if (message.Id < 0)
            {
                return;
            }
            var character = Context.GetCharacter();
            character.QueueTask(new RsTask(() =>
            {
                if (character.IsDestroyed)
                {
                    return;
                }

                var location = _locationBuilder.Create()
                    .WithX(message.AbsX)
                    .WithY(message.AbsY)
                    .WithZ(character.Location.Z)
                    .WithDimension(character.Location.Dimension)
                    .Build();
                if (!character.Viewport.InBounds(location))
                {
                    return;
                }

                var gameObject = _gameObjectService.FindByLocation(location).FirstOrDefault(obj => obj.Id == message.Id);
                gameObject?.Script.OnCharacterClick(character, message.ClickType, message.ForceRun);
            }, 1));
        }
    }
}
