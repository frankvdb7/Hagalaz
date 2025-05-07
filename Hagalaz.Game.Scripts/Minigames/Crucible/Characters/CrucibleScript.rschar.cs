using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.Crucible.Characters
{
    /// <summary>
    /// </summary>
    public class CrucibleScript : CharacterScriptBase
    {
        private readonly IHintIconBuilder _hintIconBuilder;

        /// <summary>
        ///     The immunity tick
        /// </summary>
        private int _immunityTick;

        /// <summary>
        ///     The has immunity
        /// </summary>
        private bool _hasImmunity;

        /// <summary>
        ///     The crusable interface.
        /// </summary>
        private IWidget _crusibleInter;

        /// <summary>
        ///     The target
        /// </summary>
        private ICharacter _target;

        /// <summary>
        ///     The target hint icon
        /// </summary>
        private IHintIcon _targetHintIcon;

        public CrucibleScript(ICharacterContextAccessor contextAccessor, IHintIconBuilder hintIconBuilder)
            : base(contextAccessor) =>
            _hintIconBuilder = hintIconBuilder;

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            if (Character.Widgets.GetOpenWidget(1296) == null)
            {
                var script = Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                Character.Widgets.OpenWidget(1296, Character.GameClient.IsScreenFixed ? 51 : 69, 1, script, false);
                _crusibleInter = Character.Widgets.GetOpenWidget(1296);
            }

            Refresh();
        }

        /// <summary>
        ///     Get's if this character can attack specified target.
        ///     By default , this method returns true.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public override bool CanAttack(ICreature target) => target == _target;

        /// <summary>
        ///     Get's if this character can be attacked by specified attacker.
        ///     By default , this method returns true.
        /// </summary>
        /// <param name="attacker"></param>
        /// <returns></returns>
        public override bool CanBeAttackedBy(ICreature attacker) => attacker == _target;

        /// <summary>
        ///     Determines whether this instance [can render skull].
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public override bool CanRenderSkull(SkullIcon icon) => false;

        /// <summary>
        ///     Called when the character has killed a creature.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnTargetKilled(ICreature target) => base.OnTargetKilled(target);

        /// <summary>
        ///     Called when this script is removed from the character.
        ///     By default this method does nothing.
        /// </summary>
        public override void OnRemove()
        {
            _target = null;
            if (Character.Widgets.GetOpenWidget(1296) != null)
            {
                _crusibleInter.Close();
                _crusibleInter = null;
            }

            Character.Appearance.SkullIcon = SkullIcon.None;
            if (_targetHintIcon != null)
            {
                Character.TryUnregisterHintIcon(_targetHintIcon);
                _targetHintIcon = null;
            }
        }

        /// <summary>
        ///     Refreshes this instance.
        /// </summary>
        private void Refresh()
        {
            if (_hasImmunity)
            {
                _crusibleInter.DrawString(26, _immunityTick.ToString()); // todo calculate seconds
                if (Character.Appearance.SkullIcon != SkullIcon.CrucibleImmunity)
                {
                    Character.Appearance.SkullIcon = SkullIcon.CrucibleImmunity;
                }
            }
            else if (_target != null)
            {
                _crusibleInter.DrawString(25, _target.DisplayName);
                _crusibleInter.DrawString(26, "None");
                if (Character.Appearance.SkullIcon != SkullIcon.CrucibleAttackable)
                {
                    Character.Appearance.SkullIcon = SkullIcon.CrucibleAttackable;
                }

                _targetHintIcon = _hintIconBuilder.Create().AtEntity(_target).WithModelId(9).Build();
                Character.TryRegisterHintIcon(_targetHintIcon);
            }
            else if (Character.Appearance.SkullIcon != SkullIcon.None)
            {
                if (_targetHintIcon != null)
                {
                    Character.TryUnregisterHintIcon(_targetHintIcon);
                    _targetHintIcon = null;
                }

                _crusibleInter.DrawString(26, "None");
                Character.Appearance.SkullIcon = SkullIcon.None;
            }
        }

        /// <summary>
        ///     Tick's character.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
        }
    }
}