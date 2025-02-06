using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterItemProfile : Profile
    {
        public CharacterItemProfile() 
        {
            CreateMap<CharactersItem, Item>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(dest => (int)dest.ItemId))
                .ForMember(dest => dest.ContainerType, opt => opt.MapFrom(dest => (ItemContainerType)dest.ContainerType))
                .ForMember(dest => dest.SlotId, opt => opt.MapFrom(dest => (int)dest.SlotId));
            CreateMap<Item, ItemDto>();
            CreateMap<List<Item>, ItemCollectionDto>()
                .ForMember(dest => dest.Bank, opt => opt.MapFrom(src => src.Where(i => 
                i.ContainerType == ItemContainerType.Bank)))
                .ForMember(dest => dest.Inventory, opt => opt.MapFrom(src => src.Where(i => 
                i.ContainerType == ItemContainerType.Inventory)))
                .ForMember(dest => dest.FamiliarInventory, opt => opt.MapFrom(src => src.Where(i =>
                i.ContainerType == ItemContainerType.FamiliarInventory)))
                .ForMember(dest => dest.Equipment, opt => opt.MapFrom(src => src.Where(i => 
                i.ContainerType == ItemContainerType.Equipment)))
                .ForMember(dest => dest.Rewards, opt => opt.MapFrom(src => src.Where(i => 
                i.ContainerType == ItemContainerType.Reward)))
                .ForMember(dest => dest.MoneyPouch, opt => opt.MapFrom(src => src.Where(i => 
                i.ContainerType == ItemContainerType.MoneyPouch)));
        }
    }
}
