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
            var dto = await _mapper.ProjectTo<CharacterDto>(_unitOfWork.CharacterRepository.FindByDisplayNameAsync(name)).AsNoTracking().FirstOrDefaultAsync();
            if (dto == null)
            {
                return null;
            }
            return await EnrichCharacterWithClaims(dto);
        }

        public async ValueTask<CharacterDto?> FindCharacterByIdAsync(uint id)
        {
            var dto = await _mapper.ProjectTo<CharacterDto>(_unitOfWork.CharacterRepository.FindByIdAsync(id)).AsNoTracking().FirstOrDefaultAsync();
            if (dto == null)
            {
                return null;
            }
            return await EnrichCharacterWithClaims(dto);
        }

        private async ValueTask<CharacterDto> EnrichCharacterWithClaims(CharacterDto dto)
        {
            var claims = await _mapper.ProjectTo<CharacterDto.ClaimDto>(_unitOfWork.CharacterPermissionsRepository.FindPermissionsByMasterIdAsync(dto.MasterId))
                .ToListAsync();

            return dto with
            {
                Claims = claims
            };
        }
    }
}
