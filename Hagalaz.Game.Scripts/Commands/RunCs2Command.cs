using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class RunCs2Command : IGameCommand
    {
        public string Name => "runcs2";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2)
            {
                var types = args.Arguments[0].ToCharArray();
                var scriptId = int.Parse(args.Arguments[1]);
                var p = new object[args.Arguments.Length - 2];
                var t = 0;
                for (var i = 2; i < args.Arguments.Length; i++)
                {
                    if (types[t++] == 's')
                    {
                        p[i - 2] = args.Arguments[i];
                    }
                    else
                    {
                        p[i - 2] = int.Parse(args.Arguments[i]);
                    }
                }
                args.Character.Configurations.SendCs2Script(scriptId, p);
            }
            return Task.CompletedTask;
        }
    }
}
