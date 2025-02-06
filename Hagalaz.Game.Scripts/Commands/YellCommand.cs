using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class YellCommand : IGameCommand
    {
        private readonly IGameMessageService _gameMessaging;
        public string Name { get; } = "yell";
        public Permission Permission { get; } = Permission.GameModerator;

        public YellCommand(IGameMessageService gameMessaging) => _gameMessaging = gameMessaging;

        public async Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var character = args.Character;
            var name = character.DisplayName;
            var rights = character.Permissions.ToClientRights();
            if (rights == 1)
            {
                name = "<img=0>" + name;
            }
            else if (rights >= 2)
            {
                name = "<img=1>" + name;
            }

            var text = $"[<col=009EFF>{character.Permissions.ToRightsTitle()}</col>] {name}: {string.Join(' ', args.Arguments).FormatUserMessageForPacket()}";
            await _gameMessaging.MessageAsync(text, GameMessageType.Game, name);
        }
    }
}