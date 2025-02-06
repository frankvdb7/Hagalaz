using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Authorization.Data;

namespace Hagalaz.Services.Authorization.Services
{
    public class OffenceService : IOffenceService
    {
        private readonly ICharacterUnitOfWork _characterUnitOfWork;

        public OffenceService(ICharacterUnitOfWork characterUnitOfWork) => _characterUnitOfWork = characterUnitOfWork;

        public async ValueTask<bool> HasActiveBanOffenceAsync(uint id) =>
            await _characterUnitOfWork.CharacterOffenceRepository.FindOffenceByMasterId(id)
                .AnyAsync(offence => offence.OffenceType == CharactersOffence.OffenceTypeEnum.Banned && offence.Date > DateTime.Now);
    }
}