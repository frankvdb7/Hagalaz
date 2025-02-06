using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class PrayerService : IPrayerService
    {
        private readonly IPrayerRepository _prayerRepository;
        private readonly IMapper _mapper;

        public PrayerService(IPrayerRepository prayerRepository, IMapper mapper)
        {
            _prayerRepository = prayerRepository;
            _mapper = mapper;
        }

        public Task<PrayerDto?> FindById(int itemId) => _mapper.ProjectTo<PrayerDto>(_prayerRepository.FindAll().Where(p => p.ItemId == itemId)).FirstOrDefaultAsync();
        public async Task<IReadOnlyList<PrayerDto>> FindAllByType(PrayerDtoType definitionType) => 
            await _mapper.ProjectTo<PrayerDto>(_prayerRepository.FindAll().Where(p => p.Type == definitionType.ToString())).ToListAsync();
    }
}