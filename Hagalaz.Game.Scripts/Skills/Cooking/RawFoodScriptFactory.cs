using System;
using System.Linq;

namespace Hagalaz.Game.Scripts.Skills.Cooking
{
    public class RawFoodScriptFactory : IItemScriptFactory
    {
        private readonly ICookingService _cookingService;

        public RawFoodScriptFactory(ICookingService cookingService) => _cookingService = cookingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var foodType = typeof(RawFood);
            var rawFood = await _cookingService.FindAllRawFood();
            var cookedFood = await _cookingService.FindAllFood();
            foreach (var food in rawFood.ExceptBy(cookedFood.Select(f => f.ItemId), raw => raw.ItemId))
            {
                yield return (food.ItemId, foodType);
            }
        }
    }
}
