using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;

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
            await Task.Run(() =>
            {
                for (var i = 0; i < _itemService.GetTotalItemCount(); i++)
                {
                    var def = _itemService.FindItemDefinitionById(i);
                    if (def.Name.ToLower().Contains(name))
                    {
                        args.Character.SendChatMessage(i + " - " + def.Name, ChatMessageType.ConsoleText);
                    }
                }
            });
        }
    }
}