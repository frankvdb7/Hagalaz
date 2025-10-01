using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Pnpcs
{
    [CharacterNpcScriptMetaData([2417, 3334])]
    public class WildyWyrm : CharacterNpcScriptBase
    {
        public WildyWyrm(ITypeProvider<IItemDefinition> itemProvider) => _itemProvider = itemProvider;

        /// <summary>
        /// </summary>
        private enum AttackType
        {
            Melee,
            Ranged,
            Magic
        }

        /// <summary>
        ///     The initialized.
        /// </summary>
        private bool _initialized;

        /// <summary>
        ///     The attack type.
        /// </summary>
        private readonly AttackType _attackType = AttackType.Melee;

        /// <summary>
        ///     The equip allow event.
        /// </summary>
        private EventHappened? _equipAllowEvent;

        /// <summary>
        /// 
        /// </summary>
        private readonly ITypeProvider<IItemDefinition> _itemProvider;

        /// <summary>
        ///     Happens when character is turned to npc for
        ///     which this script is made.
        ///     This method usually get's called after Initialize()
        /// </summary>
        public override void OnTurnToNpc()
        {
            if (_initialized)
            {
                return;
            }

            Owner.Statistics.Normalise();
            Owner.Statistics.Bonuses.Reset();
            Owner.Statistics.Bonuses.Add(Definition.Bonuses);

            _equipAllowEvent = Owner.RegisterEventHandler(new EventHappened<EquipAllowEvent>(e => true));

            _initialized = true;
        }

        /// <summary>
        ///     Happens when character is turned back to player
        ///     and the script is about to be disposed, this method gives
        ///     opportunity to release used resources.
        /// </summary>
        public override void OnTurnToPlayer()
        {
            Owner.Statistics.Normalise();
            Owner.Statistics.CalculateBonuses();

            if (_equipAllowEvent != null)
            {
                Owner.UnregisterEventHandler<EquipAllowEvent>(_equipAllowEvent);
            }
        }

        /// <summary>
        ///     Called when [spawn].
        /// </summary>
        public override void OnSpawn() => Owner.Appearance.TurnToPlayer();

        /// <summary>
        ///     Gets the display name.
        /// </summary>
        /// <returns></returns>
        public override string GetDisplayName() => "Wildy Wyrm";

        /// <summary>
        ///     Gets the combat level.
        /// </summary>
        /// <returns></returns>
        public override int GetCombatLevel() => 562;

        /// <summary>
        ///     Called when [killed by].
        /// </summary>
        /// <param name="killer">The killer.</param>
        public override void OnDeathBy(ICreature killer)
        {
            if (killer is not ICharacter kill)
            {
                return;
            }

            Owner.QueueTask(async () =>
            {
                var lootService = Owner.ServiceProvider.GetRequiredService<ILootService>();
                var table = await lootService.FindNpcLootTable(Definition.LootTableId);
                if (table == null)
                {
                    return;
                }

                var groundItemBuilder = Owner.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
                var lootGenerator = Owner.ServiceProvider.GetRequiredService<ILootGenerator>();
                foreach (var loot in lootGenerator.GenerateLoot<ILootItem>(new CharacterLootParams(table, kill)))
                {
                    groundItemBuilder.Create()
                        .WithItem(itemBuilder => itemBuilder.Create().WithId(loot.Item.Id).WithCount(loot.Count))
                        .WithLocation(Owner.Location)
                        .WithOwner(kill)
                        .Spawn();
                }
            });
        }

        /// <summary>
        ///     Gets the type of the attack bonus.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType()
        {
            switch (_attackType)
            {
                case AttackType.Melee: return AttackBonus.Crush;
                case AttackType.Ranged: return AttackBonus.Ranged;
                case AttackType.Magic: return AttackBonus.Magic;
                default: return base.GetAttackBonusType();
            }
        }

        /// <summary>
        ///     Gets the attack style.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle()
        {
            switch (_attackType)
            {
                case AttackType.Melee: return AttackStyle.MeleeAggressive;
                case AttackType.Ranged: return AttackStyle.RangedAccurate;
                case AttackType.Magic: return AttackStyle.MagicNormal;
                default: return base.GetAttackStyle();
            }
        }

        /// <summary>
        ///     Gets the attack distance.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance()
        {
            if (_attackType == AttackType.Ranged || _attackType == AttackType.Magic)
            {
                return 8;
            }

            return 1;
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attackType)
            {
                case AttackType.Melee: base.PerformAttack(target); break;
                case AttackType.Ranged:
                case AttackType.Magic:
                    Owner.QueueAnimation(Animation.Create(12794));
                    break;
            }
        }

        /// <summary>
        ///     Determines whether this instance [can be looted by] the specified killer.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns></returns>
        public override bool CanBeLootedBy(ICreature killer) => true;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Ticks this instance.
        /// </summary>
        public override void Tick()
        {
            if (Owner.Appearance.NpcId == 3334)
            {
                if (Owner.Combat.IsInCombat())
                {
                    return;
                }

                Owner.QueueAnimation(Animation.Create(12796));
                Owner.QueueTask(new RsTask(() => { Owner.Appearance.TurnToNpc(2417, this); }, 2));
            }
            else if (Owner.Appearance.NpcId == 2417)
            {
                if (!Owner.Combat.IsInCombat())
                {
                    return;
                }

                Owner.Appearance.TurnToNpc(3334, this);
                Owner.QueueAnimation(Animation.Create(12795));
            }


            //IEnumerable<Character> characters = this.owner.GameMap.GetVisibleCharacters().Where(c => c.WithinRange(this.owner, 0));
            //foreach (Character c in characters)
            //{

            //}
        }
    }
}