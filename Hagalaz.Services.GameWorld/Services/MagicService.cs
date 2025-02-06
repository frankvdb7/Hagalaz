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
    public class MagicService : IMagicService
    {
        private readonly IMagicCombatSpellsRepository _combatSpellsRepository;
        private readonly IMagicEnchantingSpellsRepository _enchantingSpellsRepository;
        private readonly IMagicEnchantingSpellsProductRepository _enchantingSpellsProductRepository;
        private readonly IMapper _mapper;

        public MagicService(
            IMagicCombatSpellsRepository combatSpellsRepository, IMagicEnchantingSpellsRepository enchantingSpellsRepository,
            IMagicEnchantingSpellsProductRepository enchantingSpellsProductRepository, IMapper mapper)
        {
            _combatSpellsRepository = combatSpellsRepository;
            _enchantingSpellsRepository = enchantingSpellsRepository;
            _enchantingSpellsProductRepository = enchantingSpellsProductRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<CombatSpellDto>> FindAllCombatSpells() =>
            await _mapper.ProjectTo<CombatSpellDto>(_combatSpellsRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<EnchantingSpellDto>> FindAllEnchantingSpells() =>
            await _mapper.ProjectTo<EnchantingSpellDto>(_enchantingSpellsRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<EnchantingSpellProductDto?> FindEnchantingSpellProductByButtonId(int buttonId) =>
            await _mapper.ProjectTo<EnchantingSpellProductDto>(_enchantingSpellsProductRepository.FindAll().Where(s => s.ButtonId == buttonId))
                .FirstOrDefaultAsync();
    }
}