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
    public class CookingService : ICookingService
    {
        private readonly ICookingFoodRepository _cookingFoodRepository;
        private readonly ICookingRawFoodRepository _cookingRawFoodRepository;
        private readonly IMapper _mapper;

        public CookingService(ICookingFoodRepository cookingFoodRepository, ICookingRawFoodRepository cookingRawFoodRepository, IMapper mapper)
        {
            _cookingFoodRepository = cookingFoodRepository;
            _cookingRawFoodRepository = cookingRawFoodRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<FoodDto>> FindAllFood() => await _mapper.ProjectTo<FoodDto>(_cookingFoodRepository.FindAll()).ToListAsync();


        public async Task<IReadOnlyList<RawFoodDto>> FindAllRawFood() => await _mapper.ProjectTo<RawFoodDto>(_cookingRawFoodRepository.FindAll()).ToListAsync();

        public Task<FoodDto?> FindFoodById(int itemId) => _mapper.ProjectTo<FoodDto>(_cookingFoodRepository.FindAll().Where(d => d.ItemId == itemId)).FirstOrDefaultAsync();

        public Task<RawFoodDto?> FindRawFoodById(int itemId) => _mapper.ProjectTo<RawFoodDto>(_cookingRawFoodRepository.FindAll().Where(d => d.ItemId == itemId)).FirstOrDefaultAsync();
    }
}