using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class StateHydrator : ICharacterHydrator
    {
        private readonly IStateService _stateService;

        public StateHydrator(IStateService stateService) => _stateService = stateService;

        public async Task HydrateAsync(ICharacter character, CharacterModel model)
        {
            if (model.State is null)
            {
                return;
            }

            foreach (var stateEx in model.State.StatesEx)
            {
                var result = await _stateService.GetStateAsync(stateEx.Id.ToString());
                if (result.IsSuccess)
                {
                    var state = result.Value;
                    state.TicksLeft = stateEx.TicksLeft;
                    character.AddState(state);
                }
            }
        }
    }
}
