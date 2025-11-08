using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SearchObjectCommand : IGameCommand
    {
        private readonly IGameObjectService _gameObjectService;
        public string Name { get; } = "searchobject";
        public Permission Permission { get; } = Permission.GameAdministrator;

        public SearchObjectCommand(IGameObjectService gameObjectService) => _gameObjectService = gameObjectService;

        public async Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var name = string.Join(" ", args.Arguments).ToLower();
            await Task.Run(async () =>
            {
                for (var i = 0; i < _gameObjectService.GetObjectsCount(); i++)
                {
                    var def = await _gameObjectService.FindGameObjectDefinitionById(i);
                    if (def != null && def.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        args.Character.SendChatMessage(i + " - " + def.Name, ChatMessageType.ConsoleText);
                    }
                }
            });
        }
    }
}