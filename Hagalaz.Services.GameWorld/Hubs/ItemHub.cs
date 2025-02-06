using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs.Filters;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    [CharacterFilter]
    public class ItemHub : RaidoHub
    {
        private readonly IGroundItemService _groundItemService;

        public ItemHub(IGroundItemService groundItemService)
        {
            _groundItemService = groundItemService;
        }

        [RaidoMessageHandler(typeof(GroundItemClickMessage))]
        public async Task OnGroundItemClick(GroundItemClickMessage message)
        {
            await Task.CompletedTask;
            if (message.Id < 0)
            {
                return;
            }
            var character = Context.GetCharacter();
            var location = Location.Create(message.AbsX, message.AbsY, character.Location.Z, character.Location.Dimension);
            if (!character.Viewport.InBounds(location))
            {
                return;
            }
            var groundItem = _groundItemService.FindByLocation(location).FirstOrDefault(item => item.ItemOnGround.Id == message.Id);
            if (groundItem == null)
            {
                return;
            }
            groundItem.ItemOnGround.ItemScript.ItemClickedOnGround(message.ClickType, groundItem, message.ForceRun, character);
        }
    }
}
