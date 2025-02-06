using System.Collections.Generic;
using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class ShopProfile : Profile
    {
        public ShopProfile() 
        {
            CreateProjection<Shop, ShopDto>()
                .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dto => dto.GeneralStore, opt => opt.MapFrom(src => src.GeneralStore == 1))
                .ForMember(dto => dto.Capacity, opt => opt.MapFrom(src => (int)src.Capacity))
                .ForMember(dto => dto.CurrencyId, opt => opt.MapFrom(src => (int)src.CurrencyId))
                .ForMember(dto => dto.MainStock, opt => opt.MapFrom(src => SelectItems(src.MainStockItems)))
                .ForMember(dto => dto.SampleStock, opt => opt.MapFrom(src => SelectItems(src.SampleStockItems)));
        }

        private static IEnumerable<ItemDto> SelectItems(string? data)
        {
            if (string.IsNullOrEmpty(data))
            {
                yield break;
            }

            string[] spl = data.Split(';');
            foreach (var itemData in spl)
            {
                string[] idata = itemData.Split(',');
                short itemId = short.Parse(idata[0]);
                int count = int.Parse(idata[1]);
                yield return new ItemDto
                {
                    Id = itemId,
                    Count = count
                };
            }
        }
    }
}
