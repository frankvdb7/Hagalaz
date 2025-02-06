using System.Collections.Generic;
using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class MinigamesProfile : Profile
    {
        public MinigamesProfile()
        {
            CreateMap<MinigamesTzhaarCaveWave, WaveDto>()
                 .ForMember(dest => dest.WaveId, opt => opt.MapFrom(src => src.WaveId))
                 .ForMember(dest => dest.Npcs, opt => opt.MapFrom(src => new List<WaveDto.NpcWaveDto> { new WaveDto.NpcWaveDto(src.NpcId, src.Count) }));
        }
    }
}
