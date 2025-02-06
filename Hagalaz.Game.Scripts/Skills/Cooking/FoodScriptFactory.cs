using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Cooking
{
    public class FoodScriptFactory : IItemScriptFactory
    {
        private readonly ICookingService _cookingService;

        public FoodScriptFactory(ICookingService cookingService) => _cookingService = cookingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var foodType = typeof(StandardFood);
            var rawFood = await _cookingService.FindAllRawFood();
            var cookedFood = await _cookingService.FindAllFood();
            foreach (var food in cookedFood.ExceptBy(rawFood.Select(f => f.ItemId), cooked => cooked.ItemId))
            {
                yield return (food.ItemId, foodType);
            }
            // eatable raw food
            foreach (var food in rawFood.IntersectBy(cookedFood.Select(f => f.ItemId), cooked => cooked.ItemId))
            {
                yield return (food.ItemId, foodType);
            }
        }
    }
}
