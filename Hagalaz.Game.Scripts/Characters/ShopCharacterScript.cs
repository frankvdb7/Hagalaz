using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Widgets.Shop;

namespace Hagalaz.Game.Scripts.Characters
{
    public class ShopCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IShopService _shopService;
        private readonly IWidgetBuilder _widgetBuilder;
        private EventHappened? _event;

        public ShopCharacterScript(ICharacterContextAccessor contextAccessor, IShopService shopService, IWidgetBuilder widgetBuilder) : base(contextAccessor)
        {
            _shopService = shopService;
            _widgetBuilder = widgetBuilder;
        }

        protected override void Initialize()
        {
            _event = Character.RegisterEventHandler<OpenShopEvent>(e =>
            {
                Character.QueueTask(() => OpenShop(e.ShopId));
                return true;
            });
        }

        private async Task OpenShop(int shopId)
        {
            var shop = await _shopService.GetShopByIdAsync(shopId);
            Character.CurrentShop = shop;
            var shopScreen = Character.ServiceProvider.GetRequiredService<ShopScreenScript>();
            Character.Widgets.OpenWidget(1265, 0, shopScreen, true);
        }

        public override void OnRemove()
        {
            Character.UnregisterEventHandler<OpenShopEvent>(_event!);
        }
    }
}