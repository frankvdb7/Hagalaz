using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Combat;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    /// </summary>
    public class RoguesDenWallSafeTask : RsTickTask
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoguesDenWallSafeTask" /> class.
        /// </summary>
        /// <param name="wallSafe">The wall safe.</param>
        /// <param name="owner">The owner.</param>
        public RoguesDenWallSafeTask(IGameObject wallSafe, ICharacter owner)
        {
            _wallSafe = wallSafe;
            _owner = owner;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = owner.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            SetCycle();
        }

        /// <summary>
        ///     The owner.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        ///     The wall safe.
        /// </summary>
        private readonly IGameObject _wallSafe;

        /// <summary>
        ///     The cycle.
        /// </summary>
        private int _cycle;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            _cycle--;
            if (_cycle != 0)
            {
                return;
            }

            if (GetSuccessFactor() > RandomStatic.Generator.NextDouble())
            {
                var lootService = _owner.ServiceProvider.GetRequiredService<ILootService>();
                var table = lootService.FindGameObjectLootTable(_wallSafe.Definition.LootTableId).Result;
                if (table == null)
                {
                    Cancel();
                    return;
                }

                _owner.Inventory.TryAddLoot(_owner, table, out _);
                _owner.Statistics.AddExperience(StatisticsConstants.Thieving, 70.0);
                _owner.QueueAnimation(Animation.Create(2249));
                Cancel();
                return;
            }

            if (GetFailFactor() < RandomStatic.Generator.NextDouble())
            {
                _owner.SendChatMessage("You accidentally activated one of the safes security mechanisms.");
                var damage = RandomStatic.Generator.Next(1, 61);
                var splat = new HitSplat(null!);
                splat.SetFirstSplat(HitSplatType.HitSimpleDamage, damage, false);
                _owner.QueueHitSplat(splat);
                _owner.Statistics.DamageLifePoints(damage);
                _owner.QueueAnimation(Animation.Create(2249));
                Cancel();
                return;
            }

            SetCycle();
            _owner.QueueAnimation(Animation.Create(2248));
        }

        /// <summary>
        ///     Sets the cycle.
        /// </summary>
        private void SetCycle() => _cycle = 2 + RandomStatic.Generator.Next(0, 3);

        /// <summary>
        ///     Gets the fail factor.
        /// </summary>
        /// <returns></returns>
        private double GetFailFactor() => 1.0 - GetSuccessFactor();

        /// <summary>
        ///     Gets the success factor.
        /// </summary>
        /// <returns></returns>
        private double GetSuccessFactor()
        {
            var factor = 0.15;
            if (_owner.Inventory.Contains(5560))
            {
                factor += 0.05;
            }

            var thievingLevel = _owner.Statistics.GetSkillLevel(StatisticsConstants.Thieving) - 50;
            if (thievingLevel < 0)
            {
                thievingLevel = 0;
            }

            factor += thievingLevel * 0.005;

            var agilityLevel = _owner.Statistics.GetSkillLevel(StatisticsConstants.Agility) - 50;
            if (agilityLevel < 0)
            {
                agilityLevel = 0;
            }

            factor += agilityLevel * 0.001;

            if (factor > 0.90)
            {
                factor = 0.90;
            }

            return factor;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _owner.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}