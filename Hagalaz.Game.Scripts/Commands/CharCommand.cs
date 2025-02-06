using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Scripts.Characters;

namespace Hagalaz.Game.Scripts.Commands
{
    public class CharCommand : IGameCommand
    {
        public string Name { get; } = "char";
        public Permission Permission { get; } = Permission.Standard;

        public Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            args.Character.GetScript<WidgetsCharacterScript>()?.OpenMainGameFrame();
            return Task.CompletedTask;
        }
    }
}