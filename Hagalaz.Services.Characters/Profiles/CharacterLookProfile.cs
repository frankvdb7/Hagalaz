using System.Collections.Generic;
using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;
using Hagalaz.Utilities;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterLookProfile : Profile
    {
        public CharacterLookProfile()
        {
            CreateMap<CharactersLook, Appearance>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => (int)src.Gender))
                .ForMember(dest => dest.HairColor, opt => opt.MapFrom(src => (int)src.HairColor))
                .ForMember(dest => dest.TorsoColor, opt => opt.MapFrom(src => (int)src.TorsoColor))
                .ForMember(dest => dest.LegColor, opt => opt.MapFrom(src => (int)src.LegColor))
                .ForMember(dest => dest.FeetColor, opt => opt.MapFrom(src => (int)src.FeetColor))
                .ForMember(dest => dest.SkinColor, opt => opt.MapFrom(src => (int)src.SkinColor))
                .ForMember(dest => dest.HairLook, opt => opt.MapFrom(src => src.HairLook))
                .ForMember(dest => dest.BeardLook, opt => opt.MapFrom(src => src.BeardLook))
                .ForMember(dest => dest.TorsoLook, opt => opt.MapFrom(src => src.TorsoLook))
                .ForMember(dest => dest.ArmsLook, opt => opt.MapFrom(src => src.ArmsLook))
                .ForMember(dest => dest.WristLook, opt => opt.MapFrom(src => src.WristLook))
                .ForMember(dest => dest.LegsLook, opt => opt.MapFrom(src => src.LegsLook))
                .ForMember(dest => dest.FeetLook, opt => opt.MapFrom(src => src.FeetLook))
                .ForMember(dest => dest.DisplayTitle, opt => opt.MapFrom(src => (int)src.DisplayTitle));
            CreateMap<CharactersItemsLook, ItemAppearance>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.MaleModels,
                opt => opt.MapFrom(src => new int[] { src.MaleWornModel1, src.MaleWornModel2, src.MaleWornModel3 }))
                .ForMember(dest => dest.FemaleModels,
                opt => opt.MapFrom(src => new int[] { src.FemaleWornModel1, src.FemaleWornModel2, src.FemaleWornModel3 }))
                .ForMember(dest => dest.TextureColors,
                opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.TextureColours!)))
                .ForMember(dest => dest.ModelColors, 
                opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.ModelColours!)));
            CreateMap<ItemAppearance, ItemAppearanceDto>();
            CreateMap<List<ItemAppearance>, ItemAppearanceCollectionDto>()
                .ForMember(dest => dest.Appearances, opt => opt.MapFrom(src => src));
            CreateMap<Appearance, AppearanceDto>();
        }
    }
}
