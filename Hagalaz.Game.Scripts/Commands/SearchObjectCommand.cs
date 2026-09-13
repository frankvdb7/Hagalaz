using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;

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
            var results = new List<string>();
            for (var i = 0; i < _gameObjectService.GetObjectsCount(); i++)
            {
                var def = await _gameObjectService.FindGameObjectDefinitionById(i);
                if (def != null && def.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    results.Add(i + " - " + def.Name);
                }
            }

            args.Character.QueueTask(new RsTask(() =>
            {
                foreach (var result in results)
                {
                    args.Character.SendChatMessage(result, ChatMessageType.ConsoleText);
                }
            }, 1));
        }
    }
}
