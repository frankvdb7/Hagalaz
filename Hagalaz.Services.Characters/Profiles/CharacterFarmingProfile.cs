using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterFarmingProfile : Profile
    {
        public CharacterFarmingProfile()
        {
            CreateMap<CharactersFarmingPatch, Farming.Patch>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.PatchId))
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => (int)src.ConditionFlag))
                .ForMember(dest => dest.SeedId, opt => opt.MapFrom(src => (int)src.SeedId))
                .ForMember(dest => dest.CurrentCycleTicks, opt => opt.MapFrom(src => (int)src.CurrentCycleTicks))
                .ForMember(dest => dest.CurrentCycle, opt => opt.MapFrom(src => (int)src.CurrentCycle))
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => (int)src.ProductCount));
            CreateMap<Farming, FarmingDto>();
            CreateMap<Farming.Patch, FarmingDto.PatchDto>();
        }
    }
}
