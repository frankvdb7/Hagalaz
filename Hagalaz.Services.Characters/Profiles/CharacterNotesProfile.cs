using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterNotesProfile : Profile
    {
        public CharacterNotesProfile()
        {
            CreateMap<CharactersNote, Notes.Note>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.NoteId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => (int)src.Colour));
            CreateMap<Notes, NotesDto>()
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.AllNotes));
            CreateMap<Notes.Note, NotesDto.NoteDto>();
        }
    }
}
