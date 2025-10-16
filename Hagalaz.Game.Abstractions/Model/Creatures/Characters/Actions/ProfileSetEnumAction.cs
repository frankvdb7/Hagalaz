using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to set an enum value in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Value">The enum value to set.</param>
    public record ProfileSetEnumAction(string Key, Enum Value);
}
