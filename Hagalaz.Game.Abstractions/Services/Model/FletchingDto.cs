namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the persisted definition of a Fletching recipe.
    /// The product arrays are parallel: index <c>i</c> describes the <c>i</c>-th product the recipe can make.
    /// </summary>
    public record FletchingDto
    {
        /// <summary>
        /// The resource item id. This item is necessary to create products with the tool.
        /// </summary>
        public required int ResourceId { get; init; }

        /// <summary>
        /// The tool item id. This item is necessary to create products from the resource.
        /// </summary>
        public required int ToolId { get; init; }

        /// <summary>
        /// The kind of recipe.
        /// </summary>
        public required FletchingCategory Category { get; init; }

        /// <summary>
        /// The product item ids.
        /// </summary>
        public required int[] ProductIds { get; init; }

        /// <summary>
        /// The amount of items made per product.
        /// </summary>
        public required int[] ProductCounts { get; init; }

        /// <summary>
        /// The experience granted per made item of each product.
        /// </summary>
        public required double[] ProductExperiences { get; init; }

        /// <summary>
        /// The Fletching level required for each product.
        /// </summary>
        public required int[] RequiredLevels { get; init; }

        /// <summary>
        /// The animation id, or <c>-1</c> when the recipe has no animation.
        /// </summary>
        public int AnimationId { get; init; } = -1;
    }
}
