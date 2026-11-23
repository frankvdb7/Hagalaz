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
        private readonly IFarmingService _farmingService;
        private readonly IGameObjectService _gameObjectService;

        /// <summary>
        /// The flags
        /// </summary>
        private PatchCondition _conditionFlag;

        /// <summary>
        /// The product count.
        /// </summary>
        private int _productCount;

        private int _seedId;

        /// <summary>
        /// Contains the current cycle.
        /// </summary>
        public int CurrentCycle { get; private set; }

        /// <summary>
        /// Gets the number of products that can currently be taken from the mature plant.
        /// </summary>
        public int ProductCount => _productCount;

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
        public FarmingPatchTickTask(
            ICharacter owner,
            int patchObjectID,
            IFarmingService farmingService,
            IGameObjectService gameObjectService)
            : base()
        {
            _owner = owner;
            _farmingService = farmingService;
            _gameObjectService = gameObjectService;
            PatchDefinition = _farmingService.FindPatchById(patchObjectID).Result!;
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
            _seedId = definition.ItemID;
            CurrentCycle = 0;
            Reset();
            AddCondition(PatchCondition.Planted);
            Refresh();
        }

        /// <summary>
        /// Harvests a single product from this patch.
        /// Fruit trees and bushes keep their plant and replenish; every other crop is cleared once empty.
        /// </summary>
        public void Harvest()
        {
            if (ReplenishesProducts)
            {
                if (_productCount >= Seed!.MaximumProductCount)
                    Reset(); // a full plant only starts regrowing once something is picked.
                if (_productCount > 0)
                    _productCount--;
                Refresh();
                return;
            }

            if (--_productCount <= 0)
                Clear();
        }

        /// <summary>
        /// Fells a mature, health-checked tree, leaving its stump until the patch is dug up.
        /// </summary>
        /// <returns><c>true</c> if the tree was felled; <c>false</c> if the patch holds no standing checked tree.</returns>
        public bool FellTree()
        {
            if (PatchDefinition.Type != PatchType.Tree
                || !HasCondition(PatchCondition.Mature)
                || !HasCondition(PatchCondition.Checked)
                || HasCondition(PatchCondition.Empty))
            {
                return false;
            }

            _productCount = 0;
            AddCondition(PatchCondition.Empty);
            Refresh();
            return true;
        }

        private int RegrowTicks => Seed!.ProductRegrowTicks > 0 ? Seed.ProductRegrowTicks : Seed.CycleTicks;

        private bool ReplenishesProducts =>
            Seed != null
            && HasCondition(PatchCondition.Planted)
            && HasCondition(PatchCondition.Mature)
            && PatchDefinition.Type is PatchType.FruitTree or PatchType.Bush;

        /// <summary>
        /// Regrows one fruit or berry per regrow interval until the plant holds its maximum yield.
        /// </summary>
        private void Replenish()
        {
            if (_productCount >= Seed!.MaximumProductCount || RegrowTicks <= 0)
            {
                Reset();
                return;
            }

            if (TickCount < RegrowTicks)
                return;

            _productCount++;
            Reset();
            Refresh();
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
            RemoveCondition(PatchCondition.Checked);
            RemoveCondition(PatchCondition.Empty);
            _productCount = 0;
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
            if (!HasCondition(PatchCondition.Planted) || HasCondition(PatchCondition.Mature) || Seed == null)
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

            // Watering protects only the growth interval that just completed, so the disease roll
            // below must see the state captured before the flag is consumed.
            var growthConditions = _conditionFlag;
            RemoveCondition(PatchCondition.Watered);
            IncrementCycle();
            if (CurrentCycle >= Seed.MaxCycles)
            {
                AddCondition(PatchCondition.Mature);
                if (refresh)
                    Refresh();

                var minimumCount = Seed.MinimumProductCount;
                var maximumCount = Seed.MaximumProductCount + 1;
                // Fruit trees and bushes always bear their full yield; compost must not exceed what the client can render.
                if (PatchDefinition.Type is PatchType.FruitTree or PatchType.Bush)
                    maximumCount = minimumCount + 1;
                else if (HasCondition(PatchCondition.Fertilized))
                    minimumCount = (int)(minimumCount * 1.33); // 33% more
                else if (HasCondition(PatchCondition.SuperFertilized))
                    minimumCount = (int)(minimumCount * 1.66); // 66% more
                if (minimumCount > maximumCount)
                    maximumCount = minimumCount + 1;
                _productCount = RandomStatic.Generator.Next(minimumCount, maximumCount);
            }
            else
            {
                var diseaseBaseValue = GetDiseaseChanceDenominator(growthConditions);
                if (RandomStatic.Generator.Next(diseaseBaseValue) == 0)
                    AddCondition(PatchCondition.Diseased);
                if (refresh)
                    Refresh();
            }
        }

        /// <summary>
        /// Gets the denominator of the one-in-N disease chance for a completed growth interval.
        /// </summary>
        internal static int GetDiseaseChanceDenominator(PatchCondition conditions)
        {
            // TODO - Include a farming - required level formula
            var denominator = 25;
            if (conditions.HasFlag(PatchCondition.Watered))
                denominator += 10;
            if (conditions.HasFlag(PatchCondition.Fertilized))
                denominator += 10;
            if (conditions.HasFlag(PatchCondition.SuperFertilized))
                denominator += 20;
            return denominator;
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            var objectId = PatchDefinition.ObjectID;
            _owner.QueueTask(async cancellationToken =>
            {
                var objDefinition = await _gameObjectService.FindGameObjectDefinitionById(objectId);
                cancellationToken.ThrowIfCancellationRequested();
                _owner.Configurations.SendBitConfiguration(objDefinition.VarpBitFileId,
                    HasCondition(PatchCondition.Planted) ? GetVarpBitValue() : CurrentCycle);
            });
        }

        /// <summary>
        /// The varp bit values of each bush species in the bush patch objects of the 742 cache, indexed by the seed's species slot.
        /// </summary>
        private static readonly (int Growth, int Diseased, int Dead, int CheckHealth)[] _bushLayouts =
        [
            (5, 70, 134, 250), // redberry
            (15, 80, 144, 251), // cadavaberry
            (26, 91, 155, 252), // dwellberry
            (38, 103, 167, 253), // jangerberry
            (51, 116, 180, 254), // whiteberry
            (197, 210, 217, 255) // poison ivy
        ];

        /// <summary>
        /// Gets the base varp bit value.
        /// </summary>
        /// <returns></returns>
        private int GetBaseVarpBitValue()
        {
            if (!HasCondition(PatchCondition.Planted) || Seed == null)
            {
                return CurrentCycle;
            }

            switch (Seed.Type)
            {
                case PatchType.Allotment:
                    return 6 + Seed.VarpBitIndex * 7;
                case PatchType.Herb:
                    return 4 + Seed.VarpBitIndex * 7;
                case PatchType.FruitTree:
                    // Cache layout of patch object 7965: 27 states per species, plus a 10 state gap after every second species.
                    return 8 + Seed.VarpBitIndex * 27 + Seed.VarpBitIndex / 2 * 10;
                case PatchType.Bush:
                    return _bushLayouts[Seed.VarpBitIndex].Growth;
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
                case PatchType.FruitTree:
                    {
                        // Offsets within a species block: growth 0-5, fruit count 6-12, diseased 13-18, dead 19-24, check-health 26.
                        if (HasCondition(PatchCondition.Dead))
                            return baseValue + 19 + CurrentCycle;
                        if (HasCondition(PatchCondition.Diseased))
                            return baseValue + 13 + CurrentCycle;
                        if (!HasCondition(PatchCondition.Mature))
                            return baseValue + CurrentCycle;
                        return baseValue + (HasCondition(PatchCondition.Checked) ? 6 + _productCount : 26);
                    }
                case PatchType.Bush:
                    {
                        // Berry counts 0-4 follow the growth stages; diseased, dead and check-health states are stored apart.
                        var layout = _bushLayouts[Seed!.VarpBitIndex];
                        if (HasCondition(PatchCondition.Dead))
                            return layout.Dead + CurrentCycle;
                        if (HasCondition(PatchCondition.Diseased))
                            return layout.Diseased + CurrentCycle;
                        if (!HasCondition(PatchCondition.Mature))
                            return layout.Growth + CurrentCycle;
                        return HasCondition(PatchCondition.Checked) ? layout.Growth + Seed.MaxCycles + _productCount : layout.CheckHealth;
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
            if (!HasCondition(PatchCondition.Planted) || Seed == null)
                {
                    Grow(true);
                    return;
                }
            }

            if (!HasCondition(PatchCondition.Planted))
            {
                return;
            }

            // mature plants only regrow their products
            if (HasCondition(PatchCondition.Mature))
            {
                if (ReplenishesProducts)
                    Replenish();
                return;
            }

            // growing of plants
            if (TickCount < Seed?.CycleTicks)
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
            Hydrate(hydration, DateTimeOffset.Now);
        }

        internal void Hydrate(HydratedFarmingDto.PatchDto hydration, DateTimeOffset now)
        {
            _conditionFlag = hydration.Condition;
            _productCount = hydration.ProductCount;
            CurrentCycle = hydration.CurrentCycle;
            _seedId = hydration.SeedId;
            if (HasCondition(PatchCondition.Planted))
            {
                Seed = _farmingService.FindSeedById(hydration.SeedId).Result;
            }

            var cycleLength = HasCondition(PatchCondition.Planted) && Seed != null
                ? Seed.CycleTicks
                : _weedGrowTicks;
            if (cycleLength <= 0 || HasCondition(PatchCondition.Dead))
            {
                TickCount = 0;
                return;
            }

            if (HasCondition(PatchCondition.Mature))
            {
                HydrateReplenishment(hydration, now, cycleLength);
                return;
            }

            // CurrentCycle and the crop conditions already persist completed growth.
            // Only retain the legacy counter's within-cycle remainder; replaying its
            // quotient would advance the persisted crop state a second time.
            var ticksPassed = Math.Max(0L, hydration.CurrentCycleTicks) % cycleLength;
            ticksPassed += GetOfflineTicks(now);

            while (ticksPassed >= cycleLength)
            {
                ticksPassed -= cycleLength;
                var previousCycle = CurrentCycle;
                var previousConditions = _conditionFlag;
                Grow(false);

                if (HasCondition(PatchCondition.Mature) || HasCondition(PatchCondition.Dead))
                {
                    ticksPassed = 0;
                    break;
                }

                if (CurrentCycle == previousCycle && _conditionFlag == previousConditions)
                {
                    // A missing seed or already-empty patch cannot consume more cycles.
                    ticksPassed %= cycleLength;
                    break;
                }
            }

            TickCount = checked((int)ticksPassed);
        }

        private long GetOfflineTicks(DateTimeOffset now)
        {
            var offlineElapsed = (now - _owner.LastLogin).Ticks;
            return offlineElapsed > 0 ? offlineElapsed / (TimeSpan.TicksPerMillisecond * 600L) : 0L;
        }

        /// <summary>
        /// Regrows fruit and berries for the cycles that elapsed while the plant was saved or the owner was offline.
        /// Other mature crops hold their yield and need no catch-up.
        /// </summary>
        private void HydrateReplenishment(HydratedFarmingDto.PatchDto hydration, DateTimeOffset now, int cycleLength)
        {
            if (!ReplenishesProducts || _productCount >= Seed!.MaximumProductCount)
            {
                TickCount = 0;
                return;
            }

            var regrowTicks = RegrowTicks;
            var ticksPassed = Math.Max(0L, hydration.CurrentCycleTicks) % regrowTicks + GetOfflineTicks(now);
            var regrown = Math.Min(Seed.MaximumProductCount - _productCount, ticksPassed / regrowTicks);
            _productCount += (int)regrown;
            TickCount = _productCount >= Seed.MaximumProductCount ? 0 : (int)(ticksPassed % regrowTicks);
        }

        public HydratedFarmingDto.PatchDto Dehydrate() => new HydratedFarmingDto.PatchDto()
        {
            Id = PatchDefinition.ObjectID,
            SeedId = _seedId,
            Condition = _conditionFlag,
            ProductCount = _productCount,
            CurrentCycle = CurrentCycle,
            CurrentCycleTicks = TickCount
        };
    }
}
