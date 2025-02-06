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
    public class HerbloreService : IHerbloreService
    {
        // TODO - move to database
        private static readonly PotionDto[] Potions =
        [
            new(121, 91, 1, 25, [249], [221]), // Attack potion
            new(175, 93, 5, 37.5, [251], [235]), // Antipoison
            new(4844, 4840, 8, 40, [1534], [1526]), // Relicym's balm
            new(115, 95, 12, 50, [253], [225]), // Strength potion
            new(127, 97, 22, 62.5, [255], [223]), // Restore potion
            new(3010, 97, 26, 67.5, [253], [1975]), // Energy potion
            new(133, 93, 30, 75, [257], [239]), // Defence potion
            new(139, 99, 38, 87.5, [257], [231]), // Prayer potion
            new(12142, 12181, 40, 92, [12172], [12109]), // Summoning potion
            new(145, 101, 45, 100, [259], [221]), // Super attack potion
            new(181, 101, 48, 106.3, [259], [235]), // Super antipoison
            new(3018, 103, 52, 117.5, [261], [2970]), // Super energy
            new(157, 105, 55, 125, [263], [225]), // Super strength potion
            new(187, 105, 60, 137.5, [263], [241]), // Weapon poison
            new(3026, 3004, 63, 142.5, [3000], [223]), // Super restore
            new(163, 107, 66, 150, [265], [239]), // Super defence
            new(2454, 2483, 68, 155, [2481], [241]), // Antifire
            new(169, 109, 72, 162.5, [267], [245]), // Ranging potion
            new(3042, 2483, 76, 172.5, [2481], [3138]), // Magic potion
            new(189, 111, 78, 175, [269], [247]), // Zamorak brew
            new(6687, 3002, 81, 180, [2998], [6693]), // Saradomin brew
            new(15301, 3018, 84, 200, [], [5972]), // Recover special
            new(15305, 2454, 85, 210, [], [4621]), // Super antifire
            new(15309, 145, 88, 220, [], []), // Extreme attack
            new(15313, 157, 89, 230, [], []), // Extreme strength
            new(15317, 163, 90, 240, [], []), // Extreme defence
            new(15321, 3042, 91, 250, [], []), // Extreme magic
            new(15325, 169, 92, 260, [], []), // Extreme ranging
            new(15329, 139, 94, 270, [], [6810]), // Super prayer
            new(21632, 21628, 94, 190, [21624], [21622]) // Prayer renewal
        ];

        private readonly IHerbloreRepository _herbloreRepository;
        private readonly IMapper _mapper;

        public HerbloreService(IHerbloreRepository herbloreRepository, IMapper mapper)
        {
            _herbloreRepository = herbloreRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<HerbDto>> FindAllHerbs() => await _mapper.ProjectTo<HerbDto>(_herbloreRepository.FindAll()).ToListAsync();
        public async Task<HerbDto?> FindGrimyHerbById(int id) => await _mapper.ProjectTo<HerbDto>(_herbloreRepository.FindGrimyHerbById(id)).FirstOrDefaultAsync();
        public async Task<IReadOnlyList<PotionDto>> FindAllPotions() => Potions.ToList();
    }
}
