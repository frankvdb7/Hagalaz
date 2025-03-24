using System;
using System.Text;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Logic.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class FarmingPatchTickTask : RsTickTask, IFarmingPatch, IHydratable<HydratedFarmingDto.PatchDto>, IDehydratable<HydratedFarmingDto.PatchDto>
    {
        /// <summary>
        /// The weed ticks
        /// </summary>
        private const int _weedGrowTicks = 500; // 5 minutes = 500 ticks

        /// <summary>
        /// The weed maximum cycles.
        /// </summary>
        private const byte _weedMaxCycles = 3;

        /// <summary>
        /// The owner.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// The flags
        /// </summary>
        private PatchCondition _conditionFlag;

        /// <summary>
        /// The product count.
        /// </summary>
        private int _productCount;

        /// <summary>
        /// Contains the current cycle.
        /// </summary>
        public int CurrentCycle { get; private set; }

        /// <summary>
        /// Contains the seed definition.
        /// </summary>
        public SeedDto? Seed { get; private set; }

        /// <summary>
        /// Contains the patch definition.
        /// </summary>
        public PatchDto PatchDefinition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FarmingPatchTickTask" /> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="patchObjectID">The patch object identifier.</param>
        public FarmingPatchTickTask(ICharacter owner, int patchObjectID)
            : base()
        {
            _owner = owner;
            var farmingManager = _owner.ServiceProvider.GetRequiredService<IFarmingService>();
            PatchDefinition = farmingManager.FindPatchById(patchObjectID).Result!;
            CurrentCycle = 0;
            TickActionMethod = Tick;
        }

        /// <summary>
        /// Determines whether the specified flag has flag.
        /// </summary>
        /// <param name="condition">The flag.</param>
        /// <returns></returns>
        public bool HasCondition(PatchCondition condition) => _conditionFlag.HasFlag(condition);

        /// <summary>
        /// Adds the condition.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public void AddCondition(PatchCondition condition) => _conditionFlag |= condition;

        /// <summary>
        /// Removes the condition.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public void RemoveCondition(PatchCondition condition) => _conditionFlag &= ~condition;

        /// <summary>
        /// Plants the specified definition.
        /// </summary>
        /// <param name="definition">The definition.</param>
        public void Plant(SeedDto definition)
        {
            Seed = definition;
            CurrentCycle = 0;
            Reset();
            AddCondition(PatchCondition.Planted);
            Refresh();
        }

        /// <summary>
        /// Harvests a single product from this patch.
        /// </summary>
        public void Harvest()
        {
            if (--_productCount == 0)
                Clear();
        }

        /// <summary>
        /// Clears this farming patch (and conditions).
        /// </summary>
        public void Clear()
        {
            CurrentCycle = _weedMaxCycles; // no weeds
            AddCondition(PatchCondition.Cleared);
            RemoveCondition(PatchCondition.Mature);
            RemoveCondition(PatchCondition.Diseased);
            RemoveCondition(PatchCondition.Dead);
            RemoveCondition(PatchCondition.Fertilized);
            RemoveCondition(PatchCondition.SuperFertilized);
            RemoveCondition(PatchCondition.Watered);
            RemoveCondition(PatchCondition.Planted);
            Refresh();
            Reset();
        }

        /// <summary>
        /// Increments the cycle.
        /// </summary>
        public void IncrementCycle()
        {
            CurrentCycle++;
            Reset();
        }

        /// <summary>
        /// Decrements the cycle.
        /// </summary>
        public void DecrementCycle()
        {
            CurrentCycle--;
            Reset();
        }

        /// <summary>
        /// Grows this instance.
        /// </summary>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        private void Grow(bool refresh)
        {
            if (HasCondition(PatchCondition.Dead))
                return;

            // check if patch has the default object state, if so remove the instance
            if (CurrentCycle == 0 && !HasCondition(PatchCondition.Planted))
            {
                _owner.Farming.RemoveFarmingPatch(PatchDefinition.ObjectID); // remove this instance
                return;
            }

            // weed grow back
            if (!HasCondition(PatchCondition.Planted) && CurrentCycle > 0)
            {
                RemoveCondition(PatchCondition.Cleared);
                DecrementCycle();
                if (refresh)
                    Refresh();
                return;
            }

            // growing of plants
            if (!HasCondition(PatchCondition.Planted) || HasCondition(PatchCondition.Mature))
            {
                return;
            }

            if (HasCondition(PatchCondition.Diseased))
            {
                RemoveCondition(PatchCondition.Diseased);
                AddCondition(PatchCondition.Dead);
                if (refresh)
                    Refresh();
                Reset();
                return;
            }

            RemoveCondition(PatchCondition.Watered);
            IncrementCycle();
            if (CurrentCycle >= Seed.MaxCycles)
            {
                AddCondition(PatchCondition.Mature);
                if (refresh)
                    Refresh();

                var minimumCount = Seed.MinimumProductCount;
                var maximumCount = Seed.MaximumProductCount + 1;
                if (HasCondition(PatchCondition.Fertilized))
                    minimumCount = (int)(minimumCount * 1.33); // 33% more
                else if (HasCondition(PatchCondition.SuperFertilized))
                    minimumCount = (int)(minimumCount * 1.66); // 66% more
                if (minimumCount > maximumCount)
                    maximumCount = minimumCount + 1;
                _productCount = RandomStatic.Generator.Next(minimumCount, maximumCount);
            }
            else
            {
                // TODO - Include a farming - required level formula
                var diseaseBaseValue = 25;
                if (HasCondition(PatchCondition.Watered))
                    diseaseBaseValue += 10;
                if (HasCondition(PatchCondition.Fertilized))
                    diseaseBaseValue += 10;
                if (HasCondition(PatchCondition.SuperFertilized))
                    diseaseBaseValue += 20;
                if (RandomStatic.Generator.Next(diseaseBaseValue) == 0)
                    AddCondition(PatchCondition.Diseased);
                if (refresh)
                    Refresh();
            }
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            _owner.QueueTask(async () =>
            {
                var manager = _owner.ServiceProvider.GetRequiredService<IGameObjectService>();
                var objDefinition = await manager.FindGameObjectDefinitionById(PatchDefinition.ObjectID);
                _owner.Configurations.SendBitConfiguration(objDefinition.VarpBitFileId,
                    HasCondition(PatchCondition.Planted) ? GetVarpBitValue() : CurrentCycle);
            });
        }

        /// <summary>
        /// Gets the base varp bit value.
        /// </summary>
        /// <returns></returns>
        private int GetBaseVarpBitValue()
        {
            if (!HasCondition(PatchCondition.Planted))
            {
                return CurrentCycle;
            }

            switch (Seed.Type)
            {
                case PatchType.Allotment:
                    return 6 + Seed.VarpBitIndex * 7;
                case PatchType.Herb:
                    return 4 + Seed.VarpBitIndex * 7;
                case PatchType.Flower:
                    return 8 + Seed.VarpBitIndex * 5;
                case PatchType.Hop:
                    return 3 + Seed.VarpBitIndex * 5;
                default:
                    return 8 + (Seed.VarpBitIndex ^ 2 - 1);
            }
        }

        /// <summary>
        /// Gets the varp bit value.
        /// </summary>
        /// <returns></returns>
        private int GetVarpBitValue()
        {
            var baseValue = GetBaseVarpBitValue();
            switch (PatchDefinition.Type)
            {
                case PatchType.Herb:
                    {
                        if (HasCondition(PatchCondition.Dead))
                            return CurrentCycle + 169;
                        if (HasCondition(PatchCondition.Diseased))
                            return baseValue + CurrentCycle + 127;
                        return baseValue + CurrentCycle;
                    }
                case PatchType.Tree:
                    {
                        if (HasCondition(PatchCondition.Dead))
                            baseValue += CurrentCycle + 128;
                        else if (HasCondition(PatchCondition.Diseased))
                            baseValue += CurrentCycle + 64;
                        else
                            baseValue += CurrentCycle;
                        if (!HasCondition(PatchCondition.Checked))
                        {
                            return baseValue;
                        }

                        baseValue += 2;
                        if (!HasCondition(PatchCondition.Empty))
                            baseValue--;
                        return baseValue;
                    }
                case PatchType.Flower:
                case PatchType.Hop:
                case PatchType.Allotment:
                    {
                        if (HasCondition(PatchCondition.Dead))
                            return baseValue + CurrentCycle + 192;
                        if (HasCondition(PatchCondition.Diseased))
                            return baseValue + CurrentCycle + 128;
                        if (HasCondition(PatchCondition.Watered))
                            return baseValue + CurrentCycle + 64;
                        return baseValue + CurrentCycle;
                    }
                default:
                    return baseValue + CurrentCycle;
            }
        }

        /// <summary>
        /// Ticks
        /// </summary>
        private new void Tick()
        {
            if (HasCondition(PatchCondition.Dead))
                return;

            if (TickCount >= _weedGrowTicks)
            {
                // weed grow back
                if (!HasCondition(PatchCondition.Planted))
                {
                    Grow(true);
                    return;
                }
            }

            if (!HasCondition(PatchCondition.Planted) || TickCount < Seed?.CycleTicks)
            {
                return;
            }

            // growing of plants
            if (HasCondition(PatchCondition.Mature))
            {
                return;
            }

            Grow(true);
        }

        /// <summary>
        /// Builds the update query.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public void BuildUpdateQuery(StringBuilder builder)
        {
            builder.Append("INSERT INTO characters_farming_patches VALUES (@master_id,");
            builder.Append(PatchDefinition.ObjectID).Append(",");
            builder.Append(Seed == null ? 0 : Seed.ItemID).Append(",");
            builder.Append((int)_conditionFlag).Append(",");
            builder.Append(CurrentCycle).Append(",");
            builder.Append(TickCount).Append(",");
            builder.Append(_productCount).Append(");");
        }

        public void Hydrate(HydratedFarmingDto.PatchDto hydration)
        {
            _conditionFlag = hydration.Condition;
            _productCount = hydration.ProductCount;
            CurrentCycle = hydration.CurrentCycle;
            TickCount = -hydration.CurrentCycleTicks;
            if (HasCondition(PatchCondition.Planted))
            {
                var farmingManager = _owner.ServiceProvider.GetRequiredService<IFarmingService>();
                Seed = farmingManager.FindSeedById(hydration.SeedId).Result;
            }

            // calculate the current cycle
            var timePassed = DateTimeOffset.Now - _owner.LastLogin;
            var ticksPassed = TickCount + (int)(timePassed.TotalMilliseconds / 600);
            var cyclesPassed = ticksPassed / (HasCondition(PatchCondition.Planted) ? Seed.CycleTicks : _weedGrowTicks);
            for (var cycle = 0; cycle < cyclesPassed; cycle++)
            {
                if (HasCondition(PatchCondition.Mature) || HasCondition(PatchCondition.Dead))
                    break;
                Grow(false);
                ticksPassed -= Seed != null && HasCondition(PatchCondition.Planted) ? Seed.CycleTicks : _weedGrowTicks;
            }

            if (ticksPassed > 0)
            {
                TickCount = ticksPassed;
            }
        }

        public HydratedFarmingDto.PatchDto Dehydrate() => new HydratedFarmingDto.PatchDto()
        {
            Condition = _conditionFlag,
            ProductCount = _productCount,
            CurrentCycle = CurrentCycle,
            CurrentCycleTicks = TickCount
        };
    }
}