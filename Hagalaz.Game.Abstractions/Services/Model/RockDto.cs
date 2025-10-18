namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a mineable rock.
    /// </summary>
    public record RockDto
    {
        /// <summary>
        /// Gets the game object ID of the rock.
        /// </summary>
        public required int RockId { get; init; }

        /// <summary>
        /// Gets the game object ID of the depleted (exhausted) version of this rock.
        /// </summary>
        public required int ExhaustRockId { get; init; }

        /// <summary>
        /// Gets the item ID of the ore that this rock yields.
        /// </summary>
        public required int OreId { get; init; }
    }
}