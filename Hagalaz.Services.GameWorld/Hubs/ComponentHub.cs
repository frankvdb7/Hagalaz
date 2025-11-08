using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs.Filters;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    [CharacterFilter]
    public class ComponentHub : RaidoHub
    {
        private readonly ICharacterService _characterService;
        private readonly INpcService _npcService;
        private readonly IGroundItemService _groundItemService;
        private readonly IGameObjectService _gameObjectService;

        public ComponentHub(ICharacterService characterService, INpcService npcService, IGroundItemService groundItemService, IGameObjectService gameObjectService)
        {
            _characterService = characterService;
            _npcService = npcService;
            _groundItemService = groundItemService;
            _gameObjectService = gameObjectService;
        }

        [RaidoMessageHandler(typeof(InterfaceComponentClickMessage))]
        public void OnComponentClick(InterfaceComponentClickMessage message)
        {
            if (message.InterfaceId < 0 || message.ChildId < 0)
            {
                return;
            }
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.InterfaceId, out var gameInterface))
            {
                return;
            }
            gameInterface.OnComponentClick(message.ChildId, message.ClickType, message.ExtraData1, message.ExtraData2);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentDragMessage))]
        public void OnComponentDrag(InterfaceComponentDragMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.FromId, out var fromInterface) || !character.Widgets.TryGetOpenWidget(message.ToId, out var toInterface))
            {
                return;
            }
            fromInterface.OnComponentDrag(message.FromComponentId, message.FromExtraData1, message.FromExtraData2, toInterface, message.ToComponentId, message.ToExtraData1, message.ToExtraData2);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentUseOnCharacterMessage))]
        public async Task OnComponentUseOnCharacter(InterfaceComponentUseOnCharacterMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.InterfaceId, out var @interface))
            {
                return;
            }
            var target = await _characterService.FindByIndex(message.Index);
            if (target == null)
            {
                return;
            }
            @interface.OnComponentUsedOnCreature(message.ComponentId, target, message.ForceRun, message.ExtraData1, message.ExtraData2);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentUseOnNpcMessage))]
        public async Task OnComponentUseOnNpc(InterfaceComponentUseOnNpcMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.InterfaceId, out var @interface))
            {
                return;
            }
            var target = await _npcService.FindByIndexAsync(message.Index);
            if (target == null)
            {
                return;
            }
            @interface.OnComponentUsedOnCreature(message.ComponentId, target, message.ForceRun, message.ExtraData1, message.ExtraData2);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentUseOnGroundItemMessage))]
        public void OnComponentUseOnGroundItem(InterfaceComponentUseOnGroundItemMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.InterfaceId, out var @interface))
            {
                return;
            }
            var location = new Location(message.AbsX, message.AbsY, character.Location.Z, character.Location.Dimension);
            if (!character.Viewport.InBounds(location))
            {
                return;
            }
            var target = _groundItemService.FindByLocation(location).FirstOrDefault(i => i.ItemOnGround.Id == message.ItemId);
            if (target == null)
            {
                return;
            }
            @interface.OnComponentUsedOnGroundItem(message.ComponentId, target, message.ForceRun, message.ExtraData1, message.ExtraData2);
        }


        [RaidoMessageHandler(typeof(InterfaceComponentUseOnGameObjectMessage))]
        public void OnComponentUseOnGameObject(InterfaceComponentUseOnGameObjectMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.InterfaceId, out var @interface))
            {
                return;
            }
            var location = new Location(message.AbsX, message.AbsY, character.Location.Z, character.Location.Dimension);
            if (!character.Viewport.InBounds(location))
            {
                return;
            }
            var target = _gameObjectService.FindByLocation(location).FirstOrDefault(g => g.Id == message.GameObjectId);
            if (target == null)
            {
                return;
            }
            @interface.OnComponentUsedOnGameObject(message.ComponentId, target, message.ForceRun, message.ExtraData1, message.ExtraData2);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentUseOnComponentMessage))]
        public void OnComponentUseOnComponent(InterfaceComponentUseOnComponentMessage message)
        {
            var character = Context.GetCharacter();
            if (!character.Widgets.TryGetOpenWidget(message.InterfaceId, out var @interface) || !character.Widgets.TryGetOpenWidget(message.OnInterfaceId, out var onInterface))
            {
                return;
            }
            if (!@interface.OnComponentUsedOnComponent(message.ComponentId, message.ExtraData1, message.ExtraData2, message.OnExtraData1, message.OnExtraData2))
            {
                onInterface.OnComponentUsedOnComponent(message.OnComponentId, message.OnExtraData1, message.OnExtraData2, message.ExtraData1, message.ExtraData2);
            }
        }

        [RaidoMessageHandler(typeof(InterfaceComponentRemovedMessage))]
        public void OnComponentRemoved(InterfaceComponentRemovedMessage message)
        {
            var character = Context.GetCharacter();
            character.InterruptInterfaces();
        }

        [RaidoMessageHandler(typeof(InterfaceComponentTextInputMessage))]
        public void OnTextInput(InterfaceComponentTextInputMessage message) 
        {
            var character = Context.GetCharacter();
            character.Widgets.StringInputHandler?.Invoke(message.Text);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentNumberInputMessage))]
        public void OnNumberInput(InterfaceComponentNumberInputMessage message)
        {
            var character = Context.GetCharacter();
            character.Widgets.IntInputHandler?.Invoke(message.Value);
        }

        [RaidoMessageHandler(typeof(InterfaceComponentColorInputMessage))]
        public void OnColorInput(InterfaceComponentColorInputMessage message)
        {
            var character = Context.GetCharacter();
            character.EventManager.SendEvent(new ColorSelectedEvent(character, message.Value));
        }
    }
}
