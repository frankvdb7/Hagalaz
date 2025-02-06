using System.Linq;
using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;
using Hagalaz.Utilities;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterMusicProfile : Profile
    {
        public CharacterMusicProfile()
        {
            CreateMap<CharactersMusic, Music>()
                .ForMember(dest => dest.UnlockedMusicIds, 
                opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.UnlockedMusic).ToArray()))
                .ForMember(dest => dest.PlaylistMusicIds, opt => opt.Ignore())
                .ForMember(dest => dest.IsShuffleToggled, opt => opt.Ignore())
                .ForMember(dest => dest.IsPlaylistToggled, opt => opt.Ignore());
            CreateMap<CharactersMusicPlaylist, Music>()
                .ForMember(dest => dest.UnlockedMusicIds, opt => opt.Ignore())
                .ForMember(dest => dest.PlaylistMusicIds,
                opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.Playlist).ToArray()))
                .ForMember(dest => dest.IsShuffleToggled,
                opt => opt.MapFrom(src => src.ShuffleToggled == 1))
                .ForMember(dest => dest.IsPlaylistToggled,
                opt => opt.MapFrom(src => src.PlaylistToggled == 1));
            CreateMap<Music, MusicDto>();
        }
    }
}
