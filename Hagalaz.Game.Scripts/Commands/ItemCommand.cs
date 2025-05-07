using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ItemCommand : IGameCommand
    {
        private readonly IItemBuilder _itemBuilder;
        public string Name => "item";
        public Permission Permission => Permission.GameAdministrator;

        public ItemCommand(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

        public Task Execute(GameCommandArgs args)
        {
            var cmd = args.Arguments;
            var id = int.Parse(cmd[1]);
            var amount = int.Parse(cmd[2]);

            if (id >= 0 && amount > 0)
            {
                args.Character.Inventory.Add(_itemBuilder.Create().WithId(id).WithCount(amount).Build());
            }

            return Task.CompletedTask;
        }
    }
}