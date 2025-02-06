using System;
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
    /// <summary>
    /// 
    /// </summary>
    public class CraftingService : ICraftingService
    {
        private readonly ICraftingGemDefinitionRepository _gemDefinitionRepository;
        private readonly ICraftingJewelryDefinitionRepository _jewelryDefinitionRepository;
        private readonly ICraftingLeatherDefinitionRepository _leatherDefinitionRepository;
        private readonly ICraftingPotteryDefinitionRepository _potteryDefinitionRepository;
        private readonly ICraftingSilverDefinitionRepository _silverDefinitionRepository;
        private readonly ICraftingTanDefinitionRepository _tanDefinitionRepository;
        private readonly ICraftingSpinDefinitionRepository _spinDefinitionRepository;
        private readonly IMapper _mapper;

        public CraftingService(ICraftingGemDefinitionRepository gemDefinitionRepository, ICraftingJewelryDefinitionRepository jewelryDefinitionRepository, ICraftingLeatherDefinitionRepository leatherDefinitionRepository, ICraftingPotteryDefinitionRepository potteryDefinitionRepository, ICraftingSilverDefinitionRepository silverDefinitionRepository, ICraftingTanDefinitionRepository tanDefinitionRepository, ICraftingSpinDefinitionRepository spinDefinitionRepository, IMapper mapper)
        {
            _gemDefinitionRepository = gemDefinitionRepository;
            _jewelryDefinitionRepository = jewelryDefinitionRepository;
            _leatherDefinitionRepository = leatherDefinitionRepository;
            _potteryDefinitionRepository = potteryDefinitionRepository;
            _silverDefinitionRepository = silverDefinitionRepository;
            _tanDefinitionRepository = tanDefinitionRepository;
            _spinDefinitionRepository = spinDefinitionRepository;
            _mapper = mapper;
        }

        public Task<GemDto?> FindGemByResourceID(int resourceID) => _mapper.ProjectTo<GemDto>(_gemDefinitionRepository.FindAll().Where(g => g.ResourceId == resourceID).AsNoTracking()).FirstOrDefaultAsync();

        public Task<LeatherDto?> FindLeatherByProductId(int productID) => _mapper.ProjectTo<LeatherDto>(_leatherDefinitionRepository.FindAll().Where(e => e.ProductId == productID).AsNoTracking()).FirstOrDefaultAsync();

        public Task<SpinDto?> FindSpinByProductID(int productID) => _mapper.ProjectTo<SpinDto>(_spinDefinitionRepository.FindAll().Where(s => s.ProductId == productID).AsNoTracking()).FirstOrDefaultAsync();

        public Task<TanDto?> FindTanByProductID(int productID) => _mapper.ProjectTo<TanDto>(_tanDefinitionRepository.FindAll().Where(s => s.ProductId == productID).AsNoTracking()).FirstOrDefaultAsync();

        public Task<PotteryDto?> FindPotteryByProductID(int productID) => _mapper.ProjectTo<PotteryDto>(_potteryDefinitionRepository.FindAll().Where(p => p.FormedProductId == productID).AsNoTracking()).FirstOrDefaultAsync();

        public Task<PotteryDto?> FindPotteryByFinalProductID(int finalProductID) => _mapper.ProjectTo<PotteryDto>(_potteryDefinitionRepository.FindAll().Where(p => p.BakedProductId == finalProductID).AsNoTracking()).FirstOrDefaultAsync();

        public async Task<IReadOnlyList<GemDto>> FindAllGems() => await _mapper.ProjectTo<GemDto>(_gemDefinitionRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<SilverDto>> FindAllSilver() => await _mapper.ProjectTo<SilverDto>(_silverDefinitionRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<JewelryDto>> FindAllJewelry(JewelryDto.JewelryType jewelryType)
        {
            var typeValue = Enum.GetName(jewelryType.GetType(), jewelryType);
            return await _mapper.ProjectTo<JewelryDto>(_jewelryDefinitionRepository.FindAll().Where(s => s.Type == typeValue).AsNoTracking()).ToListAsync();
        }

        public async Task<IReadOnlyList<TanDto>> FindAllTan() => await _mapper.ProjectTo<TanDto>(_tanDefinitionRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<LeatherDto>> FindAllLeather() => await _mapper.ProjectTo<LeatherDto>(_leatherDefinitionRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<PotteryDto>> FindAllPottery() => await _mapper.ProjectTo<PotteryDto>(_potteryDefinitionRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<SpinDto>> FindAllSpin() => await _mapper.ProjectTo<SpinDto>(_spinDefinitionRepository.FindAll().AsNoTracking()).ToListAsync();
    }
}