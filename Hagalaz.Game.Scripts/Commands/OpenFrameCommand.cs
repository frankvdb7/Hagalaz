using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class OpenFrameCommand : IGameCommand
    {
        private readonly IWidgetBuilder _widgetBuilder;
        public string Name { get; } = "openframe";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public OpenFrameCommand(IWidgetBuilder widgetBuilder) => _widgetBuilder = widgetBuilder;

        public async Task Execute(GameCommandArgs args)
        {
            await Task.CompletedTask;
            args.Handled = true;
            var frameID = int.Parse(args.Arguments[1]);
            args.Character.Widgets.CloseAll();
            var frame = _widgetBuilder
                .Create()
                .ForCharacter(args.Character)
                .WithId(frameID)
                .AsFrame()
                .Build();
            args.Character.Widgets.OpenFrame(frame);
        }
    }
}