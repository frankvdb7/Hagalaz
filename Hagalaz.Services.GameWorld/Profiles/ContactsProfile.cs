using AutoMapper;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol.Model;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class ContactsProfile : Profile
    {
        public ContactsProfile()
        {
            CreateMap<Hagalaz.Contacts.Messages.Model.ContactDto, ContactDto>();

            CreateMap<Hagalaz.Contacts.Messages.Model.ContactDto, Friend>()
                .ForMember(dest => dest.Availability, opt => opt.MapFrom(src => src.Settings!.Availability));
            CreateMap<Hagalaz.Contacts.Messages.Model.ContactDto, Ignore>();
        }
    }
}