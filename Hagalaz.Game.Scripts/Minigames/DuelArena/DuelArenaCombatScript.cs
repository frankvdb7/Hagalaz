using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    /// <summary>
    /// </summary>
    public class DuelArenaCombatScript : CharacterScriptBase
    {
        private readonly IHintIconBuilder _hintIconBuilder;

        public DuelArenaCombatScript(ICharacterContextAccessor contextAccessor, ICharacter target, DuelRules rules, DuelContainer? selfContainer, DuelContainer? targetContainer, IHintIconBuilder hintIconBuilder)
            : base(contextAccessor)
        {
            _hintIconBuilder = hintIconBuilder;
            Target = target;
            Rules = rules;
            SelfContainer = selfContainer;
            TargetContainer = targetContainer;
            IsStaking = selfContainer != null && targetContainer != null;
        }

        /// <summary>
        ///     Contains duel session.
        /// </summary>
        public bool DuelSession { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is staking.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is staking; otherwise, <c>false</c>.
        /// </value>
        public bool IsStaking { get; }

        /// <summary>
        ///     Contains the target.
        /// </summary>
        public ICharacter? Target { get; private set; }

        /// <summary>
        ///     Contains the self hint icon.
        /// </summary>
        public IHintIcon? MyHintIcon { get; private set; }

        /// <summary>
        ///     Contains the self container.
        /// </summary>
        public DuelContainer? SelfContainer { get; private set; }

        /// <summary>
        ///     Contains the target container.
        /// </summary>
        public DuelContainer? TargetContainer { get; private set; }

        /// <summary>
        ///     Contains the rules.
        /// </summary>
        public DuelRules Rules { get; }

        protected override void Initialize()
        {
        }

        public override void OnRegistered()
        {
            DuelSession = true;
            MyHintIcon = _hintIconBuilder.Create().AtEntity(Target!).Build();
            Character.TryRegisterHintIcon(MyHintIcon);
            Character.QueueTask(new DuelStartTask(Character));
            Rules.ApplyRules(Character, this);
        }

        /// <summary>
        ///     Starts the victory stage.
        /// </summary>
        /// <param name="victor">The victor.</param>
        /// <param name="loser">The loser.</param>
        public void StartVictoryStage(ICharacter victor, ICharacter loser)
        {
            if (!DuelSession)
            {
                return;
            }

            victor.Respawn();
            loser.Respawn();
            if (loser.IsDestroyed)
            {
                loser.Movement.Teleport(Teleport.Create(loser.Area.Script.GetRespawnLocation(loser)));
            }

            var victorDuelEndScreen = victor.ServiceProvider.GetRequiredService<DuelEndScreenScript>();
            victorDuelEndScreen.Opponent = loser;
            victorDuelEndScreen.Victorious = true;
            victor.Widgets.OpenWidget(1365, 0, victorDuelEndScreen, false);
            if (!loser.IsDestroyed)
            {
                var loserDuelEndScreen = loser.ServiceProvider.GetRequiredService<DuelEndScreenScript>();
                loserDuelEndScreen.Opponent = victor;
                loserDuelEndScreen.Victorious = false;
                loser.Widgets.OpenWidget(1365, 0, loserDuelEndScreen, false);
            }

            if (IsStaking)
            {
                var victoryInterface = victor.Widgets.GetOpenWidget(1365);
                var loseInterface = loser.Widgets.GetOpenWidget(1365);
                if (victoryInterface != null)
                {
                    //victoryInterface.SetOptions(14, 0, 27, (0x2 | 0x400)); // allow clicking of 2 right click options + auto examine option ( last ))
                    //victor.Configurations.SendCS2Script(158, new object[] { (1365 << 16 | 14), 130, 3, 3, 1, -1, "Value", "", "", "", "" });

                    victor.Configurations.SendItems(136, false, victor == Character ? TargetContainer : SelfContainer);
                }

                if (loseInterface != null)
                {
                    // loseInterface.SetOptions(14, 0, 27, (0x2 | 0x400)); // allow clicking of 2 right click options + auto examine option ( last ))
                    //loser.Configurations.SendCS2Script(158, new object[] { (1365 << 16 | 14), 130, 3, 3, 1, -1, "Value", "", "", "", "" });

                    loser.Configurations.SendItems(136, false, victor == Character ? TargetContainer : SelfContainer);
                }

                var removedCoins = 0;
                var coins = SelfContainer.GetById(995);
                if (coins != null)
                {
                    removedCoins += SelfContainer.Remove(coins);
                }

                coins = TargetContainer.GetById(995);
                if (coins != null)
                {
                    removedCoins += TargetContainer.Remove(coins);
                }

                if (removedCoins > 0)
                {
                    victor.MoneyPouch.Add(removedCoins);
                }

                victor.Inventory.AddRange(SelfContainer);
                victor.Inventory.AddRange(TargetContainer);
            }

            victor.GetScript<DuelArenaCombatScript>()?.CancelDuelSession();
            loser.GetScript<DuelArenaCombatScript>()?.CancelDuelSession();
        }

        /// <summary>
        ///     Cancels the duel session.
        /// </summary>
        public void CancelDuelSession()
        {
            if (!DuelSession)
            {
                return;
            }

            DuelSession = false;
            if (MyHintIcon != null)
            {
                Character.TryUnregisterHintIcon(MyHintIcon);
            }
            Target = null;
            SelfContainer = null;
            TargetContainer = null;
            MyHintIcon = null;
        }

        /// <summary>
        ///     Determines whether this instance [can be looted] the specified killer.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can be looted] the specified killer; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanBeLootedBy(ICreature killer) => false;

        /// <summary>
        ///     Determines whether this instance [can render skull].
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public override bool CanRenderSkull(SkullIcon icon) => false;

        /// <summary>
        ///     Get's if this character can be attacked by specified attacker.
        ///     By default , this method returns true.
        /// </summary>
        /// <param name="attacker"></param>
        /// <returns></returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (Character.Movement.Locked)
            {
                return false; // wait for unlock movement: start fighting
            }

            if (attacker is ICharacter)
            {
                if (Target != attacker)
                {
                    ((ICharacter)attacker).SendChatMessage("That player is not your target.");
                    return false;
                }
            }
            else if (attacker is INpc npc)
            {
                if (npc.HasScript<FamiliarScriptBase>())
                {
                    if (npc.GetScript<FamiliarScriptBase>().Summoner != Target)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Get's if this character can attack specified target.
        ///     By default , this method returns true.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public override bool CanAttack(ICreature target)
        {
            if (Target != target)
            {
                Character.SendChatMessage("That player is not your target.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Called when the character is killed by a creature.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnKilledBy(ICreature creature)
        {
            if (creature is ICharacter character)
            {
                if (Target == character)
                {
                    StartVictoryStage(character, Character);
                }

                var message = GameStrings.ResourceManager.GetString(GameStrings.Combat_VictorKillMessage + RandomStatic.Generator.Next(9));
                if (message != null)
                {
                    Target?.SendChatMessage(string.Format(message, Character.DisplayName));
                }

                // TODO
                //var database = ServiceLocator.Current.GetInstance<ISqlDatabaseManager>();
                // database.ExecuteAsync(new ActivityLogQuery(character.MasterId, "Duel Victory", "I have defeated " + Character.DisplayName + " in the duel arena."));
            }
        }

        /// <summary>
        ///     Called when this script is removed from the character.
        ///     By default this method does nothing.
        /// </summary>
        public override void OnRemove() => CancelDuelSession();

        /// <summary>
        ///     Tick's character.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (DuelSession)
            {
                if (Target.IsDestroyed)
                {
                    StartVictoryStage(Character, Target);
                }

                if (Character.IsDestroyed)
                {
                    StartVictoryStage(Target, Character);
                }
            }
        }
    }
}