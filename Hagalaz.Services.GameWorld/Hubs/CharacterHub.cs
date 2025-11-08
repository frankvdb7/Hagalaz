using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Hubs.Filters;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    [CharacterFilter]
    public class CharacterHub : RaidoHub
    {
        private readonly ICharacterService _characterService;
        private readonly IAuthenticationService _authenticationService;

        public CharacterHub(ICharacterService characterService, IAuthenticationService authenticationService)
        {
            _characterService = characterService;
            _authenticationService = authenticationService;
        }

        [RaidoMessageHandler(typeof(CharacterClickMessage))]
        public async Task OnCharacterClick(CharacterClickMessage message)
        {
            if (message.Index < 0 || message.Index >= short.MaxValue)
            {
                return;
            }
            var target = await _characterService.FindByIndex(message.Index);
            if (target == null)
            {
                return;
            }
            var character = Context.GetCharacter();
            if (!character.Viewport.VisibleCreatures.Contains(target))
            {
                return;
            }
            character.OnCharacterClicked(message.ClickType, message.ForceRun, target);
        }

        [RaidoMessageHandler(typeof(PublicChatMessage))]
        public void OnPublicChat(PublicChatMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.EventManager.SendEvent(new ChatAllowEvent(character, message.Text)))
            {
                return;
            }
            switch (character.CurrentChatType)
            {
                // TODO - other chat types
                case ClientChatType.Normal:
                default:
                    var publicChatMessage = new PublicChatMessage
                    {
                        CharacterIndex = character.Index,
                        Permissions = character.Permissions,
                        Text = message.Text,
                        TextAnimation = message.TextAnimation,
                        TextColor = message.TextColor
                    };
                    character.Viewport.VisibleCreatures.OfType<ICharacter>().ForEach(c => c.Session.SendMessage(publicChatMessage));
                    break;
            }
        }

        [RaidoMessageHandler(typeof(MovementMessage))]
        public void OnMovement(MovementMessage message)
        {
            if (message.AbsX < 0 || message.AbsY < 0)
            {
                return;
            }

            var character = Context.GetCharacter();
            var target = Location.Create(message.AbsX, message.AbsY, character.Location.Z, character.Location.Dimension);
            var delta = Location.GetDelta(character.Location, target);
            if (delta.X > 100 || delta.X < -100 || delta.Y > 100 || delta.Y < -100)
            {
                return;
            }

            if (character.EventManager.SendEvent(new WalkAllowEvent(character, target, message.ForceRun, false)))
            {
                character.Interrupt(this);
                character.Movement.MovementType = message.ForceRun ? MovementType.Run : character.Movement.MovementType;
                var task = new LocationReachTask(character,
                    target,
                    success =>
                    {
                        if (!success)
                        {
                            character.SendChatMessage(GameStrings.YouCantReachThat);
                        }
                    });
                character.QueueTask(task);
            }
        }

        [RaidoMessageHandler(typeof(MusicPlayedMessage))]
        public void OnMusicPlayed(MusicPlayedMessage message)
        {
            var character = Context.GetCharacter();
            character.Music.OnMusicPlayed(message.MusicId);
        }

        [RaidoMessageHandler(typeof(SetClientChatTypeMessage))]
        public void SetClientChatType(SetClientChatTypeMessage message)
        {
            var character = Context.GetCharacter();
            character.CurrentChatType = message.Type;
        }
    }
}