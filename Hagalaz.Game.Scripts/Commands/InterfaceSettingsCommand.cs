using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class InterfaceSettingsCommand : IGameCommand
    {
        public string Name => "interfacesettings";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 5
                && int.TryParse(args.Arguments[0], out var interfaceId)
                && int.TryParse(args.Arguments[1], out var childId)
                && ushort.TryParse(args.Arguments[2], out var min)
                && ushort.TryParse(args.Arguments[3], out var max)
                && int.TryParse(args.Arguments[4], out var value))
            {
                var inter = args.Character.Widgets.GetOpenWidget((short)interfaceId);
                inter?.SetOptions((short)childId, min, max, value);
            }
            return Task.CompletedTask;
        }
    }
}
