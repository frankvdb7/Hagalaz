using System.Diagnostics.CodeAnalysis;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides a collection of extension methods for the <see cref="ICharacter"/> and related interfaces,
    /// offering convenient shortcuts for common operations and checks.
    /// </summary>
    public static class CharacterExtensions
    {
        /// <summary>
        /// Checks if the character has a previous display name, indicating a name change has occurred.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns><c>true</c> if the character has a previous display name; otherwise, <c>false</c>.</returns>
        public static bool HasDisplayName(this ICharacter character) => !string.IsNullOrEmpty(character.PreviousDisplayName);

        /// <summary>
        /// Checks if the character's appearance is currently transformed into that of an NPC.
        /// </summary>
        /// <param name="appearance">The character appearance data to check.</param>
        /// <returns><c>true</c> if the character is transformed into an NPC; otherwise, <c>false</c>.</returns>
        public static bool IsTransformedToNpc(this ICharacterAppearance appearance) => appearance.NpcId != -1;

        /// <summary>
        /// Checks if the character currently has an active familiar.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns><c>true</c> if the character has a familiar; otherwise, <c>false</c>.</returns>
        public static bool HasFamiliar(this ICharacter character) => character.FamiliarScript != null!;

        /// <summary>
        /// Checks if the character is currently a member of a clan.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns><c>true</c> if the character is in a clan; otherwise, <c>false</c>.</returns>
        public static bool HasClan(this ICharacter character) => character.Clan != null!;

        /// <summary>
        /// Checks if the character currently has an active Slayer task.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns><c>true</c> if the character has a Slayer task; otherwise, <c>false</c>.</returns>
        public static bool HasSlayerTask(this ICharacter character) => character.Slayer.CurrentTaskId != -1;

        /// <summary>
        /// Temporarily forces a character's movement type to Run or resets it to their default setting.
        /// </summary>
        /// <param name="character">The character whose movement type will be changed.</param>
        /// <param name="forceRun">If set to <c>true</c>, the character's movement type is set to <see cref="MovementType.Run"/>. If <c>false</c>, it is reset to the default.</param>
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
        /// Resets the character's movement type to their default setting, based on whether their run toggle is active.
        /// </summary>
        /// <param name="character">The character whose movement type will be reset.</param>
        public static void ResetMovementType(this ICharacter character)
        {
            var isRunToggled = character.Profile.GetValue<bool>(ProfileConstants.RunSettingsToggled);
            character.Movement.MovementType = isRunToggled ? MovementType.Run : MovementType.Walk;
        }

        /// <summary>
        /// Tries to get a script of a specified type that is attached to the character.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to retrieve.</typeparam>
        /// <param name="character">The character to get the script from.</param>
        /// <param name="script">When this method returns, contains the script instance if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a script of the specified type is found; otherwise, <c>false</c>.</returns>
        public static bool TryGetScript<TScriptType>(this ICharacter character, [NotNullWhen(true)] out TScriptType? script)
            where TScriptType : class, ICharacterScript
        {
            script = character.GetScript<TScriptType>();
            return script != null;
        }

        /// <summary>
        /// Gets a script of a specified type from the character. If the script does not exist, it is added first and then returned.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to get or add.</typeparam>
        /// <param name="character">The character to get or add the script to.</param>
        /// <returns>The existing or newly added script instance.</returns>
        public static TScriptType GetOrAddScript<TScriptType>(this ICharacter character) where TScriptType : class, ICharacterScript =>
            character.TryGetScript<TScriptType>(out var script) ? script : character.AddScript<TScriptType>();
    }
}