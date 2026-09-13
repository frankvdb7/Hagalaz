using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SearchItemCommand : IGameCommand
    {
        private readonly IItemService _itemService;
        public string Name { get; } = "searchitem";
        public Permission Permission { get; } = Permission.GameAdministrator;

        public SearchItemCommand(IItemService itemService) => _itemService = itemService;

        public async Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var name = string.Join(" ", args.Arguments).ToLower();
            var results = await Task.Run(() =>
            {
                var matches = new List<string>();
                for (var i = 0; i < _itemService.GetTotalItemCount(); i++)
                {
                    var def = _itemService.FindItemDefinitionById(i);
                    if (def.Name.ToLower().Contains(name))
                    {
                        matches.Add(i + " - " + def.Name);
                    }
                }

                return matches;
            });

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
