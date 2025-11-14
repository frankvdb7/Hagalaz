using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Methods for character serialization.
    /// </summary>
    public partial class Character : Creature,
        IHydratable<HydratedClaims>,
        IHydratable<HydratedAppearanceDto>,
        IHydratable<HydratedDetailsDto>,
        IHydratable<HydratedItemCollectionDto>,
        IHydratable<HydratedStatisticsDto>,
        IHydratable<HydratedFamiliarDto>,
        IHydratable<HydratedMusicDto>,
        IHydratable<HydratedFarmingDto>,
        IHydratable<HydratedSlayerDto>,
        IHydratable<HydratedNotesDto>,
        IHydratable<HydratedProfileDto>,
        IHydratable<HydratedStateDto>,
        IDehydratable<HydratedAppearanceDto>,
        IDehydratable<HydratedDetailsDto>,
        IDehydratable<HydratedItemCollectionDto>,
        IDehydratable<HydratedStatisticsDto>,
        IDehydratable<HydratedFamiliarDto>,
        IDehydratable<HydratedMusicDto>,
        IDehydratable<HydratedFarmingDto>,
        IDehydratable<HydratedSlayerDto>,
        IDehydratable<HydratedNotesDto>,
        IDehydratable<HydratedProfileDto>,
        IDehydratable<HydratedStateDto>
    {
        public void Hydrate(HydratedAppearanceDto hydration)
        {
            if (Appearance is IHydratable<HydratedAppearanceDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        public void Hydrate(HydratedDetailsDto hydration) => Location = Game.Abstractions.Model.Location.Create(hydration.CoordX, hydration.CoordY, hydration.CoordZ, 0);

        public void Hydrate(HydratedItemCollectionDto hydration)
        {
            if (Bank is IHydratable<IReadOnlyList<HydratedItemDto>> bank)
            {
                bank.Hydrate(hydration.Bank);
            }
            if (Inventory is IHydratable<IReadOnlyList<HydratedItemDto>> inventory)
            {
                inventory.Hydrate(hydration.Inventory);
            }
            if (Equipment is IHydratable<IReadOnlyList<HydratedItemDto>> equipment)
            {
                equipment.Hydrate(hydration.Equipment);
            }
            if (Rewards is IHydratable<IReadOnlyList<HydratedItemDto>> rewards)
            {
                rewards.Hydrate(hydration.Rewards);
            }
            if (MoneyPouch is IHydratable<IReadOnlyList<HydratedItemDto>> moneyPouch)
            {
                moneyPouch.Hydrate(hydration.MoneyPouch);
            }
            if (FamiliarScript is IHydratable<IReadOnlyList<HydratedItem>> familiarInventory)
            {
                familiarInventory.Hydrate(hydration.FamiliarInventory.Select(item => new HydratedItem(item.ItemId, item.Count, item.SlotId, item.ExtraData)).ToList());
            }
        }

        public void Hydrate(HydratedStatisticsDto hydration)
        {
            if (Statistics is IHydratable<HydratedStatisticsDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        HydratedAppearanceDto IDehydratable<HydratedAppearanceDto>.Dehydrate()
        {
            if (Appearance is IDehydratable<HydratedAppearanceDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedAppearanceDto();
        }

        HydratedDetailsDto IDehydratable<HydratedDetailsDto>.Dehydrate() => new() { CoordX = Location.X, CoordY = Location.Y, CoordZ = Location.Z };

        public HydratedItemCollectionDto Dehydrate()
        {
            var items = new HydratedItemCollectionDto();
            if (Bank is IDehydratable<IReadOnlyList<HydratedItemDto>> bank)
            {
                items = items with
                {
                    Bank = bank.Dehydrate()
                };
            }
            if (Inventory is IDehydratable<IReadOnlyList<HydratedItemDto>> inventory)
            {
                items = items with
                {
                    Inventory = inventory.Dehydrate()
                };
            }
            if (Equipment is IDehydratable<IReadOnlyList<HydratedItemDto>> equipment)
            {
                items = items with
                {
                    Equipment = equipment.Dehydrate()
                };
            }
            if (Rewards is IDehydratable<IReadOnlyList<HydratedItemDto>> rewards)
            {
                items = items with
                {
                    Rewards = rewards.Dehydrate()
                };
            }
            if (MoneyPouch is IDehydratable<IReadOnlyList<HydratedItemDto>> moneyPouch)
            {
                items = items with
                {
                    MoneyPouch = moneyPouch.Dehydrate()
                };
            }
            if (FamiliarScript is IDehydratable<IReadOnlyList<HydratedItem>> familiarInventory)
            {
                items = items with
                {
                    FamiliarInventory = familiarInventory.Dehydrate().Select(item => new HydratedItemDto(item.ItemId, item.Count, item.SlotId, item.ExtraData)).ToList()
                };
            }
            return items;
        }

        HydratedStatisticsDto IDehydratable<HydratedStatisticsDto>.Dehydrate()
        {
            if (Statistics is IDehydratable<HydratedStatisticsDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedStatisticsDto();
        }

        public void Hydrate(HydratedFamiliarDto hydration)
        {
            var provider = ServiceProvider.GetRequiredService<IFamiliarScriptProvider>();
            var scriptType = provider.FindFamiliarScriptTypeById(hydration.FamiliarId);
            FamiliarScript = (IFamiliarScript)ServiceProvider.GetRequiredService(scriptType);
            if (FamiliarScript is IHydratable<HydratedFamiliar> hydratable)
            {
                hydratable.Hydrate(new HydratedFamiliar
                {
                    TicksRemaining = hydration.TicksRemaining,
                    IsUsingSpecialMove = hydration.IsUsingSpecialMove,
                    SpecialMovePoints = hydration.SpecialMovePoints
                });
            }
        }

        HydratedFamiliarDto IDehydratable<HydratedFamiliarDto>.Dehydrate()
        {
            if (FamiliarScript is IDehydratable<HydratedFamiliar> dehydratable)
            {
                var dehydration = dehydratable.Dehydrate();
                return new HydratedFamiliarDto
                {
                    FamiliarId = FamiliarScript.Familiar.Appearance.CompositeID,
                    TicksRemaining = dehydration.TicksRemaining,
                    IsUsingSpecialMove = dehydration.IsUsingSpecialMove,
                    SpecialMovePoints = dehydration.SpecialMovePoints
                };
            }
            return new HydratedFamiliarDto();
        }

        public void Hydrate(HydratedClaims hydration)
        {
            Name = hydration.UserName;
            DisplayName = hydration.DisplayName;
            PreviousDisplayName = hydration.PreviousDisplayName;
            Permissions = hydration.Permissions;
            LastLogin = hydration.LastLogin;
        }

        public void Hydrate(HydratedMusicDto hydration)
        {
            if (Music is IHydratable<HydratedMusicDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        HydratedMusicDto IDehydratable<HydratedMusicDto>.Dehydrate()
        {
            if (Music is IDehydratable<HydratedMusicDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedMusicDto([], [], false, false);
        }

        public void Hydrate(HydratedSlayerDto hydration) 
        { 
            if (Slayer is IHydratable<HydratedSlayerDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        HydratedSlayerDto IDehydratable<HydratedSlayerDto>.Dehydrate()
        {
            if (Slayer is IDehydratable<HydratedSlayerDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedSlayerDto();
        }

        public void Hydrate(HydratedFarmingDto hydration)
        {
            if (Farming is IHydratable<HydratedFarmingDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }
        HydratedFarmingDto IDehydratable<HydratedFarmingDto>.Dehydrate()
        {
            if (Farming is IDehydratable<HydratedFarmingDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedFarmingDto();
        }

        public void Hydrate(HydratedNotesDto hydration)
        {
            if (Notes is IHydratable<HydratedNotesDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        HydratedNotesDto IDehydratable<HydratedNotesDto>.Dehydrate()
        {
            if (Notes is IDehydratable<HydratedNotesDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedNotesDto();
        }

        public void Hydrate(HydratedProfileDto hydration)
        {
            if (Profile is IHydratable<HydratedProfileDto> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        HydratedProfileDto IDehydratable<HydratedProfileDto>.Dehydrate()
        {
            if (Profile is IDehydratable<HydratedProfileDto> dehydratable)
            {
                return dehydratable.Dehydrate();
            }
            return new HydratedProfileDto { JsonData = string.Empty };
        }

        public void Hydrate(HydratedStateDto hydration)
        {
            var stateService = ServiceProvider.GetRequiredService<IStateService>();
            foreach (var state in hydration.StatesEx)
            {
                var result = stateService.GetStateAsync(state.Id).Result;
                if (!result.IsSuccess)
                {
                    continue;
                }

                var stateObject = result.Value;
                stateObject.TicksLeft = state.TicksLeft;
                AddState(stateObject);
            }
        }

        HydratedStateDto IDehydratable<HydratedStateDto>.Dehydrate() => new HydratedStateDto
        {
            StatesEx = States.Values.Select(s => new HydratedStateDto.HydratedStateExDto { Id = s.GetType().GetCustomAttribute<StateIdAttribute>()!.Id, TicksLeft = s.TicksLeft }).ToList()
        };
    }
}
