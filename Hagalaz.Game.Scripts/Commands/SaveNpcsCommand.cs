using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SaveNpcsCommand : IGameCommand
    {
        public string Name => "savenpcs";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.SendChatMessage("This command is currently disabled.");
            return Task.CompletedTask;
        }
    }
}
