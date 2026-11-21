using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    ///     A beast of burden familiar whose special move is performed as soon as it is triggered (clicked, or aimed at a
    ///     creature) and consumes the scroll and points and grants experience, then applies the familiar's own effect
    ///     through <see cref="FamiliarScriptBase.ApplySpecialMoveEffect" />.
    ///     The familiar declares its capacity, move name, description, cost and effect.
    /// </summary>
    public abstract class SingleMoveBobFamiliarScriptBase(
        INpc owner,
        IItemContainerFactory itemContainerFactory,
        ISmartPathFinder pathFinder,
        INpcService npcService,
        IItemService itemService,
        IGroundItemBuilder groundItemBuilder,
        IItemBuilder itemBuilder,
        IWidgetScriptActivator widgetScriptActivator)
        : BobFamiliarScriptBase(owner, itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder, itemBuilder, widgetScriptActivator)
    {
        /// <inheritdoc />
        public override void PerformSpecialMove(IRuneObject target)
        {
            if (!CheckPerformSpecialMove())
            {
                SetUsingSpecialMove(false);
                return;
            }

            base.PerformSpecialMove(target);
        }

        /// <inheritdoc />
        public override void PerformAttack(ICreature target)
        {
            if (UsingSpecialMove)
            {
                PerformSpecialMove(target);
            }
            else
            {
                base.PerformAttack(target);
            }
        }

        /// <inheritdoc />
        public override void SetSpecialMoveTarget(IRuneObject? target)
        {
            if (target is ICreature && SpecialMoveClicked())
            {
                PerformSpecialMove(target);
            }
        }

        /// <inheritdoc />
        public override bool CanPoison() => false;
    }
}
