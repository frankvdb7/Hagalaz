namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different customizable parts of a character's base appearance (their "look").
    /// </summary>
    public enum LookType : byte
    {
        /// <summary>
        /// The character's hairstyle.
        /// </summary>
        HairLook = 0,
        /// <summary>
        /// The character's facial hair style (beard/mustache).
        /// </summary>
        BeardLook = 1,
        /// <summary>
        /// The character's torso (shirt) style.
        /// </summary>
        TorsoLook = 2,
        /// <summary>
        /// The character's arm (sleeve) style.
        /// </summary>
        ArmsLook = 3,
        /// <summary>
        /// The character's wrist (hands/gloves) style.
        /// </summary>
        WristLook = 4,
        /// <summary>
        /// The character's leg (pants) style.
        /// </summary>
        LegsLook = 5,
        /// <summary>
        /// The character's feet (shoes) style.
        /// </summary>
        FeetLook = 6,
    }
}
