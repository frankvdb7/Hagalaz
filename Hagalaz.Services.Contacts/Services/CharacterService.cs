using AutoMapper;
using Hagalaz.Services.Contacts.Data;
using Hagalaz.Services.Contacts.Services.Model;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.Contacts.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CharacterService(ICharacterUnitOfWork unitOfWork, IMapper mapper) 
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async ValueTask<CharacterDto?> FindCharacterByDisplayName(string name)
        {
            return await _mapper.ProjectTo<CharacterDto>(_unitOfWork.CharacterRepository.FindByDisplayNameAsync(name))
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async ValueTask<CharacterDto?> FindCharacterByIdAsync(uint id)
        {
            return await _mapper.ProjectTo<CharacterDto>(_unitOfWork.CharacterRepository.FindByIdAsync(id))
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
