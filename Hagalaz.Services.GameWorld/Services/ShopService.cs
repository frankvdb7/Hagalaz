using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class ShopService : IShopService
    {
        private readonly IShopRepository _repository;
        private readonly IRsTaskService _taskScheduler;
        private readonly IItemService _itemService;
        private readonly IItemBuilder _itemBuilder;
        private readonly IMapper _mapper;
        private readonly ShopStore _shops;
        private readonly IEventManager _eventManager;

        public ShopService(
            IShopRepository repository, IRsTaskService taskScheduler, IItemService itemService, IItemBuilder itemBuilder, IMapper mapper, ShopStore shops, IEventManager eventManager)
        {
            _repository = repository;
            _taskScheduler = taskScheduler;
            _itemService = itemService;
            _itemBuilder = itemBuilder;
            _mapper = mapper;
            _shops = shops;
            _eventManager = eventManager;
        }

        public async ValueTask<IShop?> GetShopByIdAsync(int shopId)
        {
            if (_shops.TryGetValue(shopId, out var shop))
            {
                return shop;
            }

            var shopDto = await _mapper.ProjectTo<ShopDto>(_repository.FindById(shopId).AsNoTracking()).FirstOrDefaultAsync();
            if (shopDto == null)
            {
                return null;
            }

            var mainStock = shopDto.MainStock.Select(item => _itemBuilder.Create().WithId(item.Id).WithCount(item.Count).Build());
            var sampleStock = shopDto.SampleStock.Select(item => _itemBuilder.Create().WithId(item.Id).WithCount(item.Count).Build());

            var shopInst = new Shop(shopDto.Name, shopDto.Capacity, shopDto.CurrencyId, shopDto.GeneralStore, mainStock, sampleStock, _itemService, _itemBuilder, _eventManager);
            _shops.TryAdd(shopId, shopInst);
            _taskScheduler.Schedule(shopInst);
            return shopInst;
        }
    }
}