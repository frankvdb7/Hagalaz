using Hagalaz.Authorization.Constants;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Messages.Protocol;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize(Roles = Roles.SystemAdministrator + Roles.Separator + Roles.GameAdministrator + Roles.Separator + Roles.GameModerator )]
    public class AdminHub : RaidoHub
    {
        [RaidoMessageHandler(typeof(ConsoleCommandMessage))]
        public void OnCommand(ConsoleCommandMessage message) => Context.GetCharacter().EventManager.SendEvent(new ConsoleCommandEvent(Context.GetCharacter(), message.Command));
    }
}
