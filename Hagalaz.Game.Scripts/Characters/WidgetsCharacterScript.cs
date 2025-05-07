using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Widgets;
using Hagalaz.Game.Scripts.Widgets.CharacterDesign;

namespace Hagalaz.Game.Scripts.Characters
{
    public class WidgetsCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IWidgetBuilder _widgetBuilder;

        public WidgetsCharacterScript(ICharacterContextAccessor contextAccessor, IWidgetBuilder widgetBuilder) : base(contextAccessor) => _widgetBuilder = widgetBuilder;

        protected override void Initialize()
        {
        }

        public override void OnRegistered()
        {
            Character.RegisterEventHandler<ScreenChangedEvent>(@event =>
            {
                if (@event.DisplayMode != Character.GameClient.DisplayMode)
                {
                    OnDisplayChanged();
                }
                return false;
            });
            // TODO
            //if (NeedsCharacterDesignFrame(Character))
            //{
            //    OpenCharacterDesignFrame(Character);
            //}
            //else
            //{
                OpenMainGameFrame();
            //}
        }

        private void OnDisplayChanged()
        {
            var opened = new List<IWidget>(Character.Widgets.Widgets);
            OpenMainGameFrame();

            foreach (var inter in opened.Where(inter => inter.IsOpened))
            {
                inter.OnDisplayChanged();
            }

            Character.Area.Script.RenderEnterArea(Character);
        }

        public void OpenMainGameFrame()
        {
            Character.Widgets.CloseAll();

            var gameFrame = _widgetBuilder
                .Create()
                .ForCharacter(Character)
                .WithId(Character.GameClient.IsScreenFixed ? (int)InterfaceIds.FixedFrame : (int)InterfaceIds.ResizedFrame)
                .WithScript<GameFrame>()
                .AsFrame()
                .Build();
            Character.Widgets.OpenFrame(gameFrame);
        }
        
        public void OpenCharacterDesignFrame()
        {
            if (Character.Equipment.FreeSlots != Character.Equipment.Capacity)
            {
                Character.SendChatMessage("Please remove all your equipment before customizing your character.");
                return;
            }

            Character.Widgets.CloseAll();
            var designFrame = _widgetBuilder
                .Create()
                .ForCharacter(Character)
                .WithId((int)InterfaceIds.AccountDesignFrame)
                .WithTransparency(2)
                .WithScript<DesignFrame>()
                .Build();
            Character.Widgets.OpenFrame(designFrame);
        }
        
        /// <summary>
        /// Get's if this character needs design frame.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private static bool NeedsCharacterDesignFrame(ICharacter character)
        {
            if (!character.HasReceivedWelcome || character.DisplayName.StartsWith("#") || character.Name.StartsWith("#") || string.IsNullOrEmpty(character.Name))
                return true;
            for (var i = 0; i < (int)LookType.FeetLook + 1; i++)
                if (character.Appearance.GetLook((LookType)i) != 0)
                    return false;
            return true;
        }
    }
}