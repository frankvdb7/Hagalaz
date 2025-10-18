using System.Diagnostics.CodeAnalysis;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class CharacterExtensions
    {
        /// <summary>
        /// Determines whether [has display name].
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if [has display name] [the specified character]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDisplayName(this ICharacter character) => !string.IsNullOrEmpty(character.PreviousDisplayName);

        /// <summary>
        /// Determines whether [is transformed to NPC].
        /// </summary>
        /// <param name="appearance">The appearance.</param>
        /// <returns>
        ///   <c>true</c> if [is transformed to NPC] [the specified appearance]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsTransformedToNpc(this ICharacterAppearance appearance) => appearance.NpcId != -1;

        /// <summary>
        /// Determines whether this instance has familiar.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if the specified character has familiar; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasFamiliar(this ICharacter character) => character.FamiliarScript != null!;

        /// <summary>
        /// Determines whether this instance has clan.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if the specified character has clan; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasClan(this ICharacter character) => character.Clan != null!;

        /// <summary>
        /// Determines whether this instance has task.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if the specified character has task; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasSlayerTask(this ICharacter character) => character.Slayer.CurrentTaskId != -1;

        /// <summary>
        /// Forces the type of the run movement.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="forceRun">if set to <c>true</c> [force run].</param>
        /// <returns></returns>
        public static void ForceRunMovementType(this ICharacter character, bool forceRun)
        {
            if (forceRun)
            {
                character.Movement.MovementType = MovementType.Run;
                return;
            }

            character.ResetMovementType();
        }

        /// <summary>
        /// Resets the type of movement for the character to default
        /// </summary>
        /// <param name="character"></param>
        public static void ResetMovementType(this ICharacter character)
        {
            var isRunToggled = character.Profile.GetValue<bool>(ProfileConstants.RunSettingsToggled);
            character.Movement.MovementType = isRunToggled ? MovementType.Run : MovementType.Walk;
        }

        /// <summary>
        /// Tries to get the character script of the supplied type.
        /// Returns true if script is found.
        /// </summary>
        /// <typeparam name="TScriptType"></typeparam>
        /// <returns></returns>
        public static bool TryGetScript<TScriptType>(this ICharacter character, [NotNullWhen(true)] out TScriptType? script)
            where TScriptType : class, ICharacterScript
        {
            script = character.GetScript<TScriptType>();
            return script != null;
        }

        /// <summary>
        /// Gets or adds the script if it does not exist.
        /// </summary>
        /// <typeparam name="TScriptType"></typeparam>
        /// <returns></returns>
        public static TScriptType GetOrAddScript<TScriptType>(this ICharacter character) where TScriptType : class, ICharacterScript =>
            character.TryGetScript<TScriptType>(out var script) ? script : character.AddScript<TScriptType>();
    }
}