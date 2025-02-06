using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Scripts.Commands
{
    public class OpenInterfaceCommand : IGameCommand
    {
        private readonly IWidgetBuilder _widgetBuilder;
        public string Name { get; } = "openinterface";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public OpenInterfaceCommand(IWidgetBuilder widgetBuilder)
        {
            _widgetBuilder = widgetBuilder;
        }

        public async Task Execute(GameCommandArgs args)
        {
            await Task.CompletedTask;
            args.Handled = true;
            var id = int.Parse(args.Arguments[0]);
            var slot = (int)(args.Character.GameClient.IsScreenFixed ? InterfaceSlots.FixedMainInterfaceSlot : InterfaceSlots.ResizedMainInterfaceSlot);
            if (args.Arguments.Length > 2)
                slot = int.Parse(args.Arguments[1]);
            var transparency = 0;
            if (args.Arguments.Length > 3)
                transparency = int.Parse(args.Arguments[2]);
            var widget = _widgetBuilder
                .Create()
                .ForCharacter(args.Character)
                .WithId(id)
                .WithParentId(args.Character.Widgets.CurrentFrame.Id)
                .WithParentSlot(slot)
                .WithTransparency(transparency)
                .Build();
            args.Character.Widgets.OpenWidget(widget);
        }
    }
}