using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Creatures.Characters
{
    /// <summary>
    /// Base class for character script.
    /// </summary>
    public abstract class CharacterScriptBase : ICharacterScript
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        /// <summary>
        /// Contains character.
        /// </summary>
        public ICharacter Character => _contextAccessor.Context.Character;

        public CharacterScriptBase(ICharacterContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            Initialize();
        }

        /// <summary>
        /// Initializes this script.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Get's called when character is spawned.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnSpawn() { }

        /// <summary>
        /// Get's called when character is dead.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnDeath() { }

        /// <summary>
        /// Called when the character is killed by a creature.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public virtual void OnKilledBy(ICreature creature) { }

        /// <summary>
        /// Called when the character has killed a creature.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void OnTargetKilled(ICreature target) { }

        /// <summary>
        /// Happens when character enter's world.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnRegistered() { }

        /// <summary>
        /// Get's called when character is destroyed permanently.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnDestroy() { }

        /// <summary>
        /// Tick's character.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void Tick() { }

        /// <summary>
        /// Get's called when character is interrupted.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="source">Object which performed the interruption,
        /// this parameter can be null , but it is not encouraged to do so.
        /// Best use would be to set the invoker class instance as source.</param>
        public virtual void OnInterrupt(object source) { }

        /// <summary>
        /// Get's if character is busy.
        /// By default this method returns false.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsBusy() => false;

        /// <summary>
        /// Get's if this character can attack specified target.
        /// By default , this method returns true.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>
        ///   <c>true</c> if this instance can attack the specified target; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanAttack(ICreature target) => true;

        /// <summary>
        /// Get's if this character can be attacked by specified attacker.
        /// By default , this method returns true.
        /// </summary>
        /// <param name="attacker"></param>
        public virtual bool CanBeAttackedBy(ICreature attacker) => true;

        /// <summary>
        /// Determines whether this instance [can be looted] the specified killer.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can be looted] the specified killer; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanBeLootedBy(ICreature killer) => true;

        /// <summary>
        /// Determines whether this instance [can render skull].
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public virtual bool CanRenderSkull(SkullIcon icon) => true;

        /// <summary>
        /// Called when this script is removed from the character.
        /// By default this method does nothing.
        /// </summary>
        public virtual void OnRemove() { }
    }
}