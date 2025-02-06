using System;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    /// <summary>
    /// Contains data on raw fish.
    /// </summary>
    public class FishingLoot : ILootObject, IFishingLoot
    {
        /// <summary>
        /// The item id of the raw fish.
        /// </summary>
        public required int Id { get; init; }

        /// <summary>
        /// The fishing level requirement to be caught.
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// The fishing experienced recieved when caught.
        /// </summary>
        public required double FishingExperience { get; init; }

        /// <summary>
        /// The probability
        /// </summary>
        public required double Probability { get; init; }

        /// <summary>
        /// Gets or sets whether this object will always be part of the result set
        /// (Probability is ignored when this flag is set to true)
        /// </summary>
        public bool Always => false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IRandomObject" /> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled => true;

        public int MinimumCount => 1;

        public int MaximumCount => 1;

        /// <summary>
        /// Gets the probability.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public double GetProbability(ICharacter character) => character.Statistics.GetSkillLevel(10) < RequiredLevel ? 0.0 : Probability;
        public int GetRandomLootCount(ICharacter character) => Random.Shared.Next(MinimumCount, MaximumCount);
    }
}