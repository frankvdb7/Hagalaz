using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Commands
{
    public class OpenShopCommand : IGameCommand
    {
        public string Name { get; } = "openshop";
        public Permission Permission { get; } = Permission.GameAdministrator;
        public Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var shopID = int.Parse(args.Arguments[0]);
            new OpenShopEvent(args.Character, shopID).Send();
            return Task.CompletedTask;
        }
    }
}