using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.CharacterDesign
{
    /// <summary>
    ///     Represents account creation script.
    /// </summary>
    public class DesignFrame : WidgetScript
    {
        private readonly IWidgetBuilder _widgetBuilder;

        public DesignFrame(ICharacterContextAccessor characterContextAccessor, IWidgetBuilder widgetBuilder) : base(characterContextAccessor)
        {
            _widgetBuilder = widgetBuilder;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            var widget = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId((int)InterfaceIds.AccountDesignInterface)
                .WithTransparency(1)
                .WithParentId(InterfaceInstance.Id)
                .WithParentSlot(1)
                .WithScript<DesignInterface>()
                .Build();
            Owner.Widgets.OpenWidget(widget);
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.GetScript<WidgetsCharacterScript>()?.OpenMainGameFrame();
    }
}