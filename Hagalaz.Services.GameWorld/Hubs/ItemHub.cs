using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs.Filters;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    [CharacterFilter]
    public class ItemHub : RaidoHub
    {
        [RaidoMessageHandler(typeof(GroundItemClickMessage))]
        public void OnGroundItemClick(GroundItemClickMessage message)
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

                var location = Location.Create(message.AbsX, message.AbsY, character.Location.Z, character.Location.Dimension);
                if (!character.Viewport.InBounds(location))
                {
                    return;
                }

                var groundItemService = character.ServiceProvider.GetRequiredService<IGroundItemService>();
                var groundItem = groundItemService.FindByLocation(location).FirstOrDefault(item => item.ItemOnGround.Id == message.Id);
                groundItem?.ItemOnGround.ItemScript.ItemClickedOnGround(message.ClickType, groundItem, message.ForceRun, character);
            }, 1));
        }
    }
}
