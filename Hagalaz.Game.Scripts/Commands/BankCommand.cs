using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Scripts.Widgets.Bank;

namespace Hagalaz.Game.Scripts.Commands
{
    public class BankCommand : IGameCommand
    {
        public string Name { get; } = "bank";
        public Permission Permission { get; } = Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var bankScript = args.Character.ServiceProvider.GetRequiredService<BankScreen>();
            args.Character.Widgets.OpenWidget(762, 0, bankScript, true);
            return Task.CompletedTask;
        }
    }
}